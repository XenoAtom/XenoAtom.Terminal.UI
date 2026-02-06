---
title: "Input, Focus & Events"
---

# Input, Focus & Events

XenoAtom.Terminal.UI is a retained-mode UI framework built on the unified input event stream of XenoAtom.Terminal:

- keys and text input
- mouse move/click/wheel
- resize events

This page explains how input is dispatched to visuals, how to handle it in custom controls, and how the routed event system works.

> [!TIP]
> The best way to learn the patterns is to read `src/XenoAtom.Terminal.UI/Visual.cs` (routing) and a few controls like `src/XenoAtom.Terminal.UI/Controls/Button.cs`.

## Input pipeline (high level)

At runtime, `TerminalApp` converts raw terminal events into routed events on the visual tree.

Keyboard:

1. A raw `TerminalKeyEvent` is received by `TerminalApp`.
2. Command shortcuts are handled first (see [Commands](commands.md)).
3. If not handled, `TerminalApp` raises `Visual.KeyDownEvent` on the focused element.
4. Text input and paste are raised separately:
   - `Visual.TextInputEvent` with `TextInputEventArgs`
   - `Visual.PasteEvent` with `PasteEventArgs`

Mouse:

1. `TerminalApp` hit-tests the input root to find a target visual.
   - Hit testing ignores visuals where `IsHitTestVisible` is false.
   - If the hit visual is not visible/enabled, `TerminalApp` walks up to the nearest visible+enabled ancestor.
2. It automatically captures the pointer on left mouse down.
3. It raises pointer routed events on the target:
   - `Visual.PointerPressedEvent`
   - `Visual.PointerMovedEvent`
   - `Visual.PointerReleasedEvent`
   - `Visual.PointerWheelEvent`

The input root can be different when a modal overlay is active (for example, a dialog or popup).

## Focus

Controls opt in to focus by setting `Focusable = true`.

- Tab navigation moves focus between focusable visuals.
- Focus affects rendering (focused styles) and keyboard input routing.

### How focus is assigned

- Keyboard focus is kept within a focus scope managed by `TerminalApp`.
- On mouse down (or double click), `TerminalApp` walks up from the hit target to the nearest focusable ancestor and focuses it.
  - This is why clicking inside a complex control (like a text editor) typically focuses that editor.

If you implement a focusable control, use `HasFocus` / `HasFocusWithin` to render focus cues and to decide what keyboard behavior to enable.

## Routed events

Terminal UI uses routed events so that containers can intercept input (preview) or react to child interactions (bubble).

### Event routing strategies

Each routed event has a `RoutingStrategy`:

- `Direct`: only invokes handlers on the target.
- `Preview`: routes from root down to the target (parents first).
- `Bubble`: routes from the target up to the root (parents last).

Most pointer events use `Preview | Bubble` so both patterns are possible.

### Handled events

Routed event args derive from `RoutedEventArgs` and include:

- `Handled`: marks the event as consumed for regular handlers.
- `OriginalSource`: the visual where the event started.
- `Source`: the current visual during routing.
- `RoutingPhase`: the current phase (`Direct`, `Preview`, `Bubble`).

Containers should set `Handled = true` when they fully consume an input gesture.
Regular routed handlers are skipped after an event is marked handled, but handlers registered with `handledEventsToo: true` still run.

### Where input is handled

Every `Visual` can participate in routed events by overriding the protected virtual methods declared on `Visual`:

- `OnKeyDown(KeyEventArgs e)`
- `OnTextInput(TextInputEventArgs e)`
- `OnPaste(PasteEventArgs e)`
- `OnPointerMoved(PointerEventArgs e)`
- `OnPointerPressed(PointerEventArgs e)`
- `OnPointerReleased(PointerEventArgs e)`
- `OnPointerWheel(PointerEventArgs e)`

These methods are themselves routed-event dispatch points (they are annotated with `[RoutedEvent]` and wired by the source generator).

For events declared as `Preview | Bubble`, each handler can check `e.RoutingPhase` to run logic only during the intended pass:

```csharp
protected override void OnPointerPressed(PointerEventArgs e)
{
    if (e.RoutingPhase != RoutingPhase.Bubble)
    {
        return;
    }

    // Bubble-only behavior here.
}
```

### Disabled visuals and routing

Disabled visuals do not participate in input routing:

- During routing, each node checks `IsEnabled` before invoking dispatch and user handlers.
- Routing continues through the tree even if a node is disabled, so enabled ancestors can still react.

This is important for composition: for example, a `ScrollViewer` can still scroll when the pointer is over a disabled child.

### Example: handling keyboard activation in a button

From `Button`:

```csharp
protected override void OnKeyDown(KeyEventArgs e)
{
    if (e.Key is TerminalKey.Enter or TerminalKey.Space)
    {
        RaiseEvent(Button.ClickEvent, new ClickEventArgs());
        e.Handled = true;
    }
}
```

### Why routed events matter

Routed events enable patterns like:

- click events on a Button
- selection change events on lists and tables
- scroll viewers handling wheel input even when the pointer is over a child control

They also make it easier to observe interactions from higher-level containers without tightly coupling controls.

## Creating a custom routed event

To expose an interaction from a control, declare a dispatch method and mark it with `[RoutedEvent]`.
The source generator will produce:

- a static `...Event` identifier (`RoutedEvent<TArgs>`)
- an instance event `...Routed` that adds/removes handlers via `Visual.AddHandler/RemoveHandler`
- fluent helper methods to register handlers (for example `button.Click(...)`)

Rules:

- the containing type must be `partial`
- the method must return `void` and take exactly one parameter (the event args type)
- the event name is derived from the method name (`OnClick` becomes `ClickEvent`)

Example:

```csharp
public sealed partial class FancyButton : ContentVisual
{
    [RoutedEvent(RoutingStrategy.Bubble)]
    protected virtual void OnClick(ClickEventArgs e) { }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (e.Button == TerminalMouseButton.Left)
        {
            RaiseEvent(ClickEvent, new ClickEventArgs());
            e.Handled = true;
        }
    }
}
```

> [!IMPORTANT]
> The routed event system is also used for framework events like `Visual.PointerPressedEvent`. Overriding the virtual method is often the simplest way to handle input inside a control.

If you need to observe an event even after descendants mark it handled, register directly with:

```csharp
AddHandler(PointerWheelEvent, OnWheelObserved, handledEventsToo: true);
```

## Hit testing and pointer coordinates

Pointer events are dispatched to a target visual chosen by hit testing:

- Hit testing is based on `Bounds` (arranged rectangles in cell coordinates).
- Use `IsHitTestVisible = false` for visuals that should never receive pointer events (for example, non-interactive overlays such as tooltips).
- `IsVisible` and `IsEnabled` affect dispatch:
  - Non-visible or disabled visuals are not chosen as targets.
  - Ancestors can still handle events via routing.

`PointerEventArgs` provides several coordinate spaces:

- `X` / `Y`: terminal coordinates
- `UiX` / `UiY`: UI root coordinates (important in inline/live hosting where the UI is offset inside the terminal)
- `LocalX` / `LocalY`: coordinates relative to the event target's `Bounds`

## Mouse capture

Mouse capture ensures that a pressed control continues to receive mouse events until release, even when the pointer moves outside.

### Capture is automatic

`TerminalApp` captures the pointer automatically on left mouse down:

- The target continues to receive move/drag events even if the pointer leaves the control (and for inline/live hosting, even if it leaves the live region).
- The capture is released on left mouse up.

Controls typically do not request capture themselves. Instead, implement press/drag/release state as bindable properties.

### Hover behavior while captured

While a pointer is captured, `TerminalApp` keeps hover on the captured element to avoid hover state leaking to other visuals during a drag.

### Example: press/drag state (Button pattern)

The button uses:

- `IsPressed` to track an active press
- `IsPressedInside` to track whether the pointer is still inside while dragging

This produces correct behavior for click-cancel (press inside, drag out, release does not click).

## Context menus (right click)

In fullscreen hosting, `TerminalApp` can open a context menu on right click if the pointer press is not handled:

- First it looks for the nearest `ContextMenuFactory` in the hovered chain.
- If none is provided, it falls back to commands with `CommandPresentation.ContextMenu`.

Controls that handle right click themselves should set `Handled = true` in `OnPointerPressed` to prevent the default context menu behavior.

## Recommended patterns for custom controls

- Store interaction state as `[Bindable]` properties so layout/render invalidation is automatic.
  - Example: `IsPressed`, `IsOpen`, `SelectedIndex`, `ScrollModel`, etc.
- In input handlers, update bindable state and set `Handled = true` when appropriate.
- Prefer routed events (and/or commands) for user-observable interactions:
  - Routed events work well for things like "clicked", "selection changed", "item activated".
  - Commands work well for keyboard shortcuts that should be discoverable in `CommandBar` / `CommandPalette`.

## Related

- [Button](controls/button.md)
- [ScrollViewer](controls/scrollviewer.md)
- [Commands](commands.md)
