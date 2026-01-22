# Culture and Value Formatting

XenoAtom.Terminal.UI formats values using a configurable `CultureInfo`.

This affects common UI scenarios such as:

- data templating (`DataPresenter<T>`, list item templates, etc.)
- default formatting of numeric values in controls (for example `Slider<T>`)

By default, the culture is `CultureInfo.InvariantCulture` (stable formatting across machines and locales).

## Setting the application culture

### Fullscreen (`Terminal.Run`)

```csharp
using System.Globalization;
using XenoAtom.Terminal.UI;

Terminal.Run(
    visual,
    onUpdate,
    new TerminalRunOptions
    {
        Culture = CultureInfo.GetCultureInfo("fr-FR"),
    });
```

### Inline (`Terminal.Live`)

```csharp
using System.Globalization;
using XenoAtom.Terminal.UI;

Terminal.Live(
    visual,
    onUpdate,
    new TerminalLiveOptions
    {
        Culture = CultureInfo.GetCultureInfo("fr-FR"),
    });
```

## Overriding culture for a subtree

You can override formatting for a specific subtree by setting `CultureStyle` in the environment:

```csharp
using System.Globalization;
using XenoAtom.Terminal.UI.Styling;

root.Set(CultureStyle.Key, CultureStyle.Default with
{
    Culture = CultureInfo.GetCultureInfo("fr-FR"),
});
```

This is useful when you want most of the UI to use one culture, but a specific panel to use another.

## Formatting helpers

Controls and templates should prefer using `Visual` helpers rather than calling `ToString()` directly:

- `Visual.ToStringValue<T>(T value, string? format = null)`
- `Visual.ToStringObject(object? value, string? format = null)`

These helpers automatically pick up the resolved culture.

