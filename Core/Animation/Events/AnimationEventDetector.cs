using Vector.Core.Detector;

namespace Vector.Core.Animation.Events
{
    public class AnimationEventDetector : AnimationEvent
    {
        public DetectorEvent.DetectorEventType Type
        {
            get;
        }

        public AnimationEventDetector(DetectorEvent.DetectorEventType type, AnimationEventParam param)
            : base(param)
        {
            Type = type;
        }
    }
}
