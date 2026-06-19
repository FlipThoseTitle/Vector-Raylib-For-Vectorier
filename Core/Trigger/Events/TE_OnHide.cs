using Vector.Core.Runners;

namespace Vector.Core.Trigger.Events
{
    public class TE_OnHide : TriggerEvent
    {
        public TE_OnHide()
        {
            _Type = EventType.TET_ON_HIDE;
        }

        public TE_OnHide(TriggerRunner p_parent)
        {
            _Type = EventType.TET_ON_HIDE;
        }
    }
}
