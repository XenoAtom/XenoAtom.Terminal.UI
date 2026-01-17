// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

namespace XenoAtom.Terminal.UI.Figlet;

/// <summary>
/// Represents metadata information about a FIGlet font, including its name, author, source URL, height, and hardblank
/// character.
/// </summary>
/// <remarks>This record is typically used to describe the properties of a FIGlet font for display or selection
/// purposes. The hardblank character is a special character defined in the FIGlet font specification.</remarks>
/// <param name="Name">The display name of the FIGlet font, or null if unspecified.</param>
/// <param name="Author">The name of the font's author, or null if unknown.</param>
/// <param name="Url">The URL where the font can be found or referenced, or null if not available.</param>
public sealed record FigletFontInfo(string? Name, string? Author = null, string? Url = null);