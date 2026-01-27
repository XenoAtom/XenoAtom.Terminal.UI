# Toast / Notification Control Specs

This document specifies a **toast/notification** system for XenoAtom.Terminal.UI:

- `Toast` — the individual notification visual
- `ToastHost` — the container that manages toast positioning and lifecycle
- `ToastService` — the API for showing toasts from anywhere in the app

The goal is to provide non-blocking, auto-dismissing feedback messages that don't interrupt user workflow — a modern UX pattern missing from most terminal UI frameworks.

Design goals:

- **Idiomatic to this framework**: retained visuals, `[Bindable]` properties, routed events, automatic dependency tracking.
- **Layout-protocol compliant**: toasts participate in normal layout within their host region.
- **Non-intrusive**: toasts overlay content without stealing focus or blocking input to underlying controls.
- **Allocation-conscious**: reuse toast visuals where possible; avoid per-frame allocations for animations.

---

## 1. Prerequisites (already in the codebase)

### 1.1 WindowLayer

`WindowLayer` already provides overlay management for dialogs and popups. Toast host SHOULD integrate with this system, appearing above normal content but potentially below modal dialogs.

### 1.2 Animation infrastructure

`IAnimatedVisual` and `TerminalApp.AdvanceAnimations` provide tick-based animation. Toasts MAY use this for entrance/exit effects and progress indicators.

### 1.3 Theme and styling

Toasts MUST use the existing `Theme` system for colors (Primary, Success, Warning, Error) and should follow the established style record pattern.

---

## 2. Public API

### 2.1 Toast severity

```csharp
public enum ToastSeverity
{
    /// <summary>Neutral informational message.</summary>
    Info,
    
    /// <summary>Successful operation feedback.</summary>
    Success,
    
    /// <summary>Warning that doesn't block operation.</summary>
    Warning,
    
    /// <summary>Error notification (non-fatal).</summary>
    Error,
}
```

### 2.2 Toast position

```csharp
public enum ToastPosition
{
    /// <summary>Top-right corner (default).</summary>
    TopRight,
    
    /// <summary>Top-left corner.</summary>
    TopLeft,
    
    /// <summary>Top-center.</summary>
    TopCenter,
    
    /// <summary>Bottom-right corner.</summary>
    BottomRight,
    
    /// <summary>Bottom-left corner.</summary>
    BottomLeft,
    
    /// <summary>Bottom-center.</summary>
    BottomCenter,
}
```

### 2.3 Toast control

```csharp
public sealed partial class Toast : Visual
```

The `Toast` visual represents a single notification. It is typically created and managed by `ToastHost`, but MAY be used standalone in custom scenarios.

#### Bindable properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `Visual?` | `null` | Optional title/header visual |
| `Content` | `Visual?` | `null` | Main message content (required) |
| `Severity` | `ToastSeverity` | `Info` | Determines styling and icon |
| `Duration` | `TimeSpan?` | `3 seconds` | Auto-dismiss delay; `null` = persistent |
| `ShowIcon` | `bool` | `true` | Whether to show severity icon |
| `ShowCloseButton` | `bool` | `true` | Whether to show manual dismiss button |
| `ShowProgress` | `bool` | `false` | Whether to show countdown progress bar |
| `Action` | `Visual?` | `null` | Optional action button/link |

#### Routed events

```csharp
[RoutedEvent(RoutingStrategy.Bubble)]
protected virtual void OnDismissed(ToastDismissedEventArgs e);

[RoutedEvent(RoutingStrategy.Bubble)]
protected virtual void OnActionInvoked(ToastActionEventArgs e);
```

#### Methods

```csharp
/// <summary>Dismisses the toast programmatically.</summary>
public void Dismiss();

/// <summary>Resets the auto-dismiss timer (e.g., on hover).</summary>
public void ResetTimer();

/// <summary>Pauses the auto-dismiss timer.</summary>
public void PauseTimer();

/// <summary>Resumes the auto-dismiss timer.</summary>
public void ResumeTimer();
```

### 2.4 ToastHost control

```csharp
public sealed partial class ToastHost : Visual
```

The `ToastHost` is a container that manages the lifecycle and positioning of multiple toasts. A typical app has one `ToastHost` at the root level (often inside `WindowLayer`).

#### Bindable properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Position` | `ToastPosition` | `TopRight` | Where toasts appear |
| `MaxVisible` | `int` | `5` | Maximum simultaneous toasts |
| `Spacing` | `int` | `1` | Gap between stacked toasts |
| `Margin` | `Thickness` | `(1,1,1,1)` | Margin from screen edges |
| `DefaultDuration` | `TimeSpan` | `3 seconds` | Default duration for toasts |
| `PauseOnHover` | `bool` | `true` | Pause timer when mouse hovers |

#### Methods

```csharp
/// <summary>Shows a toast and returns it for further manipulation.</summary>
public Toast Show(Visual content, ToastSeverity severity = ToastSeverity.Info);

/// <summary>Shows a toast with full configuration.</summary>
public Toast Show(Action<ToastBuilder> configure);

/// <summary>Dismisses all visible toasts.</summary>
public void DismissAll();

/// <summary>Dismisses toasts matching a predicate.</summary>
public void Dismiss(Func<Toast, bool> predicate);
```

### 2.5 ToastBuilder (fluent configuration)

```csharp
public sealed class ToastBuilder
{
    public ToastBuilder Title(Visual title);
    public ToastBuilder Title(string title);
    public ToastBuilder Content(Visual content);
    public ToastBuilder Content(string message);
    public ToastBuilder Severity(ToastSeverity severity);
    public ToastBuilder Duration(TimeSpan duration);
    public ToastBuilder Persistent();  // Duration = null
    public ToastBuilder ShowIcon(bool show = true);
    public ToastBuilder ShowCloseButton(bool show = true);
    public ToastBuilder ShowProgress(bool show = true);
    public ToastBuilder Action(string label, Action callback);
    public ToastBuilder Action(Visual content, Action callback);
    public ToastBuilder OnDismissed(Action<ToastDismissedEventArgs> handler);
}
```

### 2.6 ToastService (static/ambient API)

For convenience, provide a static service that resolves the `ToastHost` from the current app context:

```csharp
public static class ToastService
{
    /// <summary>Shows a simple text toast.</summary>
    public static Toast? Show(string message, ToastSeverity severity = ToastSeverity.Info);
    
    /// <summary>Shows a toast with configuration.</summary>
    public static Toast? Show(Action<ToastBuilder> configure);
    
    /// <summary>Convenience methods for common severities.</summary>
    public static Toast? Info(string message);
    public static Toast? Success(string message);
    public static Toast? Warning(string message);
    public static Toast? Error(string message);
}
```

The service resolves `ToastHost` via:
1. `Dispatcher.AttachedApp?.FindToastHost()` (searches visual tree)
2. Returns `null` if no host is found (no-op in inline mode or if host not configured)

---

## 3. Layout specification

### 3.1 Toast visual structure

A toast is composed of:

```
┌─────────────────────────────────────┐
│ [Icon] [Title................] [X]  │  ← Header row (optional)
│ [Content.........................]  │  ← Main content
│ [Action button]                     │  ← Action row (optional)
│ [▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░]  │  ← Progress bar (optional)
└─────────────────────────────────────┘
```

Layout SHOULD use existing controls internally:
- `HStack` for header row
- `VStack` for overall structure  
- `ProgressBar` (thin variant) for countdown
- `Button` for close/action

### 3.2 ToastHost layout

The `ToastHost` MUST:

1. **Not participate in normal content layout** — it overlays without affecting sibling arrangement
2. **Position toasts relative to viewport edges** based on `Position`
3. **Stack toasts** in the appropriate direction:
   - `TopRight/TopLeft/TopCenter`: stack downward (newest on top or bottom, configurable)
   - `BottomRight/BottomLeft/BottomCenter`: stack upward

#### Measure

`ToastHost` measures each visible toast with:
- WrapHStack: bounded width (viewport width minus margins), unbounded height
- Returns `SizeHints.Fixed(Size.Zero)` — host doesn't consume layout space

#### Arrange

`ToastHost` arranges itself to fill the available space but positions children absolutely:

```csharp
// Pseudocode for TopRight positioning
int x = finalRect.Right - margin.Right - toastWidth;
int y = finalRect.Top + margin.Top;

foreach (var toast in visibleToasts)
{
    toast.Arrange(new Rectangle(x, y, toastWidth, toastHeight));
    y += toastHeight + Spacing;
}
```

### 3.3 Toast sizing

Toasts SHOULD have:
- `MinWidth`: style-defined (e.g., 30 cells)
- `MaxWidth`: style-defined (e.g., 60 cells) or percentage of viewport
- Height: determined by content (auto)

---

## 4. Lifecycle and timing

### 4.1 Toast states

```
[Created] → [Entering] → [Visible] → [Exiting] → [Dismissed]
                ↑            │
                └────────────┘  (timer reset)
```

State transitions:
- **Created → Entering**: Toast added to host
- **Entering → Visible**: Entrance animation complete (or immediate if no animation)
- **Visible → Exiting**: Duration elapsed OR user dismissed OR programmatic dismiss
- **Exiting → Dismissed**: Exit animation complete; toast removed from host

### 4.2 Timer behavior

- Timer starts when toast enters `Visible` state
- Timer pauses on mouse hover (if `PauseOnHover` enabled)
- Timer resets if `ResetTimer()` called
- Timer ignored if `Duration` is `null` (persistent toast)

### 4.3 Animation (optional for v1)

Terminal animation is limited, but simple effects are possible:

- **Entrance**: Slide in from edge (1-2 frames) or instant appear
- **Exit**: Fade out (dim style) or instant disappear
- **Progress**: Smooth progress bar countdown

For v1, instant appear/disappear is acceptable. Animation can be added in v1.1.

### 4.4 Queue overflow

When `visibleToasts.Count >= MaxVisible`:

Option A (recommended): **Dismiss oldest** — auto-dismiss the oldest toast to make room
Option B: **Queue** — new toasts wait until a slot opens
Option C: **Reject** — `Show()` returns `null`, toast not displayed

Default SHOULD be Option A for best UX.

---

## 5. Input handling

### 5.1 Focus policy

Toasts MUST NOT steal focus from the current control. They are informational overlays.

- `Toast.Focusable = false` (default)
- Close button and action button ARE focusable if user tabs to them
- Tab navigation SHOULD skip toast host unless explicitly focused

### 5.2 Mouse interaction

- **Hover**: Pauses auto-dismiss timer (if `PauseOnHover`)
- **Click close button**: Dismisses toast
- **Click action button**: Invokes action, then optionally dismisses
- **Click elsewhere on toast**: No default action (configurable via event)

### 5.3 Keyboard interaction

When toast (or its buttons) is focused:
- `Escape`: Dismiss toast
- `Enter`/`Space` on action: Invoke action
- `Enter`/`Space` on close: Dismiss

Global shortcuts (optional, app-configurable):
- `Ctrl+Shift+N`: Focus toast area / cycle through toasts
- `Escape` (when toast focused): Dismiss and return focus

---

## 6. Styling

### 6.1 ToastStyle record

```csharp
public sealed record ToastStyle : IStyle<ToastStyle>
{
    public static StyleKey<ToastStyle> Key { get; }
    public static ToastStyle Default { get; }
    
    // Dimensions
    public int MinWidth { get; init; } = 30;
    public int MaxWidth { get; init; } = 60;
    public Thickness Padding { get; init; } = new(1);
    public int IconSpacing { get; init; } = 1;
    
    // Border
    public LineGlyphs Border { get; init; } = LineGlyphs.Rounded;
    public bool ShowBorder { get; init; } = true;
    
    // Icons per severity
    public Rune InfoIcon { get; init; } = new('ℹ');
    public Rune SuccessIcon { get; init; } = new('✓');
    public Rune WarningIcon { get; init; } = new('⚠');
    public Rune ErrorIcon { get; init; } = new('✗');
    
    // Close button
    public Rune CloseIcon { get; init; } = new('×');
    
    // Progress bar variant
    public ProgressBarStyle ProgressStyle { get; init; }
    
    // Resolve colors from theme
    public Style ResolveStyle(Theme theme, ToastSeverity severity);
    public Style ResolveTitleStyle(Theme theme, ToastSeverity severity);
    public Style ResolveIconStyle(Theme theme, ToastSeverity severity);
}
```

### 6.2 Severity-based theming

| Severity | Background | Border | Icon Color |
|----------|------------|--------|------------|
| Info | Surface | Border | Accent |
| Success | Surface | Success | Success |
| Warning | Surface | Warning | Warning |
| Error | Surface | Error | Error |

The exact colors come from `Theme` semantic tokens.

---

## 7. Integration patterns

### 7.1 Basic setup (fullscreen app)

```csharp
var toastHost = new ToastHost()
    .Position(ToastPosition.TopRight)
    .MaxVisible(3);

var root = new WindowLayer
{
    Content = new DockLayout()
        .Top(new Header { Title = "My App" })
        .Content(mainContent)
        .Bottom(new Footer()),
    
    // ToastHost is a window layer child (overlay)
}.Add(toastHost);

Terminal.Run(root, onUpdate: () => TerminalLoopResult.Continue);
```

### 7.2 Showing toasts

```csharp
// Simple API
ToastService.Success("File saved successfully!");
ToastService.Error("Connection failed. Retrying...");

// Full configuration
ToastService.Show(builder => builder
    .Title("Update Available")
    .Content("Version 2.0 is ready to install.")
    .Severity(ToastSeverity.Info)
    .Duration(TimeSpan.FromSeconds(10))
    .ShowProgress(true)
    .Action("Install Now", () => StartUpdate())
    .OnDismissed(e => Log($"Toast dismissed: {e.Reason}")));

// Direct host access
var toast = toastHost.Show("Processing...", ToastSeverity.Info);
// Later...
toast.Dismiss();
```

### 7.3 Persistent notifications

```csharp
// For ongoing operations
var toast = ToastService.Show(b => b
    .Content(new HStack(new Spinner(), "Uploading...").Spacing(1))
    .Persistent()
    .ShowCloseButton(false));

// When complete
await UploadAsync();
toast.Dismiss();
ToastService.Success("Upload complete!");
```

---

## 8. Event args

```csharp
public enum ToastDismissReason
{
    /// <summary>Auto-dismissed after duration elapsed.</summary>
    Timeout,
    
    /// <summary>User clicked close button.</summary>
    UserClosed,
    
    /// <summary>Dismissed programmatically via Dismiss().</summary>
    Programmatic,
    
    /// <summary>Dismissed to make room for new toasts.</summary>
    Overflow,
    
    /// <summary>Action button invoked (if configured to dismiss).</summary>
    ActionInvoked,
}

public sealed class ToastDismissedEventArgs : RoutedEventArgs
{
    public ToastDismissReason Reason { get; }
}

public sealed class ToastActionEventArgs : RoutedEventArgs
{
    /// <summary>Set to true to prevent auto-dismiss after action.</summary>
    public bool KeepOpen { get; set; }
}
```

---

## 9. Inline mode considerations

Toast notifications are primarily designed for fullscreen apps. In inline mode:

- `ToastService.Show()` returns `null` if no `ToastHost` is found
- Apps MAY fall back to `Terminal.WriteMarkupLine()` for inline feedback
- A future `InlineToastHost` could print toasts as flow output (lower priority)

---

## 10. Accessibility considerations

- Toasts SHOULD NOT convey critical information that requires user action (use dialogs for that)
- Screen reader integration is terminal-dependent; toasts should have meaningful text content
- `ShowProgress` provides visual indication of remaining time
- Keyboard-dismissable for users who can't use mouse

---

## 11. Implementation notes

### 11.1 Internal state

```csharp
internal sealed class ToastEntry
{
    public Toast Visual { get; }
    public ToastState State { get; set; }
    public long CreatedTick { get; }
    public long? DismissAtTick { get; set; }
    public bool TimerPaused { get; set; }
}
```

### 11.2 Animation ticks

`ToastHost` SHOULD implement `IAnimatedVisual` to:
- Track toast timers
- Trigger dismiss when duration elapses
- Update progress bars

### 11.3 Rendering order

Toasts render in stack order:
- Newest toasts appear at the "anchor" position
- Older toasts shift away from the edge
- This creates natural visual hierarchy

### 11.4 Memory management

- Dismissed toasts SHOULD be removed from the visual tree promptly
- Consider pooling `Toast` instances for high-frequency scenarios
- Avoid holding references to dismissed toasts in user code

---

## 12. Future extensions (post-v1)

- **Toast groups**: Group related toasts (e.g., "3 files saved")
- **Undo actions**: Built-in undo pattern (`ToastService.Success("Deleted", undo: () => Restore())`)
- **Custom positioning**: Anchor to specific visual instead of viewport
- **Sound/bell**: Optional terminal bell on certain severities
- **Inline mode support**: Print toasts as flow output in live regions
- **Toast templates**: Pre-configured toast types (download progress, error with retry, etc.)
