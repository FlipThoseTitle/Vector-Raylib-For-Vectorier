using Raylib_cs;

namespace Vector.Core.Controllers
{
    public class KeyVariables
    {
        public const string UP = "Up";

        public const string DOWN = "Down";

        public const string LEFT = "Left";

        public const string RIGHT = "Right";

        public KeyboardKey Key
        {
            get;
        }

        public KeyVariables(string p_key)
        {
            Key = Parse(p_key);
        }

        public KeyVariables(KeyboardKey p_key)
        {
            Key = p_key;
        }

        public static KeyboardKey Parse(string p_key)
        {
            switch (p_key)
            {
                case "Up":
                    return KeyboardKey.Up;
                case "Down":
                    return KeyboardKey.Down;
                case "Left":
                    return KeyboardKey.Left;
                case "Right":
                    return KeyboardKey.Right;
            }
            return KeyboardKey.Null;
        }

        public bool IsEqual(KeyboardKey p_key)
        {
            return Key == p_key;
        }

        public bool IsEqual(KeyVariables p_keysVariables)
        {
            return Key == p_keysVariables.Key;
        }

        public override string ToString()
        {
            return Key.ToString();
        }
    }
}
