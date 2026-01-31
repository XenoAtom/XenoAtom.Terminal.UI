# Input, Focus & Events

XenoAtom.Terminal.UI is built on the unified input event stream of XenoAtom.Terminal:

- keys and text input
- mouse move/click/wheel
- resize events

## Focus

Controls can opt-in to focus by setting `Focusable`.

- Tab navigation moves focus between focusable visuals.
- Focus affects rendering (focused styles) and keyboard input routing.

## Routed events

Some interactions are surfaced as routed events (preview + bubble) so containers can intercept input.
This enables patterns like:

- click events on a Button
- selection change events on lists

## Mouse capture

Mouse capture ensures that a pressed control continues to receive mouse events until release, even when the pointer moves outside.

This prevents “hover bleed” and inconsistent pressed/drag behavior.

See also:

- [Button](./controls/button.md)
- [ScrollViewer](./controls/scrollviewer.md)
