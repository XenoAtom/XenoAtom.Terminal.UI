using XenoAtom.Terminal.UI;

namespace XenoAtom.Terminal.UI.ControlsDemo;

internal static class ScreenshotMode
{
    public static T InScreenshot<T>(this T visual, DemoContext context, Action action)
        where T : Visual
    {
        ArgumentNullException.ThrowIfNull(visual);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(action);

        if (!context.IsScreenshot)
        {
            return visual;
        }

        var ran = false;
        visual.Update(_ =>
        {
            if (ran)
            {
                return;
            }

            ran = true;
            action();
        });

        return visual;
    }
}

