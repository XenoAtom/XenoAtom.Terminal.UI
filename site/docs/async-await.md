---
title: "Async & Await"
---

# Async & Await

XenoAtom.Terminal.UI supports `async` / `await`, but only in specific places and with a specific threading model.

The short version:

- `Terminal.LiveAsync(...)` and `Terminal.RunAsync(...)` support an asynchronous `onUpdate` callback.
- The UI is still single-threaded. Terminal.UI does not create a dedicated UI thread.
- Terminal.UI installs a dispatcher-backed `SynchronizationContext` while the app is running.
- If an awaited operation resumes on the captured context, the continuation runs back on the UI thread.
- Routed event handlers such as `Click(...)` are synchronous. Treat `async void` handlers as unsupported.

If you keep that model in mind, the rest of the behavior is predictable.

## Mental model

Think of Terminal.UI as a single-threaded UI loop with an optional asynchronous update coroutine.

- `Terminal.Run(...)` / `Terminal.Live(...)` run the host loop on the calling thread.
- `Terminal.RunAsync(...)` / `Terminal.LiveAsync(...)` do not move the loop to another thread.
- The async part is the `onUpdate` callback, not the existence of a background UI loop.

While a hosted app is running, Terminal.UI binds `SynchronizationContext.Current` to its dispatcher. That means a normal:

```csharp
await SomeOperationAsync();
```

inside the async update callback captures the UI context by default and resumes there later.

## What happens after `await`

When the async update callback reaches an incomplete `await`:

1. The callback returns an incomplete task to the host loop.
2. Terminal.UI keeps processing input, animations, binding writes, and rendering while that task is pending.
3. When the awaited operation completes, the continuation is posted back to the dispatcher-backed synchronization context.
4. The continuation runs on the UI thread during a later loop iteration.

That "later loop iteration" detail matters:

- if the awaited task already completed, the callback may continue synchronously in the same tick;
- if it did not complete, resumption is effectively deferred to a later UI tick.

There is only one hosted async update callback in flight at a time. Terminal.UI will not invoke `onUpdate` again until the current async update task has completed.

## What stays responsive while awaiting

Awaiting in `onUpdate` does not freeze the app. While the callback task is pending, Terminal.UI still:

- processes terminal input;
- dispatches routed events;
- advances animations;
- applies bindable writes;
- renders when something invalidates the UI.

What pauses is only the next `onUpdate` invocation. That callback behaves like a cooperative coroutine, not like a re-entrant timer.

## UI thread rules

Most UI-facing types in Terminal.UI are dispatcher-bound:

- `Visual`
- `State<T>`
- `TerminalApp`
- other `DispatcherObject`-derived types

They should be read and written on the UI thread.

If you update bound state from the wrong thread, Terminal.UI will reject it. For example, `State<T>.Value` writes are validated through the binding system and are expected to happen on the UI thread.

Use the dispatcher when background work needs to publish results to the UI:

```csharp
await app.Dispatcher.InvokeAsync(() =>
{
    result.Value = fetchedText;
    isLoading.Value = false;
});
```

Use:

- `Dispatcher.Post(...)` for fire-and-forget work queued to the UI thread;
- `Dispatcher.InvokeAsync(...)` when background code must await completion of a UI-thread update.

## `ConfigureAwait(false)` guidance

Inside the async update callback, default `await` behavior is usually what you want:

```csharp
await SomeOperationAsync();
status.Value = "Done"; // back on the UI thread
```

If you write this instead:

```csharp
await SomeOperationAsync().ConfigureAwait(false);
status.Value = "Done";
```

the continuation may run on a thread-pool thread. At that point, touching `status.Value`, a `Visual`, or other UI state is no longer safe.

Guidance:

- Do not use `ConfigureAwait(false)` in `onUpdate` if the continuation needs to touch UI state.
- Use `ConfigureAwait(false)` only for purely background continuation work.
- If you do use it, marshal back before changing UI state:

```csharp
var text = await client.GetStringAsync(uri).ConfigureAwait(false);
await context.App.Dispatcher.InvokeAsync(() => status.Value = text);
```

The same rule applies to any background `Task.Run(...)`, timer callback, or library callback: marshal back before mutating UI state.

## Recommended patterns

### Pattern 1: await directly in `onUpdate`

Use this when the hosted app itself is naturally driven by an async workflow:

```csharp
var progress = new State<double>(0);

await Terminal.LiveAsync(
    new ProgressBar().Value(progress),
    async _ =>
    {
        await Task.Delay(50);

        progress.Value = Math.Min(1.0, progress.Value + 0.05);
        return progress.Value >= 1.0
            ? TerminalLoopResult.StopAndKeepVisual
            : TerminalLoopResult.Continue;
    });
```

This is the simplest pattern, and it keeps the continuation on the UI thread as long as you do not opt out with `ConfigureAwait(false)`.

### Pattern 2: trigger async work from a synchronous event

Routed events such as `Click(...)` are synchronous, so trigger intent there and perform the `await` in `onUpdate`.

```csharp
var shouldLoad = new State<bool>(false);
var isLoading = new State<bool>(false);
var text = new State<string>("Idle");

var button = new Button("Load").Click(() =>
{
    if (!isLoading.Value)
    {
        shouldLoad.Value = true;
    }
});

await Terminal.RunAsync(
    new VStack(button, new TextBlock().Text(() => text.Value)),
    async _ =>
    {
        if (!shouldLoad.Value)
        {
            return TerminalLoopResult.Continue;
        }

        shouldLoad.Value = false;
        isLoading.Value = true;
        text.Value = "Loading...";

        var loaded = await LoadTextAsync();

        text.Value = loaded;
        isLoading.Value = false;
        return TerminalLoopResult.Continue;
    });
```

This keeps event handling synchronous while still allowing the real async work to live in the supported async host callback.

### Pattern 3: run background work and publish results through the dispatcher

Use this when the operation should run independently of the hosted update coroutine:

```csharp
var isLoading = new State<bool>(false);
var text = new State<string>("Idle");
var button = new Button("Load");

button.Click(() =>
{
    if (isLoading.Value)
    {
        return;
    }

    isLoading.Value = true;
    text.Value = "Loading...";

    _ = Task.Run(async () =>
    {
        try
        {
            var loaded = await LoadTextAsync().ConfigureAwait(false);
            await Dispatcher.Current.InvokeAsync(() =>
            {
                text.Value = loaded;
                isLoading.Value = false;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.Current.InvokeAsync(() =>
            {
                text.Value = ex.Message;
                isLoading.Value = false;
            });
        }
    });
});
```

This pattern is appropriate when a click starts background work that should not occupy the hosted async update callback.

## Limitations

The current async support has explicit limits:

- Only the hosting update callback is async-aware.
- Routed event callbacks are synchronous.
- There is only one in-flight async `onUpdate` callback per hosted app.
- UI state still has thread affinity.

Practical implications:

- avoid `async void` event handlers;
- avoid blocking calls such as `.Result`, `.Wait()`, or long synchronous I/O on the UI thread;
- if you need concurrent background work, keep it off the UI thread and marshal results back.

## Event callback guidance

This is supported:

```csharp
button.Click(() => counter.Value++);
```

This is technically possible in C#, but not a supported Terminal.UI pattern:

```csharp
button.Click(async () =>
{
    await Task.Delay(100);
    counter.Value++;
});
```

Why it is discouraged:

- `Click(...)` is a synchronous routed event API;
- the routing system does not await handler completion;
- exceptions from `async void` do not integrate cleanly with the host loop;
- thread-affinity mistakes become easy to make.

Prefer one of the patterns above instead.

## Exceptions and cancellation

If the async update callback throws, the hosted run fails when Terminal.UI observes that task completion.

If the host cancellation token is canceled, the app stops on the UI thread just like the synchronous hosting APIs.

Keep cancellation explicit in long-running async work and propagate it to the operations you start.

## Best practices

- Use `LiveAsync` / `RunAsync` when the host update itself needs to await.
- Keep normal `await` behavior inside `onUpdate` unless you intentionally want to leave the UI context.
- Treat `State<T>` and visuals as UI-thread-bound objects.
- Use `Dispatcher.InvokeAsync(...)` or `Dispatcher.Post(...)` to publish background results back to the UI.
- Use synchronous event handlers to express intent, then perform awaited work in `onUpdate` or in background tasks that marshal back to the dispatcher.
- Do not assume `onUpdate` is re-entrant; one async update callback runs at a time.

## See also

- [Hosting](hosting.md)
- [Binding](binding.md)
- [Input](input.md)
