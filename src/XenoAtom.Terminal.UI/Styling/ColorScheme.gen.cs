// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using XenoAtom.Ansi;

namespace XenoAtom.Terminal.UI.Styling;

partial record ColorScheme
{
    /// <summary>
    /// Returns a list of all predefined <see cref="ColorScheme"/> instances.
    /// </summary>
    public static List<ColorScheme> GetPredefinedSchemes()
        => [
            RetroBlue,
            CherryDark,
            CherryDarkSoft,
            CherryLightSoft,
            CherryLight,
            TomatoDark,
            TomatoDarkSoft,
            TomatoLightSoft,
            TomatoLight,
            OrangeDark,
            OrangeDarkSoft,
            OrangeLightSoft,
            OrangeLight,
            PineappleDark,
            PineappleDarkSoft,
            PineappleLightSoft,
            PineappleLight,
            AppleDark,
            AppleDarkSoft,
            AppleLightSoft,
            AppleLight,
            KiwiDark,
            KiwiDarkSoft,
            KiwiLightSoft,
            KiwiLight,
            KaleDark,
            KaleDarkSoft,
            KaleLightSoft,
            KaleLight,
            BlueberryDark,
            BlueberryDarkSoft,
            BlueberryLightSoft,
            BlueberryLight,
            PlumDark,
            PlumDarkSoft,
            PlumLightSoft,
            PlumLight,
            ElderberryDark,
            ElderberryDarkSoft,
            ElderberryLightSoft,
            ElderberryLight,
            BlackberryDark,
            BlackberryDarkSoft,
            BlackberryLightSoft,
            BlackberryLight,
            RaspberryDark,
            RaspberryDarkSoft,
            RaspberryLightSoft,
            RaspberryLight,
        ];


    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Elderberry</c> color and <c>Splash</c> milk in <c>Blue</c> mode.
    /// </summary>
    public static ColorScheme RetroBlue => RetroBlueHolder.Instance;

    private static class RetroBlueHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(7, 10, 10, RootLoopsFlavor.Classic, RootLoopsFruit.Elderberry, RootLoopsMilkAmount.Splash, "Retro Blue");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Cherry</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme CherryDark => CherryDarkHolder.Instance;

    private static class CherryDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Cherry, RootLoopsMilkAmount.None, "Cherry Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Cherry</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme CherryDarkSoft => CherryDarkSoftHolder.Instance;

    private static class CherryDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Cherry, RootLoopsMilkAmount.Splash, "Cherry Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Cherry</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme CherryLightSoft => CherryLightSoftHolder.Instance;

    private static class CherryLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Cherry, RootLoopsMilkAmount.Glug, "Cherry Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Cherry</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme CherryLight => CherryLightHolder.Instance;

    private static class CherryLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Cherry, RootLoopsMilkAmount.Cup, "Cherry Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Tomato</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme TomatoDark => TomatoDarkHolder.Instance;

    private static class TomatoDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Tomato, RootLoopsMilkAmount.None, "Tomato Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Tomato</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme TomatoDarkSoft => TomatoDarkSoftHolder.Instance;

    private static class TomatoDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Tomato, RootLoopsMilkAmount.Splash, "Tomato Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Tomato</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme TomatoLightSoft => TomatoLightSoftHolder.Instance;

    private static class TomatoLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Tomato, RootLoopsMilkAmount.Glug, "Tomato Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Tomato</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme TomatoLight => TomatoLightHolder.Instance;

    private static class TomatoLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Tomato, RootLoopsMilkAmount.Cup, "Tomato Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Orange</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme OrangeDark => OrangeDarkHolder.Instance;

    private static class OrangeDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Orange, RootLoopsMilkAmount.None, "Orange Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Orange</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme OrangeDarkSoft => OrangeDarkSoftHolder.Instance;

    private static class OrangeDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Orange, RootLoopsMilkAmount.Splash, "Orange Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Orange</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme OrangeLightSoft => OrangeLightSoftHolder.Instance;

    private static class OrangeLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Orange, RootLoopsMilkAmount.Glug, "Orange Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Orange</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme OrangeLight => OrangeLightHolder.Instance;

    private static class OrangeLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Orange, RootLoopsMilkAmount.Cup, "Orange Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Pineapple</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme PineappleDark => PineappleDarkHolder.Instance;

    private static class PineappleDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Pineapple, RootLoopsMilkAmount.None, "Pineapple Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Pineapple</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme PineappleDarkSoft => PineappleDarkSoftHolder.Instance;

    private static class PineappleDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Pineapple, RootLoopsMilkAmount.Splash, "Pineapple Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Pineapple</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme PineappleLightSoft => PineappleLightSoftHolder.Instance;

    private static class PineappleLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Pineapple, RootLoopsMilkAmount.Glug, "Pineapple Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Pineapple</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme PineappleLight => PineappleLightHolder.Instance;

    private static class PineappleLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Pineapple, RootLoopsMilkAmount.Cup, "Pineapple Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Apple</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme AppleDark => AppleDarkHolder.Instance;

    private static class AppleDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Apple, RootLoopsMilkAmount.None, "Apple Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Apple</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme AppleDarkSoft => AppleDarkSoftHolder.Instance;

    private static class AppleDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Apple, RootLoopsMilkAmount.Splash, "Apple Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Apple</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme AppleLightSoft => AppleLightSoftHolder.Instance;

    private static class AppleLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Apple, RootLoopsMilkAmount.Glug, "Apple Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Apple</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme AppleLight => AppleLightHolder.Instance;

    private static class AppleLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Apple, RootLoopsMilkAmount.Cup, "Apple Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Kiwi</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme KiwiDark => KiwiDarkHolder.Instance;

    private static class KiwiDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Kiwi, RootLoopsMilkAmount.None, "Kiwi Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Kiwi</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme KiwiDarkSoft => KiwiDarkSoftHolder.Instance;

    private static class KiwiDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Kiwi, RootLoopsMilkAmount.Splash, "Kiwi Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Kiwi</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme KiwiLightSoft => KiwiLightSoftHolder.Instance;

    private static class KiwiLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Kiwi, RootLoopsMilkAmount.Glug, "Kiwi Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Kiwi</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme KiwiLight => KiwiLightHolder.Instance;

    private static class KiwiLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Kiwi, RootLoopsMilkAmount.Cup, "Kiwi Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Kale</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme KaleDark => KaleDarkHolder.Instance;

    private static class KaleDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Kale, RootLoopsMilkAmount.None, "Kale Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Kale</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme KaleDarkSoft => KaleDarkSoftHolder.Instance;

    private static class KaleDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Kale, RootLoopsMilkAmount.Splash, "Kale Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Kale</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme KaleLightSoft => KaleLightSoftHolder.Instance;

    private static class KaleLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Kale, RootLoopsMilkAmount.Glug, "Kale Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Kale</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme KaleLight => KaleLightHolder.Instance;

    private static class KaleLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Kale, RootLoopsMilkAmount.Cup, "Kale Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Blueberry</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme BlueberryDark => BlueberryDarkHolder.Instance;

    private static class BlueberryDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Blueberry, RootLoopsMilkAmount.None, "Blueberry Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Blueberry</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme BlueberryDarkSoft => BlueberryDarkSoftHolder.Instance;

    private static class BlueberryDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Blueberry, RootLoopsMilkAmount.Splash, "Blueberry Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Blueberry</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme BlueberryLightSoft => BlueberryLightSoftHolder.Instance;

    private static class BlueberryLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Blueberry, RootLoopsMilkAmount.Glug, "Blueberry Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Blueberry</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme BlueberryLight => BlueberryLightHolder.Instance;

    private static class BlueberryLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Blueberry, RootLoopsMilkAmount.Cup, "Blueberry Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Plum</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme PlumDark => PlumDarkHolder.Instance;

    private static class PlumDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Plum, RootLoopsMilkAmount.None, "Plum Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Plum</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme PlumDarkSoft => PlumDarkSoftHolder.Instance;

    private static class PlumDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Plum, RootLoopsMilkAmount.Splash, "Plum Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Plum</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme PlumLightSoft => PlumLightSoftHolder.Instance;

    private static class PlumLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Plum, RootLoopsMilkAmount.Glug, "Plum Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Plum</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme PlumLight => PlumLightHolder.Instance;

    private static class PlumLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Plum, RootLoopsMilkAmount.Cup, "Plum Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Elderberry</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme ElderberryDark => ElderberryDarkHolder.Instance;

    private static class ElderberryDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Elderberry, RootLoopsMilkAmount.None, "Elderberry Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Elderberry</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme ElderberryDarkSoft => ElderberryDarkSoftHolder.Instance;

    private static class ElderberryDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Elderberry, RootLoopsMilkAmount.Splash, "Elderberry Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Elderberry</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme ElderberryLightSoft => ElderberryLightSoftHolder.Instance;

    private static class ElderberryLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Elderberry, RootLoopsMilkAmount.Glug, "Elderberry Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Elderberry</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme ElderberryLight => ElderberryLightHolder.Instance;

    private static class ElderberryLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Elderberry, RootLoopsMilkAmount.Cup, "Elderberry Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Blackberry</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme BlackberryDark => BlackberryDarkHolder.Instance;

    private static class BlackberryDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Blackberry, RootLoopsMilkAmount.None, "Blackberry Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Blackberry</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme BlackberryDarkSoft => BlackberryDarkSoftHolder.Instance;

    private static class BlackberryDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Blackberry, RootLoopsMilkAmount.Splash, "Blackberry Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Blackberry</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme BlackberryLightSoft => BlackberryLightSoftHolder.Instance;

    private static class BlackberryLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Blackberry, RootLoopsMilkAmount.Glug, "Blackberry Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Blackberry</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme BlackberryLight => BlackberryLightHolder.Instance;

    private static class BlackberryLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Blackberry, RootLoopsMilkAmount.Cup, "Blackberry Light");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Raspberry</c> color and <c>None</c> milk in <c>Dark</c> mode.
    /// </summary>
    public static ColorScheme RaspberryDark => RaspberryDarkHolder.Instance;

    private static class RaspberryDarkHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Raspberry, RootLoopsMilkAmount.None, "Raspberry Dark");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Raspberry</c> color and <c>Splash</c> milk in <c>Dark Soft</c> mode.
    /// </summary>
    public static ColorScheme RaspberryDarkSoft => RaspberryDarkSoftHolder.Instance;

    private static class RaspberryDarkSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Raspberry, RootLoopsMilkAmount.Splash, "Raspberry Dark Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Raspberry</c> color and <c>Glug</c> milk in <c>Light Soft</c> mode.
    /// </summary>
    public static ColorScheme RaspberryLightSoft => RaspberryLightSoftHolder.Instance;

    private static class RaspberryLightSoftHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Raspberry, RootLoopsMilkAmount.Glug, "Raspberry Light Soft");
    }

    /// <summary>
    /// A <see cref="ColorScheme"/> with <c>Raspberry</c> color and <c>Cup</c> milk in <c>Light</c> mode.
    /// </summary>
    public static ColorScheme RaspberryLight => RaspberryLightHolder.Instance;

    private static class RaspberryLightHolder
    {
        public static readonly ColorScheme Instance = ColorScheme.Generate(6, 8, 4, RootLoopsFlavor.Intense, RootLoopsFruit.Raspberry, RootLoopsMilkAmount.Cup, "Raspberry Light");
    }
}