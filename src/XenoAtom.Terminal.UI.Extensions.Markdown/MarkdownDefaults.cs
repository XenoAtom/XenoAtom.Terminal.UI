// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using Markdig;

namespace XenoAtom.Terminal.UI.Extensions.Markdown;

internal static class MarkdownDefaults
{
    public static MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
        .Configure("common+pipetables+alerts")
        .Build();
}

