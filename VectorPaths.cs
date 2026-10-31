namespace Vector
{
    /// <summary>
    /// Where the game reads its content.
    ///
    /// Launched by Vectorier with <c>-project NAME</c>, the content is the editor's project folder: the game lives in
    /// <c>Data/Game/&lt;runtime&gt;/</c> (only the executable and the raylib library) and reads
    /// <c>Data/Projects/NAME/</c>, which uses the editor's layout (<c>xmlroot</c> instead of <c>xml</c>,
    /// trick icons in <c>icons/tricks</c>).
    ///
    /// Without <c>-project</c>, the original <c>Resources</c> folder next to the executable is used.
    /// </summary>
    public static class VectorPaths
    {
        /// <summary>Folder of the executable (not the working directory, which depends on how the game is started).</summary>
        public static readonly string AppFolder = AppContext.BaseDirectory;

        public static string Resources { get; private set; } = Path.Combine(AppFolder, "Resources");

        public static string Textures => Path.Combine(Resources, "textures");

        public static string AnimatedTextures => Path.Combine(Resources, "animatedtextures");

        /// <summary>Trick icons: icons/tricks in an editor project, icons in the original Resources.</summary>
        public static string Icons => FirstExisting(Path.Combine(Resources, "icons", "tricks"), Path.Combine(Resources, "icons"));

        public static string Sounds => Path.Combine(Resources, "sounds");

        public static string Music => Path.Combine(Resources, "music");

        public static string Animations => Path.Combine(Resources, "animations");

        public static string AnimationBinary => Path.Combine(Animations, "data");

        /// <summary>Level and object XML: xmlroot in an editor project, xml in the original Resources.</summary>
        public static string Xml => FirstExisting(Path.Combine(Resources, "xmlroot"), Path.Combine(Resources, "xml"));

        public static string XmlLevels => Path.Combine(Xml, "levels");

        public static string Models => Path.Combine(Resources, "models");

        public static string? ProjectName { get; private set; }

        private static string FirstExisting(string preferred, string fallback) =>
            Directory.Exists(preferred) || !Directory.Exists(fallback) ? preferred : fallback;

        /// <summary>
        /// Uses the editor project <paramref name="projectName"/>: Data/Projects/NAME, looked up from the executable's
        /// folder (Data/Game/&lt;runtime&gt;/) and, failing that, from the working directory. Returns false when the
        /// project cannot be found (the Resources folder stays in use).
        /// </summary>
        public static bool UseProject(string projectName)
        {
            foreach (var start in new[] { AppFolder, Directory.GetCurrentDirectory() })
            {
                var dir = new DirectoryInfo(start);
                // Data/Game/<runtime> → Data, and a few levels more in case the layout is deeper.
                for (int i = 0; i < 5 && dir != null; i++, dir = dir.Parent)
                {
                    string candidate = Path.Combine(dir.FullName, "Projects", projectName);
                    if (Directory.Exists(candidate))
                    {
                        Resources = candidate;
                        ProjectName = projectName;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
