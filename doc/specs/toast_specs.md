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

### 1.1 Fullscreen overlay layering (`WindowLayer`)

In fullscreen mode, `TerminalApp` wraps the user root into a `WindowLayer` (unless the root already is a `WindowLayer`).
That gives the framework a place to host:

- dialogs,
- popups / context menus,
- tooltips.

Toasts should be layered as:

1) **Above the app content**
2) **Below any windows** (dialogs, popups, context menus)

The most idiomatic way to achieve this in the current architecture is to place the toast overlay **inside**
`WindowLayer.Content` (i.e. within the normal visual tree), rather than adding each toast as a window. That ensures:

- dialog/popup windows always appear above toasts,
- toasts participate in normal theme/style resolution,
- toasts don’t require special-case window focus rules.

In practice, this is implemented by a `ToastHost` that wraps the app content and overlays a toast layer above it
(typically via a `ZStack` internally).

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
public sealed partial class ToastHost : ContentVisual, IAnimatedVisual
```

The `ToastHost` is a container that manages the lifecycle and positioning of multiple toasts.

It wraps a normal visual tree via `Content` (inherited from `ContentVisual`) and overlays a toast layer above it.
A typical app has a single `ToastHost` at the root level:

```csharp
var root = new ToastHost(
    new DockLayout()
        .Top(new Header().Title("My App"))
        .Content(mainContent)
        .Bottom(new Footer()));

Terminal.Run(root);
```

#### Bindable properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Position` | `ToastPosition` | `TopRight` | Where toasts appear |
| `MaxVisible` | `int` | `5` | Maximum simultaneous toasts |
| `Spacing` | `int` | `1` | Gap between stacked toasts |
| `Inset` | `Thickness` | `(1,1,1,1)` | Inset from viewport edges (applies to the toast overlay) |
| `DefaultDuration` | `TimeSpan` | `3 seconds` | Default duration for toasts |
| `PauseOnHover` | `bool` | `true` | Pause timer when mouse hovers |

#### Methods

```csharp
/// <summary>Shows a toast and returns it for further manipulation.</summary>
public Toast Show(Visual content, ToastSeverity severity = ToastSeverity.Info);

/// <summary>Shows a toast with full configuration.</summary>
public Toast Show(Func<Toast> build);

/// <summary>Dismisses all visible toasts.</summary>
public void DismissAll();

/// <summary>Dismisses toasts matching a predicate.</summary>
public void Dismiss(Func<Toast, bool> predicate);
```

### 2.5 Fluent configuration (no builder)

This framework already provides fluent APIs via `[Bindable]` properties and source generation, so a dedicated
`ToastBuilder` type is not required.

Prefer:

- `toastHost.Show(() => new Toast()... )`
- `ToastService.Show(() => new Toast()... )`

### 2.6 ToastService (static/ambient API)

For convenience, provide a static service that resolves the `ToastHost` from the current app context:

```csharp
public static class ToastService
{
    /// <summary>Shows a simple text toast.</summary>
    public static Toast? Show(string message, ToastSeverity severity = ToastSeverity.Info);
    
    /// <summary>Shows a toast with configuration.</summary>
    public static Toast? Show(Func<Toast> build);
    
    /// <summary>Convenience methods for common severities.</summary>
    public static Toast? Info(string message);
    public static Toast? Success(string message);
    public static Toast? Warning(string message);
    public static Toast? Error(string message);
}
```

The service resolves `ToastHost` via:
- `Dispatcher.Current.AttachedApp` (fullscreen or inline host)
- A visual-tree search for a `ToastHost` (typically there is exactly one at the root)
- Returns `null` if no host is found (no-op in inline mode or if host not configured)

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

`ToastHost` is a **content wrapper**. Internally it contains two children:

1) The wrapped `Content` (normal app UI)
2) A toast overlay layer (a private/internal visual)

The internal structure is typically:

```
ToastHost
  └─ ZStack
      ├─ Content
      └─ ToastLayer (sized to visible toasts, aligned to corner)
```

#### Hit-testing note (important)

The current hit testing model is:

- `Visual.HitTest` returns `this` when the point is within bounds and no child hit succeeds.
- `IsHitTestVisible = false` disables hit testing for both a visual and its children.

That means a fullscreen “overlay” visual that is arranged to the entire viewport will *steal* pointer input even if
it renders nothing in most of that area.

To keep toasts non-intrusive, the toast overlay layer MUST be arranged only to the bounding rectangle of its visible
toasts (plus `Inset`). It should not cover the full viewport unless the framework adds a “hit-test pass-through”
feature in the future.

The `ToastHost` MUST:

1. Arrange the wrapped `Content` normally.
2. Overlay toasts **without affecting the layout** of the wrapped content.
3. Position toasts relative to the viewport using `Position` + `Inset`.
4. Stack toasts in the appropriate direction:
   - `TopRight/TopLeft/TopCenter`: stack downward (newest on top or bottom, configurable)
   - `BottomRight/BottomLeft/BottomCenter`: stack upward

#### Measure

`ToastHost` MUST ensure the toast overlay is measured so it can compute the toast stack size, but the host should
return size hints compatible with the wrapped `Content` (the toast overlay must not make the app “bigger”).

In practice:

- measure `Content` with the provided constraints (normal behavior),
- measure the toast overlay with bounded width and unbounded height,
- return `SizeHints` derived from `Content`.

#### Arrange

`ToastHost` arranges `Content` to the full available rectangle, then arranges the toast overlay.

The toast overlay should:

- compute its own `DesiredSize` from visible toasts,
- be arranged via normal alignment (Start/Center/End) to reach the chosen corner,
- apply `Inset` by shrinking the alignment slot before placing the overlay.

Avoid positioning toasts using raw terminal coordinates unless strictly needed; prefer the normal layout protocol so
the behavior stays consistent across fullscreen and inline hosts.

```csharp
// Pseudocode sketch (ToastLayer.ArrangeCore)
// - ToastLayer has an arranged rect that is already aligned to the chosen corner.
// - Then it stacks children inside that rect.
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
- For v1, close/action visuals SHOULD be pointer-driven and not participate in normal tab traversal by default.
  Advanced scenarios can opt into focus by providing focusable action content explicitly.

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
    
    // Container appearance
    // Prefer reusing Border/Group styles rather than duplicating border rendering.
    public BorderStyle BorderStyle { get; init; } = BorderStyle.Rounded;
    
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
var root = new ToastHost(
        new DockLayout()
            .Top(new Header().Title("My App"))
            .Content(mainContent)
            .Bottom(new Footer()))
    .Position(ToastPosition.TopRight)
    .MaxVisible(3);

Terminal.Run(root);
```

### 7.2 Showing toasts

```csharp
// Simple API
ToastService.Success("File saved successfully!");
ToastService.Error("Connection failed. Retrying...");

// Full configuration
ToastService.Show(() => new Toast()
    .Title("Update Available")
    .Content("Version 2.0 is ready to install.")
    .Severity(ToastSeverity.Info)
    .Duration(TimeSpan.FromSeconds(10))
    .ShowProgress(true)
    .Action(new Button("Install Now").Click(StartUpdate)));

// Direct host access
var toast = toastHost.Show("Processing...", ToastSeverity.Info);
// Later...
toast.Dismiss();
```

### 7.3 Persistent notifications

```csharp
// For ongoing operations
var toast = ToastService.Show(() => new Toast()
    .Content(new HStack(new Spinner(), "Uploading...").Spacing(1))
    .Duration(null)
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
