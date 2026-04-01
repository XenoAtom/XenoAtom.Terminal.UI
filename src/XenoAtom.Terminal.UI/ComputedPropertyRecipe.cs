// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI;

internal sealed class ComputedPropertyRecipe
{
    public required BindingAccessor Accessor { get; init; }

    public required object? State { get; init; }

    public required Action<Visual, object?> Apply { get; init; }
}
