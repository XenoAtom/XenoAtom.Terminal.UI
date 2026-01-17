// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System.Text;

namespace XenoAtom.Terminal.UI.Figlet;

partial class FigletFont
{
    /// <summary>
    /// Gets the ThreeXFive FIGlet font used for rendering text in the FIGlet format. From https://www.figlet.org/fonts/3x5.flf.
    /// </summary>
    public static FigletFont ThreeXFive => ThreeXFiveFontHolder.Instance;

    /// <summary>
    /// Hold the ThreeXFive font instance for lazy initialization and NativeAOT friendliness.
    /// </summary>
    private static class ThreeXFiveFontHolder
    {
        public static readonly FigletFont Instance = FigletFont.Parse(ThreeXFiveFontData, new("3x5", "Richard Kirk", "https://www.figlet.org/fonts/3x5.flf"));

        // https://www.figlet.org/fonts/3x5.flf
        private const string ThreeXFiveFontData = """
                                                  flf2a$ 6 4 6 -1 4
                                                  3x5 font by Richard Kirk (rak@crosfield.co.uk).
                                                  Ported to figlet, and slightly changed (without permission :-})
                                                  by Daniel Cabeza Gras (bardo@dia.fi.upm.es)
                                                  
                                                      @
                                                      @
                                                      @
                                                      @
                                                      @
                                                      @@
                                                      @
                                                   #  @
                                                   #  @
                                                   #  @
                                                      @
                                                   #  @@
                                                      @
                                                  # # @
                                                  # # @
                                                      @
                                                      @
                                                      @@
                                                      @
                                                  # # @
                                                  ### @
                                                  # # @
                                                  ### @
                                                  # # @@
                                                      @
                                                   ## @
                                                  ##  @
                                                  ### @
                                                   ## @
                                                  ##  @@
                                                      @
                                                  # # @
                                                    # @
                                                   #  @
                                                  #   @
                                                  # # @@
                                                      @
                                                   #  @
                                                  #   @
                                                   ## @
                                                  # # @
                                                  ### @@
                                                      @
                                                    # @
                                                   #  @
                                                  #   @
                                                      @
                                                      @@
                                                      @
                                                    # @
                                                   #  @
                                                   #  @
                                                   #  @
                                                    # @@
                                                      @
                                                  #   @
                                                   #  @
                                                   #  @
                                                   #  @
                                                  #   @@
                                                      @
                                                   #  @
                                                  ### @
                                                   #  @
                                                  ### @
                                                   #  @@
                                                      @
                                                      @
                                                   #  @
                                                  ### @
                                                   #  @
                                                      @@
                                                      @
                                                      @
                                                      @
                                                      @
                                                   #  @
                                                  #   @@
                                                      @
                                                      @
                                                      @
                                                  ### @
                                                      @
                                                      @@
                                                      @
                                                      @
                                                      @
                                                      @
                                                      @
                                                   #  @@
                                                      @
                                                    # @
                                                    # @
                                                   #  @
                                                  #   @
                                                  #   @@
                                                      @
                                                  ### @
                                                  # # @
                                                  # # @
                                                  # # @
                                                  ### @@
                                                      @
                                                   #  @
                                                  ##  @
                                                   #  @
                                                   #  @
                                                  ### @@
                                                      @
                                                  ### @
                                                    # @
                                                  ### @
                                                  #   @
                                                  ### @@
                                                      @
                                                  ### @
                                                    # @
                                                   ## @
                                                    # @
                                                  ### @@
                                                      @
                                                  # # @
                                                  # # @
                                                  ### @
                                                    # @
                                                    # @@
                                                      @
                                                  ### @
                                                  #   @
                                                  ### @
                                                    # @
                                                  ### @@
                                                      @
                                                  ### @
                                                  #   @
                                                  ### @
                                                  # # @
                                                  ### @@
                                                      @
                                                  ### @
                                                    # @
                                                    # @
                                                    # @
                                                    # @@
                                                      @
                                                  ### @
                                                  # # @
                                                  ### @
                                                  # # @
                                                  ### @@
                                                      @
                                                  ### @
                                                  # # @
                                                  ### @
                                                    # @
                                                  ### @@
                                                      @
                                                      @
                                                   #  @
                                                      @
                                                   #  @
                                                      @@
                                                      @
                                                      @
                                                   #  @
                                                      @
                                                   #  @
                                                  #   @@
                                                      @
                                                    # @
                                                   #  @
                                                  #   @
                                                   #  @
                                                    # @@
                                                      @
                                                      @
                                                  ### @
                                                      @
                                                  ### @
                                                      @@
                                                      @
                                                  #   @
                                                   #  @
                                                    # @
                                                   #  @
                                                  #   @@
                                                      @
                                                  ### @
                                                    # @
                                                   ## @
                                                      @
                                                   #  @@
                                                      @
                                                  ### @
                                                  # # @
                                                  #   @
                                                  ### @
                                                      @@
                                                      @
                                                   #  @
                                                  # # @
                                                  ### @
                                                  # # @
                                                  # # @@
                                                      @
                                                  ##  @
                                                  # # @
                                                  ##  @
                                                  # # @
                                                  ##  @@
                                                      @
                                                   ## @
                                                  #   @
                                                  #   @
                                                  #   @
                                                   ## @@
                                                      @
                                                  ##  @
                                                  # # @
                                                  # # @
                                                  # # @
                                                  ##  @@
                                                      @
                                                  ### @
                                                  #   @
                                                  ##  @
                                                  #   @
                                                  ### @@
                                                      @
                                                  ### @
                                                  #   @
                                                  ##  @
                                                  #   @
                                                  #   @@
                                                      @
                                                   ## @
                                                  #   @
                                                  # # @
                                                  # # @
                                                   ## @@
                                                      @
                                                  # # @
                                                  # # @
                                                  ### @
                                                  # # @
                                                  # # @@
                                                      @
                                                  ### @
                                                   #  @
                                                   #  @
                                                   #  @
                                                  ### @@
                                                      @
                                                   ## @
                                                    # @
                                                    # @
                                                  # # @
                                                   #  @@
                                                      @
                                                  # # @
                                                  # # @
                                                  ##  @
                                                  # # @
                                                  # # @@
                                                      @
                                                  #   @
                                                  #   @
                                                  #   @
                                                  #   @
                                                  ### @@
                                                      @
                                                  # # @
                                                  ### @
                                                  ### @
                                                  # # @
                                                  # # @@
                                                      @
                                                  ### @
                                                  # # @
                                                  # # @
                                                  # # @
                                                  # # @@
                                                      @
                                                   #  @
                                                  # # @
                                                  # # @
                                                  # # @
                                                   #  @@
                                                      @
                                                  ##  @
                                                  # # @
                                                  ##  @
                                                  #   @
                                                  #   @@
                                                      @
                                                   #  @
                                                  # # @
                                                  # # @
                                                   ## @
                                                    # @@
                                                      @
                                                  ##  @
                                                  # # @
                                                  ##  @
                                                  # # @
                                                  # # @@
                                                      @
                                                   ## @
                                                  #   @
                                                   #  @
                                                    # @
                                                  ##  @@
                                                      @
                                                  ### @
                                                   #  @
                                                   #  @
                                                   #  @
                                                   #  @@
                                                      @
                                                  # # @
                                                  # # @
                                                  # # @
                                                  # # @
                                                  ### @@
                                                      @
                                                  # # @
                                                  # # @
                                                  # # @
                                                  # # @
                                                   #  @@
                                                      @
                                                  # # @
                                                  # # @
                                                  ### @
                                                  ### @
                                                  # # @@
                                                      @
                                                  # # @
                                                  # # @
                                                   #  @
                                                  # # @
                                                  # # @@
                                                      @
                                                  # # @
                                                  # # @
                                                   #  @
                                                   #  @
                                                   #  @@
                                                      @
                                                  ### @
                                                    # @
                                                   #  @
                                                  #   @
                                                  ### @@
                                                      @
                                                   ## @
                                                   #  @
                                                   #  @
                                                   #  @
                                                   ## @@
                                                      @
                                                  #   @
                                                  #   @
                                                   #  @
                                                    # @
                                                    # @@
                                                      @
                                                  ##  @
                                                   #  @
                                                   #  @
                                                   #  @
                                                  ##  @@
                                                      @
                                                   #  @
                                                  # # @
                                                      @
                                                      @
                                                      @@
                                                      @
                                                      @
                                                      @
                                                      @
                                                      @
                                                  ### @@
                                                      @
                                                  #   @
                                                   #  @
                                                    # @
                                                      @
                                                      @@
                                                      @
                                                      @
                                                   ## @
                                                  # # @
                                                  ### @
                                                      @@
                                                      @
                                                  #   @
                                                  ### @
                                                  # # @
                                                  ### @
                                                      @@
                                                      @
                                                      @
                                                  ### @
                                                  #   @
                                                  ### @
                                                      @@
                                                      @
                                                    # @
                                                  ### @
                                                  # # @
                                                  ### @
                                                      @@
                                                      @
                                                      @
                                                  ### @
                                                  ##  @
                                                  ### @
                                                      @@
                                                      @
                                                   ## @
                                                   #  @
                                                  ### @
                                                   #  @
                                                  ##  @@
                                                      @
                                                      @
                                                  ### @
                                                  # # @
                                                   ## @
                                                  ### @@
                                                      @
                                                  #   @
                                                  ### @
                                                  # # @
                                                  # # @
                                                      @@
                                                      @
                                                   #  @
                                                      @
                                                   #  @
                                                   ## @
                                                      @@
                                                      @
                                                   #  @
                                                      @
                                                   #  @
                                                   #  @
                                                  #   @@
                                                      @
                                                  #   @
                                                  # # @
                                                  ##  @
                                                  # # @
                                                      @@
                                                      @
                                                   #  @
                                                   #  @
                                                   #  @
                                                   ## @
                                                      @@
                                                      @
                                                      @
                                                  ### @
                                                  ### @
                                                  # # @
                                                      @@
                                                      @
                                                      @
                                                  ##  @
                                                  # # @
                                                  # # @
                                                      @@
                                                      @
                                                      @
                                                  ### @
                                                  # # @
                                                  ### @
                                                      @@
                                                      @
                                                      @
                                                  ### @
                                                  # # @
                                                  ### @
                                                  #   @@
                                                      @
                                                      @
                                                  ### @
                                                  # # @
                                                  ### @
                                                    # @@
                                                      @
                                                      @
                                                  ### @
                                                  #   @
                                                  #   @
                                                      @@
                                                      @
                                                      @
                                                   ## @
                                                   #  @
                                                  ##  @
                                                      @@
                                                      @
                                                   #  @
                                                  ### @
                                                   #  @
                                                   ## @
                                                      @@
                                                      @
                                                      @
                                                  # # @
                                                  # # @
                                                  ### @
                                                      @@
                                                      @
                                                      @
                                                  # # @
                                                  # # @
                                                   #  @
                                                      @@
                                                      @
                                                      @
                                                  # # @
                                                  ### @
                                                  ### @
                                                      @@
                                                      @
                                                      @
                                                  # # @
                                                   #  @
                                                  # # @
                                                      @@
                                                      @
                                                      @
                                                  # # @
                                                  ### @
                                                    # @
                                                  ### @@
                                                      @
                                                      @
                                                  ##  @
                                                   #  @
                                                   ## @
                                                      @@
                                                      @
                                                   ## @
                                                   #  @
                                                  ##  @
                                                   #  @
                                                   ## @@
                                                      @
                                                   #  @
                                                   #  @
                                                   #  @
                                                   #  @
                                                   #  @@
                                                      @
                                                  ##  @
                                                   #  @
                                                   ## @
                                                   #  @
                                                  ##  @@
                                                      @
                                                    # @
                                                  ### @
                                                  #   @
                                                      @
                                                      @@
                                                      @
                                                  # # @
                                                   #  @
                                                  # # @
                                                  ### @
                                                  # # @@
                                                      @
                                                  # # @
                                                  ### @
                                                  # # @
                                                  # # @
                                                  ### @@
                                                      @
                                                  # # @
                                                      @
                                                  # # @
                                                  # # @
                                                  ### @@
                                                      @
                                                  # # @
                                                   ## @
                                                  # # @
                                                  ### @
                                                      @@
                                                      @
                                                  # # @
                                                  ### @
                                                  # # @
                                                  ### @
                                                      @@
                                                      @
                                                  # # @
                                                      @
                                                  # # @
                                                  ### @
                                                      @@
                                                      @
                                                  ### @
                                                  ##  @
                                                  # # @
                                                  ##  @
                                                  #   @@
                                                  """;
    }
}
