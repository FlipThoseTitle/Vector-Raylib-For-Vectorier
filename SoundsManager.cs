using Raylib_cs;

namespace Vector
{
    public class SoundsManager
    {
        public static Music CurrentMusic;

        public static void PlaySounds(SoundType sound)
        {
            var audio = ResourceManager.GetSound(sound.ToString());
            Raylib.PlaySound(audio);
        }

        public static void PlaySoundsOnce(string name, float vol)
        {
            var sound = ResourceManager.GetSound(name);
            Raylib.SetSoundVolume(sound, vol);
            Raylib.PlaySound(sound);
        }

        public static void PlaySoundsOnce(SoundType sound, float vol)
        {
            var audio = ResourceManager.GetSound(sound.ToString());
            Raylib.SetSoundVolume(audio, vol);
            Raylib.PlaySound(audio);
        }

        public static void PlaySounds(string name)
        {
            var sound = ResourceManager.GetSound(name);
            Raylib.PlaySound(sound);
        }

        /// <summary>Music muted (M): the music keeps playing silently, so unmuting picks it up where it is.</summary>
        public static bool MusicMuted;

        public static void PlayMusic(string name)
        {
            Console.WriteLine("PLAY MUSIC " + name);
            var music = ResourceManager.GetMusic(name);
            CurrentMusic = music;
            Raylib.PlayMusicStream(music);
            ApplyMusicVolume();
        }

        public static void ToggleMusicMute()
        {
            MusicMuted = !MusicMuted;
            ApplyMusicVolume();
        }

        static void ApplyMusicVolume()
        {
            if (CurrentMusic.FrameCount > 0)
                Raylib.SetMusicVolume(CurrentMusic, MusicMuted ? 0f : 1f);
        }
    }
}
