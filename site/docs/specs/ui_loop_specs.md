---
title: UI Loop & Frame Pacing (Internal Spec)
---

# UI Loop & Frame Pacing (Internal Spec)

This document proposes a better host loop for `TerminalApp.RunCore`.

The current implementation is intentionally simple:

- run `Tick()`
- `Thread.Sleep(_options.UpdateWaitDuration)`
- repeat

That design works, but it has two visible problems:

- On Windows, `Thread.Sleep(1)` is commonly limited by the system timer tick and can wake much later than requested.
- The loop is pure polling, so it pays the same wake-up cost whether the app is busy, animating, or completely idle.

For a terminal UI framework we want something more deterministic, but still low-complexity and power-friendly.

> [!NOTE]
> The `Thread.Yield()` guidance in this document is an engineering recommendation inferred from the documented behavior of
> `.NET` and Windows timing primitives, combined with common frame-pacing practice. Microsoft documents the primitive
> semantics, but not a Terminal.UI-specific pacing policy.

## Related specs

This spec sits in the middle of several other framework mechanisms:

- [Binding + Dirty Model](binding_dirty_model_specs.md) for how bindable writes become per-phase invalidation work.
- [Layout Protocol](layout_protocol_specs.md) for measure/arrange invariants that `Tick()` may trigger before render.
- [Dirty Rendering](dirty_rendering_specs.md) for render invalidation, repaint regions, and how render work is reduced once a frame is requested.

The loop spec does **not** redefine those systems. It defines how quickly and predictably the host loop wakes up to run
them.

## Threading invariants

This spec does **not** move UI work off the main/UI thread.

The following must continue to execute on the main/UI thread:

- `Tick()`
- bindable write processing
- dynamic updates / prepare-children / measure / arrange / render
- routed input dispatch
- animation advancement
- the host `onUpdate` callback

If a background helper is introduced, it must be a **passive wake proxy only**:

- it may wait for raw terminal events or task completion,
- it may enqueue raw data into a thread-safe queue,
- it may signal a wake handle,
- it must not mutate UI state, dispatch routed events, or render.

This preserves the current single-threaded UI contract.

## Goals

- Keep the UI thread model single-threaded.
- Improve wake-up precision on Windows without changing global timer resolution.
- Avoid drift by scheduling against absolute deadlines.
- Wake immediately for input, resize, `Post(...)`, render requests, and animation requests.
- Avoid busy-waiting by default.
- Keep deterministic `BeginRun()` / `Tick()` test hooks intact.

## Non-goals

- Do not call `timeBeginPeriod(1)` or otherwise change system-wide timer resolution.
- Do not introduce a permanent `Thread.Yield()` / `SpinWait()` loop in the default path.
- Do not require cross-repo changes in `XenoAtom.Terminal` for V1.
- Do not change the retained-mode rendering model.

## Why the current loop is not sufficient

`Thread.Sleep` is only a minimum suspension request. On Windows, if the requested duration is below the system clock
resolution, the actual wake-up can land on a later scheduler tick. For `UpdateWaitDuration = 1ms`, that means the app
can feel much less responsive than intended, especially for:

- pointer hover and drag feedback,
- resize handling,
- short animation intervals,
- async update completion that should resume the UI quickly.

The framework already uses `Stopwatch` timestamps for other timing-sensitive behavior, and animated visuals already
express their next desired update using absolute `Stopwatch` ticks (`IAnimatedVisual.NextAnimationTick`). The host loop
should use the same clock and schedule around absolute deadlines instead of repeated relative sleeps.

## Design summary

The proposed design has five parts:

1. Use `Stopwatch` as the canonical monotonic clock.
2. Replace fixed `Thread.Sleep(...)` with an interruptible waiter that blocks until either:
   - a wake signal is raised, or
   - the next absolute deadline is reached.
3. On Windows, use a waitable timer created with `CREATE_WAITABLE_TIMER_HIGH_RESOLUTION` when supported.
4. Use a two-phase wait:
   - block for the coarse part of the remaining time,
   - optionally use a short adaptive `Thread.Yield()` tail for the final sub-resolution window.
5. Make the blocking `Run` path, and the `RunAsync`-style hosting paths that still execute the loop on the calling
   thread, event-driven for input and async-update completion instead of pure polling.

This keeps the implementation simple while removing the main source of jitter and unnecessary CPU wake-ups.

## Run model clarification

The current code has three distinct execution models and the spec should keep them separate:

- `Run()`:
  - executes the UI loop synchronously on the calling thread.
- `RunAsync(CancellationToken)` on `TerminalApp`:
  - currently just calls `Run(...)` and returns `Task.CompletedTask`;
  - it is **not** a separate background-thread host loop today.
- `BeginRun()` / `Tick()` / `EndRun()`:
  - deterministic test hooks that bypass sleeping/waiting entirely.

There are also public `Terminal.LiveAsync(...)` / `Terminal.RunAsync(...)` hosting overloads. Their async aspect is the
update callback, not a separate UI thread. The loop itself still stays on the calling thread.

Therefore:

- this spec applies to the blocking host loop used by normal app execution,
- the proposed waiter logic must not affect `BeginRun()` / `Tick()` deterministic tests,
- any future `TerminalLoopMode` applies to `Run()`-style host loops only, not to manual test stepping.

## Proposed loop model

### 1. Separate "wake reasons" from `Tick()`

`Tick()` should remain the unit that:

- drains pending UI-thread actions,
- drains terminal events,
- advances animations,
- runs the host update callback,
- processes binding writes,
- renders if needed.

What changes is the wait before the next `Tick()`.

Instead of always sleeping for a fixed duration, the loop computes the next wake deadline from its current state.

### 2. Use absolute deadlines

All scheduling decisions should be based on absolute `Stopwatch` timestamps:

- animation deadline: `_nextAnimationTick`
- optional polling heartbeat: `lastHeartbeat + heartbeatTicks`
- immediate work: `now`

When a deadline is reached, the next one should be computed from the previous scheduled time, not from "now", unless the
loop is badly late and must resynchronize.

This avoids accumulated drift.

#### Polling slices vs frame cadence

The spec intentionally separates two different concepts:

- **polling slice**
  - how long the loop may block before it wakes up and checks state again in polling mode.
- **frame cadence**
  - when the next render/update should ideally happen for visual smoothness.

`UpdateWaitDuration` maps naturally to the first concept, not the second.

Therefore, `UpdateWaitDuration` should not be reinterpreted as "the app targets one frame every N milliseconds" or as an
implicit 60 FPS setting. It is better understood as a maximum coarse wait slice used by legacy/polling behavior.

If the framework later wants a public frame-cadence concept, it should be introduced explicitly with a separate option
such as `TargetFrameInterval` or `MaxFrameRate`.

#### Animation sentinel

The current code uses `_nextAnimationTick = 0` as an internal sentinel meaning "wake immediately".

`ComputeNextWakeDeadline(now)` should preserve that contract:

- `_nextAnimationTick == 0` means return `now`,
- `_nextAnimationTick == long.MaxValue` means "no animation deadline pending".

That keeps the existing `RequestAnimation()` behavior compatible with the new waiter design.

#### Catch-up policy

The loop needs a small explicit late-frame policy so it does not accumulate drift after an overrun.

Recommended policy:

- if a deadline is missed by a small amount, run the next tick immediately and preserve the existing schedule,
- if the loop is late by more than a bounded threshold, resynchronize.

Reasonable starting rule:

- if `now - deadline > 2 * effectiveWaitResolution` or `now - deadline > 1 frame interval` for animation-driven waits,
  snap the next deadline to `now`.

The exact threshold can be tuned later, but the spec should require explicit late-frame handling instead of letting drift
grow silently.

### 3. Use an interruptible wake signal

The blocking run loop needs a synchronous wake handle that can interrupt a timed wait.

Recommended internal additions:

- retain `_wakeUp`: `AsyncAutoResetEvent` for existing async-friendly wake behavior,
- add `_wakeSignal`: `AutoResetEvent` or equivalent synchronous waitable primitive used only by the blocking run loop.

The following should signal **both** wake mechanisms through a shared helper (for example `SignalWakeUp()`):

- `Post(...)`
- `RequestRender()`
- `RequestAnimation()`
- completion of a pending async update task
- arrival of input/resize events from a background input relay
- cancellation / stop

The spec intentionally keeps both:

- `_wakeUp` continues to serve existing async/cooperative paths,
- `_wakeSignal` gives the blocking wait code a real handle that can participate in kernel waits.

The new handle is therefore a **second wake channel**, not a replacement.

### 4. Blocking loop pseudocode

Conceptual shape:

```csharp
BeginRunCore(...);
StartInputRelayIfNeeded();

while (!token.IsCancellationRequested)
{
    Tick();

    var now = Stopwatch.GetTimestamp();
    var deadline = ComputeNextWakeDeadline(now);
    if (deadline <= now)
    {
        continue;
    }

    _waiter.WaitUntil(deadline, _wakeSignal, token);
}
```

`ComputeNextWakeDeadline(now)` returns the earliest of:

- `now` if there is already work pending,
- the next animation deadline,
- the next polling heartbeat if polling mode is enabled,
- "infinite" if the app is idle and only wake signals should resume the loop.

In `Polling` mode, the coarse wait should be sliced:

- compute the absolute next deadline first,
- then block for `min(deadline - now, UpdateWaitDuration)`,
- wake and re-evaluate,
- keep the deadline absolute across slices.

That preserves compatibility for callers that expect periodic update heartbeats, while still avoiding the drift of a
pure relative-sleep loop.

## Input handling in blocking runs

Today `Tick()` calls `_terminal.TryReadEvent(...)` directly, so the blocking loop must poll to notice new input.

For the blocking run path, the recommended design is to add a lightweight background input relay:

- It awaits `_terminal.ReadEventAsync(token)` on a background task.
- Each event is pushed into a concurrent queue owned by `TerminalApp`.
- It signals the shared wake helper.
- `Tick()` drains that queue instead of depending on fixed-interval polling.

Important:

- This relay is a passive wake proxy only. It does not process UI events or touch UI state.
- All event handling still happens inside `Tick()` on the main thread.
- This relay is for the blocking host loop only.
- `BeginRun()` / `Tick()` used by deterministic tests remain synchronous and continue to operate without background waits.

This gives responsive input without requiring a 1 ms polling loop.

### Resize sourcing

Resize should continue to be treated as a regular terminal event from the app's point of view.

In the sibling `XenoAtom.Terminal` implementation, both Windows and Unix backends publish `TerminalResizeEvent` into
the same event stream consumed by `ReadEventAsync(...)`. Platform-specific detection remains a backend concern; the UI
loop only needs to wake when a resize event is enqueued.

## Fullscreen vs inline host considerations

The loop design applies to both fullscreen and inline interactive hosting, but inline mode has two practical
differences:

- inline mode does not own the full viewport in the same way as fullscreen,
- posted work may capture flow output into `_updateOutputBuilder` and flush it before the next render.

Therefore, the spec requires:

- wake-up signaling must preserve the existing `CaptureFlowOutput` behavior of `Post(...)`,
- inline mode must continue to flush captured output on the UI thread before rendering,
- the waiter design must not move any inline host rendering or flow-output flushing off-thread.

The input relay, if used, may still be shared because it only enqueues raw terminal events and wakes the UI thread.

## Async update callback handling

The current async update model stores `_pendingUpdateTask` and polls it each tick.

That should be improved in the same style:

- When `_pendingUpdateTask` is created, attach a continuation on `TaskScheduler.Default` that signals the shared wake
  helper when the task completes.
- If the async continuation resumes through `DispatcherSynchronizationContext`, `Dispatcher.Post(...)` already wakes the
  app; the completion continuation is still useful as a fallback for tasks that complete off-context.

This removes another source of pointless polling.

### `TerminalLoopResult` interaction

The wake-on-completion continuation is only responsible for making the UI loop observe task completion promptly.

It must **not** interpret the result off-thread.

The required flow is:

1. `_pendingUpdateTask` completes.
2. Its continuation only signals the wake helper.
3. The next `Tick()` runs on the UI thread.
4. `Tick()` reads the `TerminalLoopResult`.
5. If the result is `Stop` or `StopAndKeepVisual`, the existing shutdown behavior runs on the UI thread:
   - inline host decides whether to remove or keep the final visual,
   - binding writes are processed,
   - a final render is requested,
   - cancellation is triggered.

This keeps loop-exit behavior deterministic and single-threaded.

## Preferred default behavior

The default implementation should prioritize:

- prompt wake-up,
- low CPU usage,
- stable deadlines,
- simple code.

It should **not** rely on a permanent spin loop, but it may use a very small and adaptive `Thread.Yield()` tail when the
remaining time is below the effective resolution of the blocking wait backend.

For a TUI, the right default is:

- block efficiently for most of the interval,
- avoid `SpinWait`,
- use `Thread.Yield()` only in the final short interval where a blocking wait is likely to overshoot.

### Recommended cadence policy

The framework should not enforce a single universal frame cadence for all situations.

Recommended default policy:

- **active animation / active interaction**
  - target up to `15 ms` per frame (`~66.7 Hz`)
- **lower-priority / power-friendly animation**
  - target up to `33.333 ms` per frame (`~30 Hz`)
- **idle**
  - no frame cadence target; fully event-driven

This should be interpreted as a ceiling for framework-driven pacing, not as a guarantee that every frame is rendered at
those intervals.

The active cadence budget applies to the whole loop iteration, not only to the wait after user code completes. If
`onUpdate`, layout, or rendering already consumed part of the `15 ms` budget, the remaining wait should shrink
accordingly.

These values are soft timing targets only.

If user code, layout, rendering, terminal I/O, or OS scheduling causes a tick to take longer than `15 ms` or
`33.333 ms`, the loop must continue correctly:

- the app remains responsive and correct,
- the loop simply misses the smoothness target for that period,
- no secondary timing mechanism should "break" the UI loop in an attempt to catch up.

Why this is the recommended default:

- a nominal `15 ms` cap gives the loop a small amount of headroom so real-world wait overshoot still tends to land
  closer to the user-visible `~60-61 FPS` range.
- `~30 Hz` is often sufficient for spinners, progress indicators, and less critical motion while reducing wake-ups.
- idle mode should not spend wake-ups chasing a cadence when nothing is changing.

### Prefer visual deadlines over a global frame loop

The codebase already has a stronger primitive than a blanket FPS loop: `IAnimatedVisual.NextAnimationTick`.

That should remain the primary source of timing for animation:

- if a visual knows its next desired tick, the loop should honor that absolute deadline,
- a global active cadence should only act as a cap or fallback,
- the framework should not coerce all animations into a fixed 60 FPS heartbeat if their own deadlines are slower or more precise.

This keeps the loop both more efficient and more correct:

- a spinner that advances every 80 ms should not wake at 15 ms just because the app is "animated",
- a hover or drag interaction may still benefit from the active default cap,
- the loop remains event-driven when no visual is asking for time-based advancement.

### Overrun behavior

The loop must tolerate overruns gracefully.

Examples of overruns:

- a user `onUpdate` callback takes longer than the target cadence,
- layout or render work takes longer than expected,
- the terminal backend or scheduler wakes late.

Required behavior:

- the current tick is allowed to complete normally on the UI thread,
- the next wake deadline is recomputed from the current state,
- missed cadence targets degrade visual smoothness only,
- the loop must not queue multiple pending renders just to "catch up",
- the loop must not attempt to replay missed intermediate visual frames.

In practice, this means:

- if one 60 Hz interval is missed, the app may effectively render at a lower rate for that period,
- animations should advance based on absolute time / absolute deadlines so they remain logically correct even when frames
  are skipped,
- responsiveness and correctness take priority over trying to preserve every intermediate frame.

### Two-phase wait policy

For V1:

- do not use `Thread.SpinWait()` in the regular loop,
- do not use an unbounded tight `Thread.Yield()` loop,
- do use a bounded `Thread.Yield()` phase for the final short interval near the deadline.

This is a better fit than `SpinWait` for Terminal.UI:

- `SpinWait` burns CPU cycles continuously.
- `Thread.Yield()` gives the scheduler a chance to run another ready thread and does not force a busy spin.
- A blocking wait remains the primary mechanism, so CPU usage stays low.

### Adaptive yield window

The yield phase should not use a hardcoded constant only. It should be derived from the observed behavior of the current
backend.

Recommended internal state:

- keep a rolling window or EWMA of coarse-wait overshoot:
  - `overshoot = actualWakeTimestamp - requestedDeadline`
- maintain this per run, and optionally per backend type

Recommended derived values:

- `effectiveWaitResolution`
  - based on the recent average or EWMA overshoot of timed waits
- `yieldWindow`
  - `max(minYieldWindow, effectiveWaitResolution)`
  - clamped to a sane upper bound

Reasonable starting bounds:

- `minYieldWindow = 250us`
- `maxYieldWindow = 2ms`

Bootstrap rule:

- before enough overshoot samples exist, start from `maxYieldWindow` and converge downward.

This is preferable to starting too small because early frames should fail safe toward responsiveness rather than toward
coarse overshoot.

This keeps the system simple:

- when timed waits are accurate, the yield window stays tiny,
- when timed waits are coarse, the final yield window grows automatically,
- the framework does not need a platform-specific constant tuned by guesswork alone.

### Yield-loop behavior

When `remaining <= yieldWindow`, the waiter should stop scheduling another blocking wait and enter a bounded yield phase:

```csharp
while (Stopwatch.GetTimestamp() < deadline)
{
    if (_wakeSignal.WaitOne(0))
    {
        break;
    }

    Thread.Yield();
}
```

Policy notes:

- this phase must remain small because it only begins inside the adaptive `yieldWindow`,
- it must check the wake signal so posted work/input is not delayed behind the pacing deadline,
- no `SpinWait` fallback is used.

If profiling later shows that some platforms need a more explicit bound, the implementation can cap consecutive yields or
fall back to `WaitOne(0)` plus loop exit once the deadline is very near. That refinement is optional; the key rule is
that the yield phase stays short and adaptive.

## Windows wait backend

On Windows 10 version 1803 and later, the framework should prefer a reusable waitable timer created with
`CREATE_WAITABLE_TIMER_HIGH_RESOLUTION`.

Recommended shape:

- create one timer handle for the lifetime of the blocking run,
- use a one-shot relative due time for each wait,
- wait on both the timer and `_wakeSignal`,
- dispose the handle in `EndRunCore()`.

Implementation notes:

- Try `CreateWaitableTimerExW(..., CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, ...)`.
- If that fails because the flag is unsupported, fall back to a normal waitable timer or timeout-based wait.
- Use `SetWaitableTimerEx` or `SetWaitableTimer` with a relative due time computed from the remaining `Stopwatch` ticks.
- Use `WaitForMultipleObjects` or `WaitForMultipleObjectsEx` to wait on the timer handle and `_wakeSignal` together.
- Use zero tolerable delay for the pacing path.
- Do not raise global timer resolution.

### Interop / AOT note

Per project guidance, the Windows interop surface should remain AOT/trimmer-friendly.

Recommended implementation shape:

- use `LibraryImport`-based source-generated P/Invoke declarations,
- keep the wait backend isolated in a small internal Windows-specific helper,
- avoid reflection or dynamic interop setup.

Why this path is recommended:

- it stays local to the process,
- it is interruptible,
- it gives much better short-wait behavior than `Sleep(1)` on supported Windows versions,
- combined with a final adaptive `Thread.Yield()` window, it avoids both coarse overshoot and `SpinWait`.

## Non-Windows wait backend

On macOS/Linux, the implementation can stay simpler:

- compute the remaining time from the absolute deadline,
- block on `_wakeSignal` with that timeout,
- wake immediately when the signal is set.

This is expected to behave acceptably on Unix-like systems.

The same adaptive final-yield policy should still apply:

- block while outside the yield window,
- use `Thread.Yield()` only in the final short interval,
- never use `SpinWait` by default.

## Observability and debug overlay

The loop change should be measurable.

`DebugOverlayMetrics` already tracks per-tick and per-render timings. The new loop should extend observability with:

- requested deadline timestamp,
- actual wake timestamp,
- wake latency / overshoot,
- coarse-wait duration,
- yield-phase duration,
- yield count,
- wake reason.

These metrics are useful for:

- validating the adaptive yield window,
- comparing Windows high-resolution timers against fallback waits,
- diagnosing jitter or unexpectedly hot loops.

### Overlay vs diagnostics

Not every internal timing datum belongs in the real-time debug overlay.

At terminal refresh/update rates, a human can only read stable, aggregated indicators. Per-wake values such as the last
exact wake reason or the last raw overshoot are too noisy and change too quickly to be meaningful on-screen.

Therefore the spec distinguishes two observability layers:

- **overlay metrics**
  - human-readable, stable over a short rolling window,
  - intended for live inspection during app execution.
- **diagnostic metrics**
  - higher-frequency internal counters / last-sample values,
  - intended for logging, tracing, tests, or deeper debugging.

### Recommended overlay metrics

The real-time overlay should favor a few aggregated values over many rapidly changing raw samples.

Recommended display metrics:

- loop mode
  - `Auto` / `Polling`
- target wait backend
  - `WindowsHighResolutionTimer`, `WindowsWaitableTimer`, `TimeoutWait`
- average wake overshoot over a rolling window
  - for example EWMA or last 32-128 samples
- p95 wake overshoot over a rolling window
  - optional, but more useful than a raw "last wake reason"
- average coarse-wait duration
- average yield duration
- average yields per wake
- percentage of wakes by category over a rolling window
  - input
  - animation
  - render request
  - async completion
  - timeout/deadline
- catch-up / resync count
  - per second or over the current rolling window

These are readable because they answer human questions like:

- "Is the loop overshooting badly?"
- "Are we mostly waking because of input or because of polling timeouts?"
- "Is the yield tail constantly active?"
- "Are we repeatedly missing deadlines and resynchronizing?"

### Metrics that should stay out of the overlay by default

The following are still useful internally, but should not be shown prominently in the live overlay unless a deeper
diagnostic mode is enabled:

- exact last wake reason,
- exact requested deadline timestamp,
- exact actual wake timestamp,
- exact last overshoot sample,
- exact last yield count.

Those values are too volatile to be reliably read by a human and tend to create visual noise rather than insight.

### Suggested presentation

For the default overlay, present loop metrics as a compact "health summary", for example:

- backend
- avg/p95 overshoot
- avg yield time
- wake distribution
- resync count

If a deeper diagnostic mode is added later, it can expose raw last-sample values and more detailed counters separately
without crowding the default overlay.

## Public API direction

The current public option is:

- `UpdateWaitDuration`

That property describes a polling sleep interval, which is no longer the right mental model once the loop becomes
deadline-based and event-driven.

Recommended API direction:

- keep `UpdateWaitDuration` for backward compatibility,
- introduce a new loop mode concept so the default can be simple and correct.

Suggested shape:

```csharp
public enum TerminalLoopMode
{
    Auto,    // default
    Polling, // legacy behavior
}
```

Behavior:

- `Auto`:
  - event-driven idle,
  - animation-driven deadlines,
  - wake on input / posted work / render requests / async completion,
  - `UpdateWaitDuration` is not used as a frame cadence control.
- `Polling`:
  - preserves periodic update heartbeats,
  - still schedules against absolute deadlines,
  - uses `UpdateWaitDuration` as the maximum coarse wait slice between re-evaluations,
  - uses the new waiter instead of raw `Thread.Sleep`.

Why this split is useful:

- It gives good defaults for new apps.
- It preserves an escape hatch for existing apps that intentionally depend on free-running `onUpdate` calls.

This enum intentionally does **not** model the deterministic `BeginRun()` / `Tick()` test path. Manual test stepping
stays as an internal explicit mode outside the public host-loop options.

## Compatibility notes

Some current samples increment state once per `onUpdate` call. Those samples implicitly depend on a polling loop.

Once `Auto` becomes the default, those scenarios should be rewritten to use one of:

- `IAnimatedVisual` with explicit next-tick scheduling,
- time-based logic using `TerminalRunningContext.Timestamp`,
- async update callbacks that await real work or delays,
- `TerminalLoopMode.Polling` for legacy behavior.

This is a worthwhile change because "one increment per host tick" is not a stable contract across machines anyway.

## Recommended implementation stages

### Stage 1

- Introduce an internal wait abstraction for blocking runs.
- Replace raw `Thread.Sleep(...)` with absolute-deadline waiting.
- Add the Windows high-resolution waitable timer backend with graceful fallback.
- Keep the main UI loop synchronous.

### Stage 2

- Add the background input relay for `Run()` / `RunAsync()`.
- Add wake-on-completion for `_pendingUpdateTask`.
- Make blocking runs event-driven when idle.

### Stage 3

- Introduce `TerminalLoopMode.Auto` publicly.
- Update docs and samples away from tick-count-based update logic.
- Consider obsoleting `UpdateWaitDuration` once the new model is established.

## Testing expectations

The implementation should be covered by tests for:

- absolute-deadline scheduling math,
- wake-up on `Post(...)`,
- wake-up on `RequestRender()` / `RequestAnimation()`,
- wake-up on async update completion,
- Windows high-resolution timer fallback when unsupported,
- `BeginRun()` / `Tick()` behavior remaining deterministic and sleep-free.

Where practical, timing math should be tested behind small abstractions rather than by depending on real wall-clock
delays.

Recommended abstractions:

- `ITimeSource`
  - exposes `GetTimestamp()` and `Frequency`
- `IWaitBackend`
  - exposes `WaitUntil(deadline, wakeReasonToken, cancellationToken)`

That enables tests to:

- advance time deterministically,
- simulate timer overshoot,
- force wake reasons in a controlled order,
- verify catch-up and yield-window logic without sleeping the test process.

## Recommendation

Adopt a deadline-based, interruptible host loop with a two-phase wait and no default busy-spin behavior.

Concretely:

- keep `Stopwatch` as the canonical clock,
- use an interruptible waiter instead of `Thread.Sleep(...)`,
- add an adaptive final `Thread.Yield()` window based on observed wait accuracy,
- use a Windows high-resolution waitable timer when available,
- make blocking runs event-driven for input and async completions,
- keep a legacy polling mode only for compatibility.

This is simple enough to maintain, materially improves Windows responsiveness, and gives Terminal.UI better defaults for
animations, resize handling, and general UI responsiveness.

## References

- [CreateWaitableTimerExW](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-createwaitabletimerexw)
- [SetWaitableTimerEx](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-setwaitabletimerex)
- [WaitForMultipleObjects](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitformultipleobjects)
- [Sleep](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-sleep)
- [Stopwatch.IsHighResolution](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch.ishighresolution)
- [Thread.Yield](https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.yield)
- [P/Invoke source generation / LibraryImport](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation)
- [How to: Use SpinWait to Implement a Two-Phase Wait Operation](https://learn.microsoft.com/en-us/dotnet/standard/threading/how-to-use-spinwait-to-implement-a-two-phase-wait-operation)
