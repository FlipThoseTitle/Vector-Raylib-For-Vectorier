using System.Xml;
using Vector.Core.Runners;

namespace Vector.Core.Trigger.Events
{
    public class TE_ChangeVar : TriggerEvent
    {
        private bool _IsInit;

        private Variable _Var;

        private int _OldValue;

        /// <param name="p_trigger">
        /// The trigger owning the loop. A watched value such as ?getModel[_$Model].animationName reads the trigger's
        /// variables ($Model, $Node...), so it needs the trigger (without it TF_Model crashed in InitFuncVar).
        /// </param>
        public TE_ChangeVar(XmlNode p_node, TriggerRunner p_trigger)
        {
            _Type = EventType.TET_VAR_CHANGE;
            _Var = Variable.createVariable(p_node.Attributes["Value"]?.Value ?? string.Empty, string.Empty, p_trigger);
        }

        private void Init()
        {
            _IsInit = true;
            _OldValue = _Var.ValueInt;
        }

        public void Reset()
        {
            _IsInit = false;
        }

        public bool IsChange()
        {
            if (!_IsInit)
            {
                Init();
                return false;
            }
            if (_Parent.ParentTrigger.IsActive)
            {
                return _OldValue != _Var.ValueInt;
            }
            return false;
        }
    }
}
