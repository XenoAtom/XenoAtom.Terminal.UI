using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Templating;
using System;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Data templates", "Patterns", Description = "Environment-scoped data templates and DataPresenter<T>.")]
public sealed class DataTemplatesDemo : ControlsDemoBase
{
    private enum DemoEnum
    {
        Alpha,
        Beta,
        Gamma,
    }

    public DataTemplatesDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        var text = new State<string>("Alex");
        var port = new State<int>(8080);

        var presenter = new VStack(
                DemoUi.Title("DataPresenter<T>"),
                DemoUi.Hint("Use Role=Display for viewing and Role=Editor for editing when the value is a bindable source (State<T>/Binding<T>)."),
                new HStack(
                        "Name:",
                        text.PresentAs(DataTemplateRole.Editor).HorizontalAlignment(HorizontalAlignment.Stretch))
                    .Spacing(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),
                new HStack(
                        "Port:",
                        port.PresentAs(DataTemplateRole.Editor).HorizontalAlignment(HorizontalAlignment.Stretch))
                    .Spacing(1)
                    .HorizontalAlignment(HorizontalAlignment.Stretch),
                new HStack(
                        "Summary:",
                        text.PresentAs(DataTemplateRole.Display))
                    .Spacing(1))
            .Spacing(1)
            .HorizontalAlignment(HorizontalAlignment.Stretch);

        var boolValue = new State<bool>(true);
        var charValue = new State<char>('A');
        var guidValue = new State<Guid>(Guid.NewGuid());
        var sbyteValue = new State<sbyte>(-12);
        var byteValue = new State<byte>(200);
        var shortValue = new State<short>(-1234);
        var ushortValue = new State<ushort>(1234);
        var uintValue = new State<uint>(123_456u);
        var longValue = new State<long>(123_456_789L);
        var ulongValue = new State<ulong>(123_456_789UL);
        var floatValue = new State<float>(1.2345f);
        var doubleValue = new State<double>(3.1415926535);
        var decimalValue = new State<decimal>(12.34m);
        var dateOnlyValue = new State<DateOnly>(new DateOnly(2024, 1, 1));
        var timeOnlyValue = new State<TimeOnly>(new TimeOnly(12, 34, 56));
        var timeSpanValue = new State<TimeSpan>(TimeSpan.FromMinutes(90));
        var dateTimeValue = new State<DateTime>(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));
        var dateTimeOffsetValue = new State<DateTimeOffset>(DateTimeOffset.UtcNow);
        var enumValue = new State<DemoEnum>(DemoEnum.Beta);

        var enumTemplates = DataTemplates.Default.Derive(builder => builder.RegisterEnum<DemoEnum>());

        var commonTable = new Table()
            .Headers("Type", "Display", "Editor");

        void Add<T>(string typeName, State<T> state)
        {
            commonTable.AddRow(
                new TextBlock(typeName),
                state.PresentAs(DataTemplateRole.Display),
                state.PresentAs(DataTemplateRole.Editor).HorizontalAlignment(HorizontalAlignment.Stretch));
        }

        Add("string", text);
        Add("bool", boolValue);
        Add("char", charValue);
        Add("Guid", guidValue);
        Add("DateOnly", dateOnlyValue);
        Add("TimeOnly", timeOnlyValue);
        Add("TimeSpan", timeSpanValue);
        Add("DateTime", dateTimeValue);
        Add("DateTimeOffset", dateTimeOffsetValue);
        Add("enum", enumValue);

        var numericTable = new Table()
            .Headers("Type", "Display", "Editor");

        void AddNumber<T>(string typeName, State<T> state)
        {
            numericTable.AddRow(
                new TextBlock(typeName),
                state.PresentAs(DataTemplateRole.Display),
                state.PresentAs(DataTemplateRole.Editor).HorizontalAlignment(HorizontalAlignment.Stretch));
        }

        AddNumber("sbyte", sbyteValue);
        AddNumber("byte", byteValue);
        AddNumber("short", shortValue);
        AddNumber("ushort", ushortValue);
        AddNumber("int", port);
        AddNumber("uint", uintValue);
        AddNumber("long", longValue);
        AddNumber("ulong", ulongValue);
        AddNumber("float", floatValue);
        AddNumber("double", doubleValue);
        AddNumber("decimal", decimalValue);

        var defaultTemplates = new VStack(
                DemoUi.Title("Default templates"),
                DemoUi.Hint("These are built-in defaults for common .NET types (display + editor)."),
                new HStack(
                        new VStack(DemoUi.Title("Common"), commonTable).Spacing(1),
                        new VStack(DemoUi.Title("Numbers"), numericTable).Spacing(1))
                    .Spacing(2)
                    .HorizontalAlignment(HorizontalAlignment.Stretch))
            .Spacing(1);

        var calloutTemplates = DataTemplates.Default.Derive(builder => builder
            .Register<string>(DataTemplateRole.Display, new((Binding<string> binding, in DataTemplateContext _) => new TextBlock(() => $"> {binding.GetValue()}")))
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
                defaultTemplates.Style(enumTemplates),
                new Rule(),
                listDefaults)
            .Spacing(2);
    }
}
