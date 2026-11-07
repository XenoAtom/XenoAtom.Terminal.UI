// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.UI.Controls;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class BindingManagerThreadingTests
{
    [TestMethod]
    public void Current_Is_Distinct_Per_Thread_And_Stable()
    {
        var current = BindingManager.Current;
        Assert.AreSame(current, BindingManager.Current);

        BindingManager? other = null;
        var otherIsStable = false;
        TestThreads.Run(() =>
        {
            other = BindingManager.Current;
            otherIsStable = ReferenceEquals(other, BindingManager.Current);
        });

        Assert.IsNotNull(other);
        Assert.AreNotSame(current, other);
        Assert.IsTrue(otherIsStable);
    }

    [TestMethod]
    public void ValueChanged_Is_Only_Raised_On_The_Thread_Of_The_Change()
    {
        TestThreads.Run(() =>
        {
            var owner = new PlainBindableOwner();
            var raisedHere = 0;
            void Handler(Binding binding)
            {
                if (ReferenceEquals(binding.Owner, owner))
                {
                    raisedHere++;
                }
            }

            BindingManager.Current.ValueChanged += Handler;
            try
            {
                var raisedThere = 0;
                TestThreads.Run(() =>
                {
                    void OtherHandler(Binding binding)
                    {
                        if (ReferenceEquals(binding.Owner, owner))
                        {
                            raisedThere++;
                        }
                    }

                    BindingManager.Current.ValueChanged += OtherHandler;
                    try
                    {
                        BindingManager.Current.NotifyValueChanged(owner, PlainBindableOwner.ValueAccessor);
                    }
                    finally
                    {
                        BindingManager.Current.ValueChanged -= OtherHandler;
                    }
                });

                Assert.AreEqual(1, raisedThere);
                Assert.AreEqual(0, raisedHere);

                BindingManager.Current.NotifyValueChanged(owner, PlainBindableOwner.ValueAccessor);
                Assert.AreEqual(1, raisedHere);
            }
            finally
            {
                BindingManager.Current.ValueChanged -= Handler;
            }
        });
    }

    [TestMethod]
    public void Tracking_Is_Local_To_The_Thread()
    {
        TestThreads.Run(() =>
        {
            var local = new State<int>(1);
            var remote = new State<int>(2);

            using var session = BindingManager.Current.StartTracking();
            _ = local.Value;

            var remoteIsTracking = true;
            TestThreads.Run(() =>
            {
                remoteIsTracking = BindingManager.Current.IsTracking;
                _ = remote.Value;
            });

            Assert.IsFalse(remoteIsTracking);
            Assert.IsTrue(BindingManager.Current.IsTracking);
            Assert.Contains((Binding)local, session.Reads);
            Assert.DoesNotContain((Binding)remote, session.Reads);
        });
    }

    [TestMethod]
    public void Nested_Tracking_Sessions_Restore_In_Order()
    {
        TestThreads.Run(() =>
        {
            var outerState = new State<int>(1);
            var innerState = new State<int>(2);
            var manager = BindingManager.Current;

            using (var outer = manager.StartTracking())
            {
                _ = outerState.Value;
                using (var inner = manager.StartTracking())
                {
                    _ = innerState.Value;
                    Assert.Contains((Binding)innerState, inner.Reads);
                    Assert.DoesNotContain((Binding)outerState, inner.Reads);
                }

                Assert.IsTrue(manager.IsTracking);
                Assert.Contains((Binding)outerState, outer.Reads);
                Assert.DoesNotContain((Binding)innerState, outer.Reads);
            }

            Assert.IsFalse(manager.IsTracking);
        });
    }

    [TestMethod]
    public void TrackingSession_Disposed_On_Another_Thread_Releases_Its_Own_Manager()
    {
        TestThreads.Run(() =>
        {
            var manager = BindingManager.Current;
            var session = manager.StartTracking();
            Assert.IsTrue(manager.IsTracking);

            var otherWasTracking = true;
            var otherIsTracking = true;
            TestThreads.Run(() =>
            {
                otherWasTracking = BindingManager.Current.IsTracking;
                session.Dispose();
                otherIsTracking = BindingManager.Current.IsTracking;
            });

            Assert.IsFalse(otherWasTracking);
            Assert.IsFalse(otherIsTracking);
            Assert.IsFalse(manager.IsTracking);
        });
    }

    [TestMethod]
    public void EnableDependencyLoopCheck_Is_Per_Thread()
    {
        TestThreads.Run(() =>
        {
            BindingManager.Current.EnableDependencyLoopCheck = false;

            var otherEnabled = false;
            TestThreads.Run(() => otherEnabled = BindingManager.Current.EnableDependencyLoopCheck);

            Assert.IsTrue(otherEnabled);
            Assert.IsFalse(BindingManager.Current.EnableDependencyLoopCheck);
        });
    }

    [TestMethod]
    public void Two_Way_Binding_Registered_And_Written_On_A_Thread_Propagates()
    {
        TestThreads.Run(() =>
        {
            var state = new State<string?>("hello");
            var textBox = new TextBox().Text(state);

            Assert.AreEqual("hello", textBox.Text);

            textBox.Text = "world";
            Assert.AreEqual("world", state.Value);

            state.Value = "again";
            Assert.AreEqual("again", textBox.Text);
        });
    }

    [TestMethod]
    public void Two_Way_Bindings_On_Concurrent_Threads_Are_Independent()
    {
        const int threadCount = 8;
        const int iterations = 500;

        // A shared source bound from several threads exercises the process-wide two-way binding table under contention.
        var shared = new State<string?>("shared");

        TestThreads.RunConcurrently(threadCount, index =>
        {
            var state = new State<string?>($"start-{index}");
            var textBox = new TextBox().Text(state);
            var sharedTarget = new TextBox().Text(shared);

            for (var i = 0; i < iterations; i++)
            {
                var value = $"value-{index}-{i}";
                state.Value = value;
                Assert.AreEqual(value, textBox.Text);

                var back = $"back-{index}-{i}";
                textBox.Text = back;
                Assert.AreEqual(back, state.Value);
            }

            Assert.IsNotNull(sharedTarget.Text);
        });
    }

    private sealed class PlainBindableOwner
    {
        public static readonly BindingAccessor<int> ValueAccessor = new(
            "Value",
            owner => ((PlainBindableOwner)owner).Value,
            (owner, value) => ((PlainBindableOwner)owner).Value = value);

        public int Value;
    }
}
