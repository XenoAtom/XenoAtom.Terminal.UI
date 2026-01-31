using System.Globalization;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Bindable models (form)", "Patterns", Description = "Bind controls directly to your own [Bindable] model properties.")]
public sealed class BindableModelsDemo : ControlsDemoBase
{
    public BindableModelsDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        _ = context;

        var model = new WorkItemModel
        {
            Done = false,
            Kind = WorkItemKind.Feature,
            Priority = WorkItemPriority.Medium,
            EstimateHours = 6,
            Title = "Add search + filters",
            Notes = "Describe the work here.\nUse a TextArea for multi-line input.",
        };

        // Local UI state is often best expressed with State<T> (a single bindable Value property).
        var showPreview = new State<bool>(true);

        var form = new Table()
            .Headers("Field", "Value")
            .AddRow("Done", new Switch().IsOn(model.Bind.Done))
            .AddRow("Kind", new EnumSelect<WorkItemKind>().Value(model.Bind.Kind))
            .AddRow("Priority", new EnumSelect<WorkItemPriority>().Value(model.Bind.Priority))
            .AddRow(
                "Estimate (hours)",
                new NumberBox<int>()
                    .Value(model.Bind.EstimateHours)
                    .ValueValidator(v => v is >= 0 and <= 10_000 ? null : "Estimate must be in [0..10000]"))
            .AddRow("Title", new TextBox().Text(model.Bind.Title))
            .AddRow("Notes", new TextArea().Text(model.Bind.Notes).MinHeight(5).MaxHeight(5).Scrollable());

        var preview = new Group("Live preview")
            .Content(new VStack(
                    new TextBlock(() => $"{(model.Done ? "✅" : "⬜")} {model.Kind}  •  {model.Priority}"),
                    new TextBlock(() => $"Estimate: {model.EstimateHours.ToString(CultureInfo.InvariantCulture)} h"),
                    new Rule(),
                    new TextBlock(() => model.Title ?? string.Empty),
                    new Rule(),
                    new TextBlock(() => model.Notes ?? string.Empty))
                .Spacing(1))
            .HorizontalAlignment(Align.Stretch);

        var body = new HStack(
                new Border(form).Padding(1).MinWidth(32),
                new Border(new ContentSwitcher()
                        .SelectedIndex(() => showPreview.Value ? 1 : 0)
                        .Add(new TextBlock("Preview hidden."), preview))
                    .Padding(1)
                    .HorizontalAlignment(Align.Stretch))
            .Spacing(2)
            .HorizontalAlignment(Align.Stretch);

        return new VStack(
                DemoUi.Hint("Bind controls to model properties via model.Bind.Property."),
                DemoUi.Hint("Use State<T> for small pieces of UI-only state."),
                new HStack(new Switch().IsOn(showPreview), "Show preview").Spacing(1),
                new Rule(),
                body)
            .Spacing(1)
            .HorizontalAlignment(Align.Stretch);
    }
}

public enum WorkItemKind
{
    Feature,
    Bug,
    Chore,
}

public enum WorkItemPriority
{
    Low,
    Medium,
    High,
    Critical,
}

public sealed partial class WorkItemModel
{
    public WorkItemModel()
    {
        Title = string.Empty;
        Notes = string.Empty;
    }

    [Bindable] public partial bool Done { get; set; }
    [Bindable] public partial WorkItemKind Kind { get; set; }
    [Bindable] public partial WorkItemPriority Priority { get; set; }
    [Bindable] public partial int EstimateHours { get; set; }
    [Bindable] public partial string? Title { get; set; }
    [Bindable] public partial string? Notes { get; set; }
}
