using Raylib_cs;

namespace Vector
{
    public static class InputManager
    {
        public static bool WasKeyPressedThisFrame(KeyboardKey key)
        {
            return Raylib.IsKeyPressed(key);
        }

        public static bool IsKeyDown(KeyboardKey key)
        {
            return Raylib.IsKeyDown(key);
        }

        public static bool IsKeyUp(KeyboardKey key)
        {
            return Raylib.IsKeyUp(key);
        }

        public static KeyboardKey[] GetPressedKeys()
        {
            List<KeyboardKey> pressedKeys = new();

            foreach (KeyboardKey key in Enum.GetValues<KeyboardKey>())
            {
                if (Raylib.IsKeyDown(key))
                {
                    pressedKeys.Add(key);
                }
            }

            if (pressedKeys.Count == 0)
            {
                pressedKeys.Add(KeyboardKey.Null);
            }

            return pressedKeys.ToArray();
        }
    }
}
