// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Terminal.Graphics;
using XenoAtom.Terminal.UI.Graphics;
using ImageControl = XenoAtom.Terminal.UI.Graphics.Image;
using UiImageScaleMode = XenoAtom.Terminal.UI.ImageScaleMode;

namespace XenoAtom.Terminal.UI.Tests;

[TestClass]
public sealed class TerminalImageGraphicsTests
{
    [TestMethod]
    public void Image_RenderGraphics_EmitsTerminalImageSourceCommand()
    {
        var source = CreateRedPixelSource();
        var image = new ImageControl(source)
        {
            CellWidth = 4,
            CellHeight = 2,
            ScaleMode = UiImageScaleMode.Stretch,
            PreserveAspectRatio = false,
            AccessibilityText = "Logo",
            FallbackContent = new TextBlock("fallback"),
        };
        var presenter = new RecordingGraphicsPresenter();

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(12, 6),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();

        Assert.AreEqual(1, presenter.Frames.Count);
        var command = presenter.Frames[0].Single();
        Assert.AreEqual(new Rectangle(0, 0, 4, 2), command.CellBounds);
        Assert.AreEqual(UiImageScaleMode.Stretch, command.ScaleMode);
        Assert.IsFalse(command.PreserveAspectRatio);
        Assert.IsTrue(command.ReserveCells);
        Assert.AreEqual("Logo", command.AccessibilityText);
        Assert.AreEqual(TerminalGraphicContentKind.Object, command.Content.Kind);
        Assert.AreSame(source, command.Content.Source);
        Assert.AreEqual(TerminalImageGraphicsContentTypes.TerminalImageSource, command.Content.MediaType);

        var screen = new AnsiTestScreen(12, 6);
        screen.Apply(driver.Backend.GetOutText());
        Assert.IsFalse(screen.GetText().Contains("fallback", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Image_UsesFallbackContent_WhenGraphicsPresenterIsUnavailable()
    {
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 4,
            CellHeight = 2,
            FallbackContent = new TextBlock("fallback"),
        };

        using var driver = new TerminalAppTestDriver(image, TerminalHostKind.Fullscreen, new TerminalSize(12, 3));

        driver.Tick();

        var screen = new AnsiTestScreen(12, 3);
        screen.Apply(driver.Backend.GetOutText());
        StringAssert.Contains(screen.GetText(), "fallback");
    }

    [TestMethod]
    public void Image_UsesFallbackContent_WhenConfiguredPresenterCannotSelectProtocol()
    {
        var terminalOptions = new TerminalOptions { ImplicitStartInput = true };
        terminalOptions.Graphics.DisableGraphics = true;
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 4,
            CellHeight = 2,
            FallbackContent = new TextBlock("fallback"),
        };
        var presenter = new TerminalImageGraphicsPresenter();

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(12, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter },
            terminalOptions);

        driver.Tick();

        var output = driver.Backend.GetOutText();
        var screen = new AnsiTestScreen(12, 3);
        screen.Apply(output);
        StringAssert.Contains(screen.GetText(), "fallback");
        Assert.IsFalse(output.Contains("\x1b_G", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("\x1b]1337;File=", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("\x1bP0;1q", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Image_RealtimeSourceInvalidation_CoalescesGraphicsRenderRequests()
    {
        var source = new TestRealtimeImageSource();
        var image = new ImageControl(source)
        {
            CellWidth = 2,
            CellHeight = 1,
        };
        var presenter = new RecordingGraphicsPresenter();

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        presenter.ClearFrames();

        source.Publish(1);
        source.Publish(2);

        driver.TickUntil(() => presenter.Frames.Count == 1);

        Assert.AreEqual(1, presenter.Frames.Count);
        Assert.AreEqual(2, presenter.Frames[0].Single().Content.Version);
    }

    [TestMethod]
    public void Image_RealtimeSourceInvalidation_WorksInInlineHost()
    {
        var source = new TestRealtimeImageSource();
        var image = new ImageControl(source)
        {
            CellWidth = 2,
            CellHeight = 1,
        };
        var presenter = new RecordingGraphicsPresenter();

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Inline,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        presenter.ClearFrames();

        source.Publish(3);

        driver.TickUntil(() => presenter.Frames.Count == 1);

        Assert.AreEqual(1, presenter.Frames.Count);
        Assert.AreEqual(TerminalHostKind.Inline, presenter.Contexts[0].HostKind);
        Assert.AreEqual(3, presenter.Frames[0].Single().Content.Version);
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_ForcedKittyProtocol_WritesImageEscapeSequence()
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Kitty,
            MaxPayloadChunkBytes = 64,
        });
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();

        var output = driver.Backend.GetOutText();
        StringAssert.Contains(output, "\x1b_Ga=T");
        StringAssert.Contains(output, ",i=");
        StringAssert.Contains(output, "\x1b\\");
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_AppendsImagesInsideSingleSynchronizedFrame()
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Kitty,
            MaxPayloadChunkBytes = 64,
        });
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();

        var output = driver.Backend.GetOutText();
        var beginIndex = output.IndexOf("\x1b[?2026h", StringComparison.Ordinal);
        var imageIndex = output.IndexOf("\x1b_Ga=T", StringComparison.Ordinal);
        var endIndex = output.LastIndexOf("\x1b[?2026l", StringComparison.Ordinal);

        Assert.IsTrue(beginIndex >= 0);
        Assert.IsTrue(imageIndex > beginIndex, output.Replace("\x1b", "<ESC>", StringComparison.Ordinal));
        Assert.IsTrue(endIndex > imageIndex);
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026h"));
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026l"));
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_AppendsMultipleImagesInsideSingleSynchronizedFrame()
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Sixel,
            MaxPayloadChunkBytes = 128,
        });
        var root = new HStack(
                new ImageControl(CreateRedPixelSource())
                {
                    CellWidth = 2,
                    CellHeight = 1,
                },
                new ImageControl(TerminalImageSource.FromRgba32(new byte[] { 0, 0, 255, 255 }, 1, 1, "blue-pixel"))
                {
                    CellWidth = 2,
                    CellHeight = 1,
                })
            .Spacing(1);

        using var driver = new TerminalAppTestDriver(
            root,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();

        var output = driver.Backend.GetOutText();
        var beginIndex = output.IndexOf("\x1b[?2026h", StringComparison.Ordinal);
        var firstImageIndex = output.IndexOf("\x1bP0;1q", StringComparison.Ordinal);
        var secondImageIndex = output.IndexOf("\x1bP0;1q", firstImageIndex + 1, StringComparison.Ordinal);
        var endIndex = output.LastIndexOf("\x1b[?2026l", StringComparison.Ordinal);

        Assert.IsTrue(beginIndex >= 0);
        Assert.IsTrue(firstImageIndex > beginIndex);
        Assert.IsTrue(secondImageIndex > firstImageIndex);
        Assert.IsTrue(endIndex > secondImageIndex);
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026h"));
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026l"));
        Assert.AreEqual(2, CountOccurrences(output, "\x1bP0;1q"));
    }

    [TestMethod]
    public void TerminalWrite_WithGraphicsPresenter_AppendsImageInsideSingleSynchronizedFlowOutput()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(16, 6));
        using var session = global::XenoAtom.Terminal.Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        using var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Sixel,
            MaxPayloadChunkBytes = 128,
        });
        var visual = new VStack(
                new TextBlock("before"),
                new ImageControl(CreateRedPixelSource())
                {
                    CellWidth = 2,
                    CellHeight = 1,
                },
                new TextBlock("after"))
            .Spacing(1);

        session.Instance.Write(visual, new TerminalWriteOptions { GraphicsPresenter = presenter });

        var output = backend.GetOutText();
        var beginIndex = output.IndexOf("\x1b[?2026h", StringComparison.Ordinal);
        var imageIndex = output.IndexOf("\x1bP0;1q", StringComparison.Ordinal);
        var endIndex = output.LastIndexOf("\x1b[?2026l", StringComparison.Ordinal);

        StringAssert.Contains(output, "before");
        StringAssert.Contains(output, "after");
        Assert.IsTrue(beginIndex >= 0);
        Assert.IsTrue(imageIndex > beginIndex, output.Replace("\x1b", "<ESC>", StringComparison.Ordinal));
        Assert.IsTrue(endIndex > imageIndex);
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026h"));
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026l"));
        Assert.AreEqual(1, CountOccurrences(output, "\x1bP0;1q"));
    }

    [TestMethod]
    public void TerminalWrite_ImageConvenienceOverload_UsesTerminalImageGraphicsPresenter()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(8, 3));
        var terminalOptions = new TerminalOptions { ImplicitStartInput = true };
        terminalOptions.Graphics.PreferredProtocol = TerminalGraphicsProtocol.Sixel;
        using var session = global::XenoAtom.Terminal.Terminal.Open(backend, terminalOptions, force: true);
        session.Instance.WriteLine("heading");

        session.Instance.Write(new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        });

        var output = backend.GetOutText();
        var beginIndex = output.IndexOf("\x1b[?2026h", StringComparison.Ordinal);
        var imageIndex = output.IndexOf("\x1bP0;1q", StringComparison.Ordinal);
        var endIndex = output.LastIndexOf("\x1b[?2026l", StringComparison.Ordinal);

        Assert.IsTrue(beginIndex >= 0);
        Assert.IsTrue(imageIndex > beginIndex, output.Replace("\x1b", "<ESC>", StringComparison.Ordinal));
        Assert.IsTrue(endIndex > imageIndex);
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026h"));
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026l"));
        Assert.AreEqual(1, CountOccurrences(output, "\x1bP0;1q"));
    }

    [TestMethod]
    public void TerminalWrite_StaticImageConvenienceOverload_UsesTerminalImageGraphicsPresenter()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(8, 3));
        var terminalOptions = new TerminalOptions { ImplicitStartInput = true };
        terminalOptions.Graphics.PreferredProtocol = TerminalGraphicsProtocol.Sixel;
        using var session = global::XenoAtom.Terminal.Terminal.Open(backend, terminalOptions, force: true);

        global::XenoAtom.Terminal.Terminal.Write(new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        });

        var output = backend.GetOutText();
        var beginIndex = output.IndexOf("\x1b[?2026h", StringComparison.Ordinal);
        var imageIndex = output.IndexOf("\x1bP0;1q", StringComparison.Ordinal);
        var endIndex = output.LastIndexOf("\x1b[?2026l", StringComparison.Ordinal);

        Assert.IsTrue(beginIndex >= 0);
        Assert.IsTrue(imageIndex > beginIndex, output.Replace("\x1b", "<ESC>", StringComparison.Ordinal));
        Assert.IsTrue(endIndex > imageIndex);
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026h"));
        Assert.AreEqual(1, CountOccurrences(output, "\x1b[?2026l"));
        Assert.AreEqual(1, CountOccurrences(output, "\x1bP0;1q"));
    }

    [TestMethod]
    [DataRow(TerminalGraphicsProtocol.ITerm2, "\x1b]1337;File=")]
    [DataRow(TerminalGraphicsProtocol.Sixel, "\x1bP0;1q")]
    public void TerminalImageGraphicsPresenter_ForcedStreamedProtocol_WritesImageEscapeSequence(TerminalGraphicsProtocol protocol, string expectedPrefix)
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = protocol,
            MaxPayloadChunkBytes = 128,
        });
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();

        StringAssert.Contains(driver.Backend.GetOutText(), expectedPrefix);
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_UsesConfiguredSixelOptions()
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Sixel,
            SixelOptions = new TerminalSixelEncoderOptions
            {
                PaletteMode = TerminalSixelPaletteMode.FixedRgb332,
                EnableDithering = false,
            },
        });
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();

        var output = driver.Backend.GetOutText();
        StringAssert.Contains(output, "\x1bP0;1q");
        StringAssert.Contains(output, "#224;2;100;0;0");
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_SkipsUnchangedStreamedImage_WhenOnlyGraphicsFrameIsRequested()
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Sixel,
            MaxPayloadChunkBytes = 128,
        });
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        var outputLength = driver.Backend.GetOutText().Length;

        driver.App.RequestGraphicsRender();
        driver.Tick();

        Assert.AreEqual(outputLength, driver.Backend.GetOutText().Length);
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_ReusesEncodedCache_WhenStaticStreamedImageIsRedrawn()
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Sixel,
            MaxPayloadChunkBytes = 128,
        });
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();

        Assert.AreEqual(1, presenter.Metrics.CacheMissCount);
        Assert.AreEqual(1, presenter.Metrics.CacheStoreCount);
        Assert.AreEqual(0, presenter.Metrics.CacheHitCount);

        driver.App.RequestFullRender();
        driver.TickUntil(() => presenter.Metrics.CacheHitCount == 1);

        Assert.AreEqual(1, presenter.Metrics.CacheHitCount);
        Assert.AreEqual(1, presenter.Metrics.CacheMissCount);
        Assert.AreEqual(1, presenter.Metrics.CacheStoreCount);
        Assert.AreEqual(1, presenter.Metrics.LastCacheHitCount);
        Assert.AreEqual(0, presenter.Metrics.LastCacheMissCount);
        Assert.AreEqual(0, presenter.Metrics.LastCacheStoreCount);
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_RedrawsStreamedImage_WhenRealtimeVersionChanges()
    {
        var source = new TestRealtimeImageSource();
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Sixel,
            MaxPayloadChunkBytes = 128,
        });
        var image = new ImageControl(source)
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        var outputLength = driver.Backend.GetOutText().Length;

        source.Publish(1);
        driver.TickUntil(() => driver.Backend.GetOutText().Length > outputLength);

        var appended = driver.Backend.GetOutText()[outputLength..];
        StringAssert.Contains(appended, "\x1bP0;1q");
        Assert.IsFalse(appended.Contains("\x1b[1;1H  \x1b[1;1H", StringComparison.Ordinal), "Realtime streamed redraws should overwrite the image directly instead of clearing the cell region first.");
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_RedrawsOnlyChangedRealtimeImage_WhenStaticImageIsAlsoVisible()
    {
        var realtime = new TestRealtimeImageSource();
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Sixel,
            MaxPayloadChunkBytes = 128,
        });
        var root = new HStack(
                new ImageControl(CreateRedPixelSource())
                {
                    CellWidth = 2,
                    CellHeight = 1,
                },
                new ImageControl(realtime)
                {
                    CellWidth = 2,
                    CellHeight = 1,
                })
            .Spacing(1);

        using var driver = new TerminalAppTestDriver(
            root,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        Assert.AreEqual(2, presenter.Metrics.EncodedFrameCount);
        var outputLength = driver.Backend.GetOutText().Length;

        realtime.Publish(1);
        driver.TickUntil(() => presenter.Metrics.EncodedFrameCount == 3);

        var appended = driver.Backend.GetOutText()[outputLength..];
        var beginIndex = appended.IndexOf("\x1b[?2026h", StringComparison.Ordinal);
        var imageIndex = appended.IndexOf("\x1bP0;1q", StringComparison.Ordinal);
        var endIndex = appended.LastIndexOf("\x1b[?2026l", StringComparison.Ordinal);

        Assert.IsTrue(beginIndex >= 0);
        Assert.IsTrue(imageIndex > beginIndex);
        Assert.IsTrue(endIndex > imageIndex);
        Assert.AreEqual(1, CountOccurrences(appended, "\x1b[?2026h"));
        Assert.AreEqual(1, CountOccurrences(appended, "\x1b[?2026l"));
        Assert.AreEqual(1, CountOccurrences(appended, "\x1bP0;1q"));
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_ClearsRemovedStreamedImage_AndRequestsTextRepaint()
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Sixel,
            MaxPayloadChunkBytes = 128,
        });
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };
        var host = new SingleChildHostVisual(image);

        using var driver = new TerminalAppTestDriver(
            host,
            TerminalHostKind.Fullscreen,
            new TerminalSize(12, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        var outputLength = driver.Backend.GetOutText().Length;

        host.SetChild(new TextBlock("after"));
        driver.App.RequestFullRender();
        driver.Tick();

        var afterClearLength = driver.Backend.GetOutText().Length;
        driver.Tick();

        var repaintOutput = driver.Backend.GetOutText()[afterClearLength..];
        StringAssert.Contains(repaintOutput, "after");
        Assert.IsTrue(afterClearLength > outputLength);
    }

    [TestMethod]
    public void TerminalApp_DoesNotPassCanceledTokenToFinalGraphicsPresentation()
    {
        var backend = new InMemoryTerminalBackend(new TerminalSize(12, 3));
        using var session = global::XenoAtom.Terminal.Terminal.Open(backend, new TerminalOptions { ImplicitStartInput = true }, force: true);
        var presenter = new CancellationCheckingGraphicsPresenter();

        session.Instance.Run(
            new TextBlock("done"),
            _ => TerminalLoopResult.Stop,
            new TerminalRunOptions { GraphicsPresenter = presenter });

        Assert.IsTrue(presenter.WasCalled);
        Assert.IsFalse(presenter.SawCancellationRequested);
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_UsesFallbackCellPixelSize_WhenMetricsAreUnavailable()
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Sixel,
            FallbackCellPixelWidth = 8,
            FallbackCellPixelHeight = 16,
        });
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();

        StringAssert.Contains(driver.Backend.GetOutText(), "\"1;1;16;16");
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_DeletesKittyImage_WhenVisualDisappears()
    {
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Kitty,
            MaxPayloadChunkBytes = 64,
        });
        var image = new ImageControl(CreateRedPixelSource())
        {
            CellWidth = 2,
            CellHeight = 1,
        };
        var host = new SingleChildHostVisual(image);

        using var driver = new TerminalAppTestDriver(
            host,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        var outputLength = driver.Backend.GetOutText().Length;

        host.SetChild(null);
        driver.App.RequestGraphicsRender();
        driver.Tick();

        var appended = driver.Backend.GetOutText()[outputLength..];
        StringAssert.Contains(appended, "a=d,d=i,i=");
    }

    [TestMethod]
    public void TerminalImageGraphicsPresenter_RecordsRealtimeMetrics()
    {
        var source = new TestRealtimeImageSource();
        var presenter = new TerminalImageGraphicsPresenter(new TerminalImageGraphicsPresenterOptions
        {
            Protocol = TerminalGraphicsProtocol.Kitty,
            MaxPayloadChunkBytes = 64,
        });
        var image = new ImageControl(source)
        {
            CellWidth = 2,
            CellHeight = 1,
        };

        using var driver = new TerminalAppTestDriver(
            image,
            TerminalHostKind.Fullscreen,
            new TerminalSize(8, 3),
            new TerminalAppOptions { GraphicsPresenter = presenter });

        driver.Tick();
        source.Publish(1);
        driver.TickUntil(() => presenter.Metrics.EncodedFrameCount >= 2);
        source.Publish(3);
        driver.TickUntil(() => presenter.Metrics.DroppedFrameCount == 1);

        Assert.IsTrue(presenter.Metrics.EncodedFrameCount >= 3);
        Assert.IsTrue(presenter.Metrics.PayloadByteCount > 0);
        Assert.IsTrue(presenter.Metrics.EffectiveFramesPerSecond > 0.0);
        Assert.AreEqual(1, presenter.Metrics.LastCommandCount);
        Assert.AreEqual(1, presenter.Metrics.LastEncodedFrameCount);
        Assert.AreEqual(1, presenter.Metrics.LastDroppedFrameCount);
        Assert.IsTrue(presenter.Metrics.LastPayloadByteCount > 0);

        var diagnostics = ((ITerminalGraphicsPresenterDiagnostics)presenter).GetDiagnosticsSnapshot();
        Assert.AreEqual("image", diagnostics.Name);
        Assert.AreEqual(TerminalGraphicsProtocol.Kitty, diagnostics.Protocol);
        Assert.AreEqual(presenter.Metrics.PresentationCount, diagnostics.PresentationCount);
        Assert.AreEqual(presenter.Metrics.LastCommandCount, diagnostics.LastCommandCount);
        Assert.AreEqual(presenter.Metrics.EncodedFrameCount, diagnostics.EncodedFrameCount);
        Assert.AreEqual(presenter.Metrics.LastEncodedFrameCount, diagnostics.LastEncodedFrameCount);
        Assert.AreEqual(presenter.Metrics.LastPayloadByteCount, diagnostics.LastPayloadByteCount);
        Assert.AreEqual(presenter.Metrics.LastDroppedFrameCount, diagnostics.LastDroppedFrameCount);
        Assert.AreEqual(presenter.Metrics.CacheHitCount, diagnostics.CacheHitCount);
        Assert.AreEqual(presenter.Metrics.CacheMissCount, diagnostics.CacheMissCount);
        Assert.AreEqual(presenter.Metrics.CacheStoreCount, diagnostics.CacheStoreCount);
        Assert.AreEqual(presenter.Metrics.LastCacheHitCount, diagnostics.LastCacheHitCount);
        Assert.AreEqual(presenter.Metrics.LastCacheMissCount, diagnostics.LastCacheMissCount);
        Assert.AreEqual(presenter.Metrics.LastCacheStoreCount, diagnostics.LastCacheStoreCount);
    }

    private static TerminalImageSource CreateRedPixelSource()
        => TerminalImageSource.FromRgba32(new byte[] { 255, 0, 0, 255 }, 1, 1, "red-pixel");

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var start = 0;
        while (true)
        {
            var index = text.IndexOf(value, start, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count++;
            start = index + value.Length;
        }
    }

    private sealed class TestRealtimeImageSource : TerminalImageSource, ITerminalRealtimeImageSource
    {
        private readonly byte[] _pixels = [255, 0, 0, 255];
        private long _version;

        public event EventHandler<TerminalImageFrameAvailableEventArgs>? FrameAvailable;

        public TimeSpan MinimumFrameInterval => TimeSpan.FromMilliseconds(16);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Publish(long version)
        {
            _version = version;
            FrameAvailable?.Invoke(this, new TerminalImageFrameAvailableEventArgs(version, TimeSpan.FromMilliseconds(version * 16)));
        }

        public override ValueTask<TerminalImageFrame?> GetFrameAsync(TerminalImageFrameRequest request, CancellationToken cancellationToken = default)
            => GetLatestFrameAsync(request, cancellationToken);

        public ValueTask<TerminalImageFrame?> GetLatestFrameAsync(TerminalImageFrameRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<TerminalImageFrame?>(new TerminalImageFrame
            {
                Format = TerminalImageFormat.RawRgba32,
                Data = _pixels,
                PixelWidth = 1,
                PixelHeight = 1,
                SourceId = "test-realtime-source",
                Version = _version,
                Timestamp = request.Timestamp ?? TimeSpan.Zero,
            });
        }
    }

    private sealed class SingleChildHostVisual : Visual
    {
        private Visual? _child;

        public SingleChildHostVisual(Visual? child)
        {
            SetChild(child);
        }

        protected override int ChildrenCount => _child is null ? 0 : 1;

        protected override Visual GetChild(int index)
            => index == 0 && _child is not null ? _child : throw new ArgumentOutOfRangeException(nameof(index));

        public void SetChild(Visual? child)
        {
            if (_child is not null)
            {
                DetachChild(_child);
            }

            _child = child;
            if (_child is not null)
            {
                AttachChild(_child);
            }
        }

        protected override SizeHints MeasureCore(in LayoutConstraints constraints)
            => _child is null ? SizeHints.Fixed(Size.Zero) : _child.Measure(constraints);

        protected override void ArrangeCore(in Rectangle finalRect)
        {
            _child?.Arrange(finalRect);
        }
    }

    private sealed class CancellationCheckingGraphicsPresenter : ITerminalGraphicsPresenter
    {
        public bool WasCalled { get; private set; }

        public bool SawCancellationRequested { get; private set; }

        public TerminalGraphicsCapabilities Capabilities => TerminalGraphicsCapabilities.None;

        public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, CancellationToken cancellationToken = default)
        {
            _ = current;
            _ = context;
            WasCalled = true;
            SawCancellationRequested |= cancellationToken.IsCancellationRequested;
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class RecordingGraphicsPresenter : ITerminalGraphicsPresenter
    {
        public List<GraphicsCommand[]> Frames { get; } = new();

        public List<ContextSnapshot> Contexts { get; } = new();

        public TerminalGraphicsCapabilities Capabilities => TerminalGraphicsCapabilities.None;

        public ValueTask PresentAsync(GraphicsCommandBuffer current, TerminalGraphicsPresentContext context, CancellationToken cancellationToken = default)
        {
            _ = context;
            _ = cancellationToken;
            Frames.Add(current.AsSpan().ToArray());
            Contexts.Add(new ContextSnapshot(context.HostKind));
            return ValueTask.CompletedTask;
        }

        public void ClearFrames()
        {
            Frames.Clear();
            Contexts.Clear();
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
        }
    }

    private readonly record struct ContextSnapshot(TerminalHostKind HostKind);
}
