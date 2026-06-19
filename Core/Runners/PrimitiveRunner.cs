using Raylib_cs;
using System.Numerics;
using Vector.Core.Models;

namespace Vector.Core.Runners
{
    public class PrimitiveRunner : Runner
    {
        private Vector3 _position = new Vector3();

        private Vector3 _DeltaPosition = new Vector3();

        private Color _Color;

        private ModelType _Type = ModelType.Primitive;

        private bool _IsUpdate;

        private bool _IsLoaded;

        private List<string> _Sounds;

        private ModelPrimitive _Model;

        private float _Impulse;

        protected int _type;

        public Vector3 Position
        {
            get => _position;
            set => _position = DeltaPosition + value;
        }

        public Vector3 DeltaPosition => _DeltaPosition == null ? new Vector3() : _DeltaPosition;

        public Color Color
        {
            get => _Color;
            set => _Color = value;
        }

        public ModelType Type
        {
            get => _Type;
            set => _Type = value;
        }

        public bool IsUpdate => _IsUpdate;

        public ModelPrimitive Model => _Model;

        public virtual bool IsStrike
        {
            get => _Model.IsStrike;
            set => _Model.IsStrike = value;
        }
        public Rectangle Rectangle => _Model.Rectangle;

        public PrimitiveRunner(int type, string name, Color color, Vector3 deltaPosition, float impulse, List<string> sounds)
            : base(0f, 0f)
        {
            _Color = color;
            _DeltaPosition = deltaPosition;
            Name = name;
            _Sounds = sounds;
            _Impulse = impulse;
            TypeClass = RunnerType.Primitive;
            _type = type;
        }

        public override void InitRunner(Vector2 point)
        {
            base.InitRunner(point);
            DefaultPosition = DefaultPosition.Set(new Vector2(_position.X, _position.Y));
        }

        public virtual void Load()
        {
            List<string> skins = new List<string> { Name + ".xml" };
            _Model = new ModelPrimitive(this, skins, _Sounds);
            _Model.Type = _Type;
            _Model.Color = _Color;
            _Model.Name = Name;
            _Model.IsEnabled = true;
            _Model.IsVisible = true;
            _Model.Impulse = _Impulse;
            _IsLoaded = true;
            _Model.Position(new Vector3(Position.X, Position.Y, 0));

        }

        public virtual void Render(List<QuadRunner> Platforms)
        {
            if (!IsEnabled || !_Model.IsStrike)
            {
                return;
            }
            CollisitionPlatform(Platforms);
            Update();
            _Model.ModelObject.RenderMacroNode();
            _Model.ControllerPhysics.Render();
            if (_Model.IsStrike)
            {
                if (_Model.RenderTime > 0)
                {
                    _Model.RenderTime--;
                    return;
                }
                _Model.IsVisible = false;
                _Model.ControllerPhysics.Stop();
            }
        }

        public override void Move(Vector2 point)
        {
            if (point == null)
            {
                return;
            }
            Position = new Vector3(point, 0);
            if (_Model != null)
                _Model.Position(new Vector3(Position.X, Position.Y, 0));
        }

        public void CollisitionPlatform(List<QuadRunner> quads)
        {
            foreach (var quad in quads)
            {
                _Model.ControllerCollisions.UpdatePlatform(quad);
            }
        }

        public void Update()
        {
            if (_Model.IsEnabled && _IsUpdate)
            {
                Position += _DeltaPosition;
            }
        }

        public override void Reset()
        {
            _IsUpdate = false;
            _Model.IsStrike = false;
            _Model.IsEnabled = true;
            _Model.ControllerPhysics.Stop();
            _Model.Reset();
            Move(new Vector2(Position.X - _DeltaPosition.X, Position.Y - _DeltaPosition.Y));
            _Model.IsVisible = true;
            _Model.ModelObject.RenderMacroNode();
            _Model.ControllerPhysics.Render();
        }
    }
}
