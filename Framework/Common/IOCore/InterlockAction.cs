using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.IOCore
{
    /*
     * 
  <Action do="DO_MFC1_valve__" value="true" tip="" tip.zh-CN="" tip.en-US="">
    <Limit di="DI_Chamber_door_sw"  value="true" tip="" tip.zh-CN="" tip.en-US="" />
  </Action>
     * 
     */
    class InterlockAction
    {
        private List<InterlockLimit> _limits = new List<InterlockLimit>();

        private DOAccessor _do;
        private bool _actionValue;
        private string _tip;
        private Dictionary<string, string> _cultureTip = new Dictionary<string, string>();
        public string ActionName => _do != null ? _do.Name : "";

        public InterlockAction(DOAccessor doItem, bool value, string tip, Dictionary<string, string> cultureTip, List<InterlockLimit> limits)
        {
            _do = doItem;
            _actionValue = value;
            _tip = tip;
            _cultureTip = cultureTip;
            _limits = limits;
        }

        public bool IsSame(string doName, bool value)
        {
            return (doName == _do.Name) && (_actionValue == value);
        }

        public bool Reverse(out string reason)
        {
            reason = string.Empty;
            if (_do.Value != _actionValue)
                return false;

            if (_do.SetValue(!_actionValue, out reason))
            {
                reason = string.Format("Force setting DO-{0}({1}) = [{2}]", _do.IoTableIndex, _do.Name, (!_actionValue) ? "ON" : "OFF");
            }

            return true;
        }

        public bool CanDo(out string reason)
        {
            reason = string.Empty;
            bool result = true;

            foreach (var interlockLimit in _limits)
            {
                string info;
                if (!interlockLimit.CanDo(out info))
                {
                    if (reason.Length > 0)
                        reason += "\n";
                    else
                    {
                        reason = string.Format("Interlock triggered, DO-{0}({1}) can not be [{2}], {3} \n", _do.IoTableIndex, _do.Name, _actionValue ? "ON" : "OFF",
                            _tip);
                    }
                    reason += info;
                    result = false;
                }
            }
            return result;
        }
    }
}
