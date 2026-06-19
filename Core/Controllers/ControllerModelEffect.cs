using System.Numerics;
using Vector.Core.Location;
using Vector.Core.Models;
using Vector.Core.Node;
using Vector.Core.Runners;
using Vector.Core.Runners.Animation;

namespace Vector.Core.Controllers
{
    public class ControllerModelEffect
    {
        private ModelHuman _modelHuman;

        private AnimationSprite _antibotEffect;

        private AnimationSprite _taserExplosionEffect;

        private Dictionary<ModelNode, AnimationSprite> _paralyzeEffect = new Dictionary<ModelNode, AnimationSprite>();

        private bool _taserEffectActive;

        private List<(ModelNode, AnimationSprite)> _taserEffect = new List<(ModelNode, AnimationSprite)>();

        public IParent Parent;

        public ControllerModelEffect(ModelHuman modelHuman)
        {
            Parent = Sets.Current.Containers[1];
            _modelHuman = modelHuman;
            if (_modelHuman.IsSelf)
            {
                CreateAntibotEffect();
            }
            else
            {
                CreateTaserEffect();
            }
            CreateTaserExplosionEffect();
        }

        private void CreateAntibotEffect()
        {
            _antibotEffect = new AnimationSprite("antibot")
            {
                Parent = Parent,
                Z = -20 + (Parent as VisualContainer).Z,
                FPS = 100,
                Iterations = 1,
                Pivot = new System.Numerics.Vector2(0.5f, 0.5f)
            };
        }

        public void RunAntibotEffect()
        {
            if (_antibotEffect == null)
            {
                Console.WriteLine("Run antibot but effect null");
                return;
            }
            var node = _modelHuman.GetNode("COM");
            _antibotEffect.LocalPosition = new System.Numerics.Vector2(node.Start.X, node.Start.Y);
            _antibotEffect.Play();
        }

        private void CreateTaserExplosionEffect()
        {
            _taserExplosionEffect = new AnimationSprite("lightning_expl_v2")
            {
                Parent = Parent,
                Z = -20 + (Parent as VisualContainer).Z,
                FPS = 100,
                Iterations = 1,
                Pivot = new System.Numerics.Vector2(0.5f, 0.5f),
                LocalScale = new System.Numerics.Vector2(2, 2)
            };

        }

        public void RunTaserExplosion()
        {
            if (_taserExplosionEffect == null)
            {
                Console.WriteLine("Run TaserExplosion but effect null");
                return;
            }
            var node = _modelHuman.GetNode("COM");

            _taserExplosionEffect.LocalPosition = new System.Numerics.Vector2(node.Start.X, node.Start.Y);
            _taserExplosionEffect.Play();
        }

        private void CreateTaserEffect()
        {

            var taser1 = new AnimationSprite("lightning_hands")
            {
                Parent = Parent,
                Z = -20 + (Parent as VisualContainer).Z,
                Pivot = new Vector2(0.5f, 0.5f)
            };
            var taser2 = new AnimationSprite("lightning_hands")
            {
                Parent = Parent,
                Z = -20 + (Parent as VisualContainer).Z,
                Pivot = new Vector2(0.5f, 0.5f)
            };

            _taserEffect.Add((_modelHuman.GetNode("NKnuckles_1"), taser1));
            _taserEffect.Add((_modelHuman.GetNode("NKnuckles_2"), taser2));
        }

        public void RunTaserEffect()
        {
            _taserEffectActive = true;
            foreach (var item in _taserEffect)
            {
                item.Item2.Play();
            }
        }

        public void StopTaserEffect()
        {
            _taserEffectActive = false;
            foreach (var item in _taserEffect)
            {
                item.Item2.Finish();
            }
        }

        private void RenderTasetEffect()
        {
            if (!_taserEffectActive)
            {
                return;
            }
            foreach (var item in _taserEffect)
            {
                item.Item2.LocalPosition = new Vector2((float)item.Item1.Start.X, (float)item.Item1.Start.Y);
            }
        }

        public void RunParalyzeEffect()
        {
            string[] nodes = {
               "NNeck",
               "NPivot",
                "NKnee_1",
                "NKnee_2",
                "NElbow_1",
                "NElbow_2"
            };
            _paralyzeEffect.Clear();
            foreach (var nodeName in nodes)
            {
                var node = _modelHuman.GetNode(nodeName);
                var animation = new AnimationSprite("lightning_paraliz_v2")
                {
                    Parent = Parent,
                    Z = -20 + (Parent as VisualContainer).Z,
                    Pivot = new Vector2(0.5f, 0.5f)
                };

                animation.Play();

                _paralyzeEffect.Add(node, animation);
            }
        }

        public void ParalyzeRender()
        {
            foreach (var node in _paralyzeEffect.Keys)
            {
                _paralyzeEffect[node].LocalPosition = new Vector2((float)node.Start.X, (float)node.Start.Y);
            }
        }

        public void StopParalyzeEffect()
        {
            foreach (var effect in _paralyzeEffect.Values)
            {
                effect.Finish();
            }
            _paralyzeEffect.Clear();
        }

        public void Render()
        {
            ParalyzeRender();
            RenderTasetEffect();
        }

        public void Reset()
        {
            if (_antibotEffect != null)
                _antibotEffect.Finish();
            _taserExplosionEffect.Finish();
            StopParalyzeEffect();
            StopTaserEffect();
        }
    }
}
