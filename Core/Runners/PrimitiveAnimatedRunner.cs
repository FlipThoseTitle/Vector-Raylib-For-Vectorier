using Raylib_cs;
using System.Numerics;
using Vector.Core.Animation;
using Vector.Core.Controllers;
using Vector.Core.Models;

namespace Vector.Core.Runners
{
    public class PrimitiveAnimatedRunner : PrimitiveRunner
    {
        private ControllerAnimations _ControllerAnimation;

        private AnimationReaction _reaction;

        public ControllerAnimations ControllerAnimation => _ControllerAnimation;

        public override bool IsStrike
        {
            get => false;
            set
            {
            }
        }

        public PrimitiveAnimatedRunner(int type, string p_name, Color p_color, Vector3 p_deltaPosition, float p_impulse, List<string> p_sounds)
            : base(type, p_name, p_color, p_deltaPosition, p_impulse, p_sounds)
        {
            Type = ModelType.PrimitiveAnimated;
        }

        public override void Load()
        {
            base.Load();
            _ControllerAnimation = new ControllerAnimations(Model);
        }

        public override void Render(List<QuadRunner> p_platforms)
        {
            base.Render(p_platforms);
            _ControllerAnimation.Render();
            if (_reaction != null)
            {
                Play();
            }
        }

        public void Play()
        {
            _ControllerAnimation.Play(Animations.Animation[_reaction.Name], false, _reaction.FirstFrame);
            _reaction = null;
        }

        public void PlayReaction(string[] reactions)
        {
            _reaction = AnimationLoader.ParseReaction(reactions);
        }

        public override void Reset()
        {
            _ControllerAnimation.Clear();
            base.Reset();
        }
    }
}
