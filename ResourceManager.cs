using Raylib_cs;
using System.Collections.Concurrent;

namespace Vector
{
    public static class ResourceManager
    {
        public static Dictionary<string, string> images = new();
        public static Dictionary<string, Texture2D> textureCache = new();

        public static Dictionary<string, string> sounds = new();
        public static Dictionary<string, Sound> soundCache = new();

        public static Dictionary<string, string> music = new();

        /// <summary>Files of a folder; nothing when the folder does not exist (a project may lack music, icons...).</summary>
        private static string[] FilesIn(string folder) => Directory.Exists(folder) ? Directory.GetFiles(folder) : Array.Empty<string>();

        public static void LoadResources()
        {
            foreach (var imageFile in FilesIn(VectorPaths.Textures))
            {
                if (!imageFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var fileName = Path.GetFileNameWithoutExtension(imageFile);
                if (!images.ContainsKey(fileName))
                {
                    images.Add(fileName, imageFile);
                }
            }

            foreach (var icon in FilesIn(VectorPaths.Icons))
            {
                var fileName = Path.GetFileNameWithoutExtension(icon);
                if (!images.ContainsKey(fileName))
                {
                    images.Add(fileName, icon);
                }
            }

            foreach (var soundFile in FilesIn(VectorPaths.Sounds))
            {
                var fileName = Path.GetFileNameWithoutExtension(soundFile);
                if (!sounds.ContainsKey(fileName))
                {
                    sounds.Add(fileName, soundFile);
                }
            }

            foreach (var musicFile in FilesIn(VectorPaths.Music))
            {
                var fileName = Path.GetFileNameWithoutExtension(musicFile);
                music.TryAdd(fileName, musicFile);
            }
        }

        /// <summary>Images decoded ahead of time (on worker threads), waiting for their GPU upload in GetTexture.</summary>
        private static readonly ConcurrentDictionary<string, Image> prefetched = new();

        /// <summary>
        /// Decodes the PNGs of <paramref name="names"/> in parallel. Decoding is CPU work and safe off the main thread;
        /// only the texture upload (GetTexture) has to stay on it.
        /// </summary>
        public static void PrefetchImages(IEnumerable<string> names)
        {
            var todo = names.Where(n => n != null && images.ContainsKey(n) && !textureCache.ContainsKey(n) && !prefetched.ContainsKey(n))
                .Distinct().ToList();
            if (todo.Count < 2)
                return;
            Parallel.ForEach(todo, name =>
            {
                try
                {
                    byte[] data = File.ReadAllBytes(images[name]);
                    var image = Raylib.LoadImageFromMemory(".png", data);
                    if (image.Width > 0 && image.Height > 0)
                        prefetched[name] = image;
                }
                catch
                {
                    // Left to the normal (main thread) loading.
                }
            });
        }

        /// <summary>Frees decoded images that were not used.</summary>
        public static void ReleasePrefetched()
        {
            foreach (var name in prefetched.Keys.ToList())
                if (prefetched.TryRemove(name, out var image))
                    Raylib.UnloadImage(image);
        }

        public static byte[] GetBinary(string path)
        {
            path = PathCase.Resolve(path);
            if (!File.Exists(path))
            {
                return null;
            }
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] array = new byte[fileStream.Length];
            fileStream.Read(array, 0, (int)fileStream.Length);
            fileStream.Close();
            return array;
        }

        public static Texture2D GetTexture(string name)
        {
            if (!images.ContainsKey(name))
            {
                return default(Texture2D);
            }

            if (textureCache.ContainsKey(name))
            {
                return textureCache[name];
            }

            Texture2D texture;
            if (prefetched.TryRemove(name, out var image))
            {
                texture = Raylib.LoadTextureFromImage(image);
                Raylib.UnloadImage(image);
            }
            else
            {
                texture = Raylib.LoadTexture(images[name]);
            }
            Raylib.SetTextureWrap(texture, TextureWrap.Clamp);
            Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
            textureCache.Add(name, texture);
            return texture;
        }

        public static Sound GetSound(string name)
        {
            if (!sounds.ContainsKey(name))
            {
                return default(Sound);
            }

            if (soundCache.ContainsKey(name))
            {
                return soundCache[name];
            }

            var sound = Raylib.LoadSound(sounds[name]);
            soundCache.Add(name, sound);
            return sound;
        }

        public static Music GetMusic(string name)
        {
            if (!music.ContainsKey(name))
            {
                return default;
            }
            var musicSound = Raylib.LoadMusicStream(music[name]);
            return musicSound;
        }

        public static void ClearTextureCache()
        {
            foreach (var texture in textureCache.Values)
            {
                Raylib.UnloadTexture(texture);
            }
            textureCache.Clear();
            GC.Collect();
        }

        public static void ClearSoundCache()
        {
            foreach (var sound in soundCache.Values)
            {
                Raylib.UnloadSound(sound);
            }
            soundCache.Clear();
            GC.Collect();
        }

        public static bool FileExists(string p_path, out string existingPath, params string[] exts)
        {
            existingPath = p_path;

            foreach (var ext in exts)
            {
                existingPath = PathCase.Resolve(p_path + ext);

                if (File.Exists(existingPath))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
