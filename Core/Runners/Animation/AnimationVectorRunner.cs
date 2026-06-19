using System.Numerics;

namespace Vector.Core.Runners.Animation
{
    public class AnimationVectorRunner : AnimationRunner
    {
        private Vector2 _Direction = new Vector2();

        private Vector2 _Acceleration = new Vector2();

        private Vector2 _DirectionResult = new Vector2();

        private Vector2 _AccelerationResult = new Vector2();

        private int _Life;

        private int _Count;

        public AnimationVectorRunner(float x, float y, float width, float height, string name, int type, float scaleX, float scaleY, Vector2? direction, Vector2? acceleration, int life, bool replay = false, bool iskill = false, bool isSmooth = false, int speed = 1, int thin = 1)
            : base(x, y, width, height, name, type, scaleX, scaleY, true, 2)
        {
            if (direction != null)
            {
                _Direction = direction.Value;
            }
            if (acceleration != null)
            {
                _Acceleration = acceleration.Value;
            }
            _AccelerationResult = _Acceleration;
            _Life = life;
            _IsPlay = false;
        }

        public override void Reset()
        {
            _DirectionResult = DefaultPosition;
            _AccelerationResult = _Acceleration;
            _Count = 0;
            IsEnabled = false;
            Stop();
            base.Reset();
        }

        public override void Move(Vector2 Point)
        {
            base.Move(Point);
            _DirectionResult.X = LocalPosition.X;
            _DirectionResult.Y = LocalPosition.Y;
        }

        public override bool Render()
        {
            if (!_IsPlay)
            {
                return true;
            }
            _AccelerationResult += _Acceleration;
            _DirectionResult += _Direction;
            _DirectionResult += _AccelerationResult;
            LocalPosition = new Vector2(_DirectionResult.X, _DirectionResult.Y);
            base.Render();
            if (LevelMainController.current.slowModeFrames * _Life < _Count)
            {
                Stop(true);
                return true;
            }
            _Count++;
            return false;
        }
    }
}
