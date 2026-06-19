using Raylib_cs;

namespace Vector
{
    public static class ResourceManager
    {
        public static Dictionary<string, string> images = new();
        public static Dictionary<string, Texture2D> textureCache = new();

        public static Dictionary<string, string> sounds = new();
        public static Dictionary<string, Sound> soundCache = new();

        public static Dictionary<string, string> music = new();

        public static void LoadResources()
        {
            foreach (var imageFile in Directory.GetFiles(VectorPaths.Textures))
            {
                if (!imageFile.EndsWith(".png"))
                {
                    continue;
                }
                var fileName = Path.GetFileNameWithoutExtension(imageFile);
                if (!images.ContainsKey(fileName))
                {
                    images.Add(fileName, imageFile);
                }
            }

            foreach (var icon in Directory.GetFiles(VectorPaths.Icons))
            {
                var fileName = Path.GetFileNameWithoutExtension(icon);
                if (!images.ContainsKey(fileName))
                {
                    images.Add(fileName, icon);
                }
            }

            foreach (var soundFile in Directory.GetFiles(VectorPaths.Sounds))
            {
                var fileName = Path.GetFileNameWithoutExtension(soundFile);
                if (!sounds.ContainsKey(fileName))
                {
                    sounds.Add(fileName, soundFile);
                }
            }

            foreach (var musicFile in Directory.GetFiles(VectorPaths.Music))
            {
                var fileName = Path.GetFileNameWithoutExtension(musicFile);
                if (!sounds.ContainsKey(fileName))
                {
                    music.Add(fileName, musicFile);
                }
            }
        }

        public static byte[] GetBinary(string path)
        {
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

            var texture = Raylib.LoadTexture(images[name]);
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
                existingPath = p_path + ext;

                if (File.Exists(existingPath))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
