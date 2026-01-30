// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Text;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class WordBoundaryUtilityTests
{
    [TestMethod]
    public void WordBoundaryUtility_Matches_Identifier_WholeWord_Semantics()
    {
        var text = "ab_cd ef";

        // Match "ab_cd" at start: before boundary (start) and after boundary (space).
        Assert.IsTrue(WordBoundaryUtility.IsWordBoundary(text, 0, 5));

        // Match "cd" inside identifier: not a boundary (surrounded by word chars).
        Assert.IsFalse(WordBoundaryUtility.IsWordBoundary(text, 3, 2));

        // Match "ef" after space: boundary.
        Assert.IsTrue(WordBoundaryUtility.IsWordBoundary(text, 6, 2));
    }
}

