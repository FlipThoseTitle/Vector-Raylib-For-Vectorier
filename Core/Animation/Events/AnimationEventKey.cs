using Raylib_cs;
using System.Xml;
using Vector.Core.Controllers;

namespace Vector.Core.Animation.Events
{
    public class AnimationEventKey : AnimationEvent
    {
        private readonly KeyboardKey _key;

        public AnimationEventKey(AnimationEventParam param, XmlNode node)
            : base(param)
        {
            _key = KeyVariables.Parse(node.Attributes["Key"].Value);
        }

        public bool IsKey(KeyVariables keysVariables, int sign)
        {
            if (sign == -1 && keysVariables.Key == KeyboardKey.Right)
            {
                return _key == KeyboardKey.Left;
            }
            if (sign == -1 && keysVariables.Key == KeyboardKey.Left)
            {
                return _key == KeyboardKey.Right;
            }
            return _key == keysVariables.Key;
        }
    }
}
