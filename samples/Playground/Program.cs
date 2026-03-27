using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Input;



var promptEditor = new PromptEditor();
promptEditor.EscapeBehavior(PromptEditorEscapeBehavior.CancelCompletionOnly);

promptEditor.AddCommand(new Command()
{
    Id = "CodeAlta.Thread.ExpandPrompt.Close",
    LabelMarkup = "Close",
    DescriptionMarkup = "Close the large prompt editor and keep the current draft.",
    Gesture = new KeyGesture(TerminalKey.Escape),
    Importance = CommandImportance.Primary,
    Execute = _ => Terminal.Title = "Hello",
});

Terminal.Run(
    promptEditor,
    onUpdate: () => TerminalLoopResult.Continue);
