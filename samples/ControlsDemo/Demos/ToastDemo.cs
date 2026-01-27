using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.ControlsDemo.Demos;

[Demo("Toast", "Overlays", Description = "Non-blocking notifications with actions and timers.")]
public sealed class ToastDemo : ControlsDemoBase
{
    public ToastDemo() : base(DemoSource.Get())
    {
    }

    public override Visual Build(DemoContext context)
    {
        if (context.ToastHost is null)
        {
            return new VStack(
                    DemoUi.Title("ToastHost not available"),
                    DemoUi.Hint("Wrap your root with ToastHost to show notifications."))
                .Spacing(1);
        }

        var positionState = new State<ToastPosition>(ToastPosition.TopRight);
        var maxVisibleState = new State<int>(4);
        var durationSecondsState = new State<int>(4);
        var pauseOnHoverState = new State<bool>(true);
        var showProgressState = new State<bool>(true);

        var positionSelect = new Select<ToastPosition>()
            .Items([ToastPosition.TopRight, ToastPosition.TopLeft, ToastPosition.TopCenter, ToastPosition.BottomRight, ToastPosition.BottomLeft, ToastPosition.BottomCenter]);

        var initialIndex = positionSelect.Items.IndexOf(positionState.Value);
        positionSelect.SelectedIndex = initialIndex >= 0 ? initialIndex : 0;
        positionState.Value = positionSelect.Items[Math.Clamp(positionSelect.SelectedIndex, 0, positionSelect.Items.Count - 1)];

        positionSelect.SelectionChanged((_, e) =>
        {
            if (positionSelect.Items.Count == 0)
            {
                return;
            }

            var index = Math.Clamp(e.NewIndex, 0, positionSelect.Items.Count - 1);
            positionState.Value = positionSelect.Items[index];
        });

        var maxVisibleSlider = new Slider<int>
        {
            Minimum = 1,
            Maximum = 6,
            Step = 1,
            ShowValueLabel = true,
        }.Value(maxVisibleState);

        var durationSlider = new Slider<int>
        {
            Minimum = 1,
            Maximum = 10,
            Step = 1,
            ShowValueLabel = true,
        }
            .ValueFormatter(value => $"{value}s")
            .Value(durationSecondsState);

        var pauseOnHoverToggle = new CheckBox("Pause on hover", true).IsChecked(pauseOnHoverState);
        var showProgressToggle = new CheckBox("Show countdown", true).IsChecked(showProgressState);

        ConfigureHost(context.ToastHost, positionState, maxVisibleState, durationSecondsState, pauseOnHoverState);

        void ShowToast(ToastSeverity severity)
        {
            ToastService.Show(() => new Toast
            {
                Title = $"{severity} toast",
                Content = $"This is a {severity.ToString().ToLowerInvariant()} notification.",
                Severity = severity,
                ShowProgress = showProgressState.Value,
                Duration = TimeSpan.FromSeconds(Math.Max(1, durationSecondsState.Value)),
            });
        }

        void ShowActionToast()
        {
            ToastService.Show(() => new Toast
            {
                Title = "Update available",
                Content = "A new build is ready to install.",
                Severity = ToastSeverity.Info,
                ShowProgress = showProgressState.Value,
                Duration = TimeSpan.FromSeconds(Math.Max(1, durationSecondsState.Value)),
                Action = new Button("Install").Click(() => context.Log("Install clicked")),
            });
        }

        void ShowPersistentToast()
        {
            ToastService.Show(() => new Toast
            {
                Title = "Uploading…",
                Content = new HStack(new Spinner(), "Processing files…").Spacing(1),
                Severity = ToastSeverity.Info,
                Duration = null,
                ShowProgress = false,
                ShowCloseButton = true,
            });
        }

        var buttons = new HStack(
                new Button("Info").Click(() => ShowToast(ToastSeverity.Info)),
                new Button("Success").Click(() => ShowToast(ToastSeverity.Success)),
                new Button("Warning").Click(() => ShowToast(ToastSeverity.Warning)),
                new Button("Error").Click(() => ShowToast(ToastSeverity.Error)))
            .Spacing(1);

        var actions = new HStack(
                new Button("Action toast").Click(ShowActionToast),
                new Button("Persistent toast").Click(ShowPersistentToast),
                new Button("Dismiss all").Click(() => context.ToastHost?.DismissAll()))
            .Spacing(1);

        return new VStack(
                DemoUi.Hint("Toasts are overlay notifications that do not steal focus."),
                new Group().Padding(1).Content(new VStack(
                        DemoUi.Title("Host settings"),
                        new HStack("Position:", positionSelect).Spacing(1),
                        new HStack("Max visible:", maxVisibleSlider).Spacing(1),
                        new HStack("Default duration:", durationSlider).Spacing(1),
                        pauseOnHoverToggle,
                        showProgressToggle)
                    .Spacing(1)),
                new Group().Padding(1).Content(new VStack(
                        DemoUi.Title("Show toasts"),
                        buttons,
                        actions)
                    .Spacing(1)))
            .Spacing(1);
    }

    private static void ConfigureHost(ToastHost host, State<ToastPosition> position, State<int> maxVisible, State<int> durationSeconds, State<bool> pauseOnHover)
    {
        host.Position(position);
        host.MaxVisible(maxVisible);
        host.PauseOnHover(pauseOnHover);
        host.DefaultDuration(() => TimeSpan.FromSeconds(Math.Max(1, durationSeconds.Value)));
    }
}
