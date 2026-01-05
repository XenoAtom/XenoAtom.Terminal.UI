// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal;

namespace XenoAtom.Terminal.UI;

public sealed class TerminalApp : IAsyncDisposable
{
    private readonly TerminalInstance _terminal;
    private readonly InlineInteractiveHost _host;
    private readonly AsyncAutoResetEvent _wakeUp = new();
    private readonly CancellationTokenSource _cts = new();

    private bool _renderRequested = true;

    public TerminalApp(Visual root, TerminalInstance? terminal = null)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        _terminal = terminal ?? global::XenoAtom.Terminal.Terminal.Instance;
        _host = new InlineInteractiveHost(_terminal);
    }

    public TerminalInstance Terminal => _terminal;

    public Visual Root { get; }

    public Visual? FocusedElement { get; private set; }

    public void Stop() => _cts.Cancel();

    public async ValueTask DisposeAsync()
    {
        Stop();
        _host.Dispose();
        _cts.Dispose();
        await ValueTask.CompletedTask;
    }

    public void WriteMarkupLine(string markup)
    {
        _host.WriteMarkupLine(markup);
        RequestRender();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
        var token = linkedCts.Token;

        Root.AttachToApp(this);
        BindingManager.Current.ValueChanged += OnValueChanged;

        try
        {
            if (!_terminal.IsInputRunning)
            {
                _terminal.StartInput();
            }

            EnsureInitialFocus();
            RequestRender();

            while (!token.IsCancellationRequested)
            {
                if (_renderRequested)
                {
                    _renderRequested = false;
                    Render();
                }

                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                var readEventTask = _terminal.ReadEventAsync(waitCts.Token).AsTask();
                var wakeTask = _wakeUp.WaitAsync(token);

                var completed = await Task.WhenAny(readEventTask, wakeTask).ConfigureAwait(false);
                if (completed == wakeTask)
                {
                    waitCts.Cancel();

                    try
                    {
                        var maybeEvent = await readEventTask.ConfigureAwait(false);
                        HandleTerminalEvent(maybeEvent);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore.
                    }
                    continue;
                }

                var ev = await readEventTask.ConfigureAwait(false);
                HandleTerminalEvent(ev);
            }
        }
        finally
        {
            BindingManager.Current.ValueChanged -= OnValueChanged;
        }
    }

    internal void RequestRender()
    {
        _renderRequested = true;
        _wakeUp.Set();
    }

    private void OnValueChanged(object owner, string name)
    {
        _ = name;
        if (ReferenceEquals(owner, Root) || owner is Visual)
        {
            RequestRender();
        }
    }

    private void Render()
    {
        var width = Math.Max(1, _terminal.Size.Columns);

        Root.Measure(new CellSize(width, int.MaxValue / 4));
        Root.Arrange(new CellRect(0, 0, width, Root.DesiredSize.Height));

        var buffer = new CellBuffer(width, Math.Max(1, Root.DesiredSize.Height));
        Root.RenderTree(buffer);

        _host.Render(buffer.ToMarkupLines());
    }

    private void DispatchKeyEvent(TerminalKeyEvent keyEvent)
    {
        if (FocusedElement is null)
        {
            return;
        }

        var args = new KeyEventArgs { RawEvent = keyEvent };
        FocusedElement.RaiseEvent(Visual.KeyDownEvent, args);
        if (!args.Handled && keyEvent.Char is { } ch && ch >= ' ')
        {
            FocusedElement.RaiseEvent(Visual.TextInputEvent, args);
        }
    }

    private void EnsureInitialFocus()
    {
        if (FocusedElement is not null)
        {
            return;
        }

        FocusedElement = Root.EnumerateVisualsDepthFirst().FirstOrDefault(v => v.Focusable);
    }

    private void FocusNext()
    {
        var focusables = Root.EnumerateVisualsDepthFirst().Where(v => v.Focusable).ToList();
        if (focusables.Count == 0)
        {
            return;
        }

        if (FocusedElement is null)
        {
            FocusedElement = focusables[0];
            RequestRender();
            return;
        }

        var index = focusables.IndexOf(FocusedElement);
        FocusedElement = focusables[(index + 1) % focusables.Count];
        RequestRender();
    }

    private void HandleTerminalEvent(TerminalEvent ev)
    {
        if (ev is TerminalResizeEvent)
        {
            RequestRender();
            return;
        }

        if (ev is not TerminalKeyEvent keyEvent)
        {
            return;
        }

        if (keyEvent.Key == TerminalKey.Escape)
        {
            _cts.Cancel();
            return;
        }

        if (keyEvent.Key == TerminalKey.Tab)
        {
            FocusNext();
            return;
        }

        DispatchKeyEvent(keyEvent);
    }
}

internal sealed class AsyncAutoResetEvent
{
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();
    private bool _signaled;

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        lock (_waiters)
        {
            if (_signaled)
            {
                _signaled = false;
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Enqueue(tcs);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(static state =>
                {
                    var source = (TaskCompletionSource<bool>)state!;
                    source.TrySetCanceled();
                }, tcs);
            }

            return tcs.Task;
        }
    }

    public void Set()
    {
        TaskCompletionSource<bool>? toRelease = null;

        lock (_waiters)
        {
            if (_waiters.Count > 0)
            {
                toRelease = _waiters.Dequeue();
            }
            else if (!_signaled)
            {
                _signaled = true;
            }
        }

        toRelease?.TrySetResult(true);
    }
}
