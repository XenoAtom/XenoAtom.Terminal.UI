# Markup

`Markup` renders XenoAtom.Terminal markup text as a visual (with wrapping and styling).

## Basic usage

```csharp
new Markup("[bold]Hello[/] [gray]world[/]!");
```

## Notes

- Markup is parsed using the XenoAtom.Terminal markup parser.
- Use `Markup` when you want inline color and text styles without building a full visual tree.

## Related

- `../markup-parsing.md`
- `../styling.md`
