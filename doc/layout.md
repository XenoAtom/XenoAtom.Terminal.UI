---
title: Layout
---

# Layout

Terminal UI layout is cell-based (integer coordinates) and uses a two-pass protocol:

- **Measure**: compute intrinsic `SizeHints` under `LayoutConstraints`
- **Arrange**: receive a finite `Rectangle` and position children inside it

The detailed specification is in:

- [Layout Protocol Specs](./specs/layout_protocol_specs.md)

## Alignment

By default:

- `HorizontalAlignment` is `Align.Start`
- `VerticalAlignment` is `Align.Start`

Containers and content controls may choose defaults more appropriate for their role (e.g. `ScrollViewer` stretches).

## Margin and padding

- Margin is handled on `Visual`.
- Padding is typically handled by containers/controls (e.g. Border, Group, TextBoxStyle).

## Common containers

- `VStack`, `HStack`
- `Grid`
- `DockLayout`
- `Border`, `Group`, `Center`

See also:

- [Controls Reference](./controls/readme.md)
