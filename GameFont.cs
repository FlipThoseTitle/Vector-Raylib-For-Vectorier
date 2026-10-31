using Raylib_cs;
using System.Numerics;

namespace Vector
{
    /// <summary>
    /// The game's UI font: Arial, loaded from the system (Windows, macOS, Linux). Linux usually has no Arial; the
    /// metric-compatible Liberation Sans / Arimo is used then, and raylib's default font as the last resort.
    /// </summary>
    public static class GameFont
    {
        private const int BaseSize = 64;

        private static Font? _font;

        public static Font Font => _font ??= Load();

        private static IEnumerable<string> Candidates()
        {
            if (OperatingSystem.IsWindows())
            {
                string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                yield return Path.Combine(fonts, "arial.ttf");
                yield return @"C:\Windows\Fonts\arial.ttf";
            }
            else if (OperatingSystem.IsMacOS())
            {
                yield return "/System/Library/Fonts/Supplemental/Arial.ttf";
                yield return "/Library/Fonts/Arial.ttf";
                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Fonts/Arial.ttf");
            }
            else
            {
                yield return "/usr/share/fonts/truetype/msttcorefonts/Arial.ttf";
                yield return "/usr/share/fonts/truetype/msttcorefonts/arial.ttf";
                yield return "/usr/share/fonts/msttcore/arial.ttf";
                yield return "/usr/share/fonts/TTF/arial.ttf";
                yield return "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf";
                yield return "/usr/share/fonts/liberation-sans/LiberationSans-Regular.ttf";
                yield return "/usr/share/fonts/TTF/LiberationSans-Regular.ttf";
                yield return "/usr/share/fonts/truetype/croscore/Arimo-Regular.ttf";
            }
            // A copy shipped next to the executable wins on any system that lacks it.
            yield return Path.Combine(VectorPaths.AppFolder, "arial.ttf");
        }

        private static Font Load()
        {
            foreach (var path in Candidates())
            {
                if (!File.Exists(path))
                    continue;
                var font = Raylib.LoadFontEx(path, BaseSize, null, 0);
                if (font.Texture.Id != 0)
                {
                    Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
                    return font;
                }
            }
            return Raylib.GetFontDefault();
        }

        public static void Draw(string text, float x, float y, float size, Color color) =>
            Raylib.DrawTextEx(Font, text, new Vector2(x, y), size, size / 10f, color);
    }
}
