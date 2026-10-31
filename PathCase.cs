using System.Collections.Concurrent;

namespace Vector
{
    /// <summary>
    /// Linux and macOS file systems can be case-sensitive while Vector's XML names files the Windows way
    /// ("Buildings.xml" for buildings.xml). Resolve finds the file with any casing; on Windows it does nothing.
    /// </summary>
    public static class PathCase
    {
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Folders = new();

        public static string Resolve(string path)
        {
            if (OperatingSystem.IsWindows() || File.Exists(path) || Directory.Exists(path))
                return path;
            string? dir = Path.GetDirectoryName(path);
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name))
                return path;
            dir = Resolve(dir);
            if (!Directory.Exists(dir))
                return path;
            var entries = Folders.GetOrAdd(dir, d =>
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in Directory.EnumerateFileSystemEntries(d))
                    map.TryAdd(Path.GetFileName(entry), entry);
                return map;
            });
            return entries.TryGetValue(name, out var found) ? found : path;
        }
    }
}
