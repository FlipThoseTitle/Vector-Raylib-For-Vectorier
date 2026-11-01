using Raylib_cs;
using System.Xml;
using Vector.Core.Animation;
using Vector.Core.Controllers;
using Vector.Core.Location;
using Vector.Core.Models;

namespace Vector.Core.Runners
{
    public class AreaRunner : QuadRunner
    {
        public enum AreaType
        {
            None,
            Trick,
            Help,
            Catch
        }

        private AreaType _Type;

        private readonly XmlNode _node;

        protected string _TypeName;

        /// <summary>The XML Type ("Trick", "Help", "Catch", an animation type...).</summary>
        public string TypeName => _TypeName;

        public bool IsNone => _Type == AreaType.None;

        public bool IsHelp => _Type == AreaType.Help;

        public bool IsTrick => _Type == AreaType.Trick;

        public bool IsCatch => _Type == AreaType.Catch;

        public AreaRunner(AreaType type, float x, float y, float width, float height, string typeName, string name)
            : base(x, y, width, height, false, string.IsNullOrEmpty(typeName) ? 1 : 0, name)
        {
            _Type = type;
            _TypeName = typeName;

            TypeClass = RunnerType.Area;
            LoadBinaryIfTrick();
        }

        /// <summary>A Trick area's move is loaded with the level (and again after Reload Moves).</summary>
        public void LoadBinaryIfTrick()
        {
            if (Animations.Animation.ContainsKey(Name.Replace("Trigger", "")))
            {
                AnimationInfo animationInfo = Animations.Animation[Name.Replace("Trigger", "")];
                if (animationInfo.IsTrick)
                {
                    AnimationTrickInfo.LoadAnimation(animationInfo as AnimationTrickInfo);
                }
            }
        }

        public virtual void Init()
        {

        }

        public override void Draw()
        {
            if (!IsEnabled)
                return;

            var vs = Sets.Current.Containers[1];
            var pos = vs.TransformPoint(rectangle.Origin);
            Raylib.DrawRectangleV(pos, rectangle.Size * WorldScale, new Color(255, 0, 0, 64));
        }

        public virtual bool OnKeyClick(KeyVariables Key)
        {
            return true;
        }

        public virtual void Activate(ModelHuman model)
        {
        }

        public virtual void Deactivate(ModelHuman model)
        {
        }
    }
}
