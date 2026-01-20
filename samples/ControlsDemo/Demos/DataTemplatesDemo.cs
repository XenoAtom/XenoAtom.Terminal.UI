using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Templating;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Data templates", "Patterns", Description = "Environment-scoped data templates and DataPresenter<T>.")]
public sealed class DataTemplatesDemo : ControlsDemoBase
{
    public DataTemplatesDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var name = new State<string?>("Alex");
        var port = new State<int>(8080);

        var presenter = new VStack(
                DemoUi.Title("DataPresenter<T>"),
                DemoUi.Hint("Use Role=Display for viewing and Role=Editor for editing when the value is a bindable source (State<T>/Binding<T>)."),
                new HStack(
                        "Name:",
                        new DataPresenter<State<string?>> { Value = name, Role = DataTemplateRole.Editor }.HorizontalAlignment(HorizontalAlignment.Stretch))
                    .Spacing(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),
                new HStack(
                        "Port:",
                        new DataPresenter<State<int>> { Value = port, Role = DataTemplateRole.Editor }.HorizontalAlignment(HorizontalAlignment.Stretch))
                    .Spacing(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),
                new HStack(
                        "Summary:",
                        new DataPresenter<State<string?>> { Value = name, Role = DataTemplateRole.Display })
                    .Spacing(1))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var calloutTemplates = DataTemplates.Default.Derive(builder => builder
            .Register<string>(DataTemplateRole.Display, new((string value, in DataTemplateContext _) => new TextBlock($"> {value}")))
        );

        var listDefaults = new VStack(
                DemoUi.Title("Environment overrides"),
                DemoUi.Hint("Override DataTemplates in a subtree with .Style(templates)."),
                new HStack(
                        new VStack(
                                DemoUi.Title("Default"),
                                new ListBox<string>().Items(["One", "Two", "Three"]).MinHeight(4).MaxHeight(4)),
                        new VStack(
                                DemoUi.Title("Override"),
                                new ListBox<string>().Items(["One", "Two", "Three"]).MinHeight(4).MaxHeight(4).Style(calloutTemplates)))
                    .Spacing(2))
            .Spacing(1);

        return new VStack(
                presenter,
                new Rule(),
                listDefaults)
            .Spacing(2);
    }
}

