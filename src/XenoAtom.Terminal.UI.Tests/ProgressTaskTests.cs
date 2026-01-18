// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class ProgressTaskTests
{
    [TestMethod]
    public void ProgressTask_Computes_Progress01_From_Value_And_Range()
    {
        var task = new ProgressTask("Work")
        {
            Minimum = 0.0,
            Maximum = 10.0,
            Value = 5.0,
        };

        Assert.AreEqual(0.5, task.Progress01, 1e-12);
    }

    [TestMethod]
    public void ProgressTask_Clamps_Percentage_To_0_100()
    {
        var task = new ProgressTask("Work")
        {
            Minimum = 0.0,
            Maximum = 10.0,
            Value = 15.0,
        };

        Assert.AreEqual(1.0, task.Progress01, 1e-12);
        Assert.AreEqual(100, task.Percentage);

        task.Value = -5.0;
        Assert.AreEqual(0.0, task.Progress01, 1e-12);
        Assert.AreEqual(0, task.Percentage);
    }

    [TestMethod]
    public void ProgressTask_Returns_Zero_Progress_When_Range_Is_Invalid()
    {
        var task = new ProgressTask("Work")
        {
            Minimum = 10.0,
            Maximum = 10.0,
            Value = 10.0,
        };

        Assert.AreEqual(0.0, task.Progress01, 1e-12);
        Assert.AreEqual(0, task.Percentage);
    }
}

