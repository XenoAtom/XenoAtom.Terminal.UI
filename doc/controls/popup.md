# Popup

`Popup` is an overlay surface used for dropdowns, context menus, and lightweight transient UI.

Screenshot placeholder:

![Popup](../../img/screenshots/popup.png)

Popups close when focus is lost or when the user clicks outside (depending on configuration).

## Anchoring

Popups can be positioned relative to:

- an anchor visual (via `Popup.Anchor`), or
- an explicit rectangle in UI coordinates (via `Popup.AnchorRect`).

`AnchorRect` is primarily used for point-based popups such as context menus (right-click), where there is no natural anchor
visual to align to.

