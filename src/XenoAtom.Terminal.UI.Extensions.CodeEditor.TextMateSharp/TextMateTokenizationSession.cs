// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;

internal sealed class TextMateTokenizationSession
{
    private readonly IGrammar _grammar;

    public TextMateTokenizationSession(string scopeName, RegistryOptions registryOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeName);
        ArgumentNullException.ThrowIfNull(registryOptions);

        ScopeName = scopeName;
        var registry = new Registry(registryOptions);
        _grammar = registry.LoadGrammar(scopeName)
            ?? throw new InvalidOperationException($"Unable to load the TextMate grammar for scope `{scopeName}`.");
    }

    public string ScopeName { get; }

    public ITokenizeLineResult TokenizeLine(string lineText, IStateStack? previousState)
        => _grammar.TokenizeLine(lineText ?? string.Empty, previousState, TimeSpan.MaxValue);
}
