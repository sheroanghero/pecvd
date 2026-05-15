using System.Collections.Generic;
using Aitex.Core.Util;
using DocumentFormat.OpenXml.Spreadsheet;
 
namespace Aitex.Core.RT.IOCore
{
    /*
     * 
  <Action do="DO_MFC1_valve__" value="true" tip="" tip.zh-CN="" tip.en-US="">
    <Limit di="DI_Chamber_door_sw"  value="true" tip="" tip.zh-CN="" tip.en-US="" />
  </Action>
     * 
     */

    public abstract class InterlockLimit
    {
        public string Name
        {
            get { return _name; }
        }

        public abstract bool CurrentValue { get; }

        public abstract string LimitReason { get; }

        public bool LimitValue
        {
            get { return _limitValue; }
        }

        public string Tip
        {
            get
            {
                return _tip;
            }
        }
    

        private string _name;
        private bool _limitValue;
        private string _tip;
        private Dictionary<string, string> _cultureTip = new Dictionary<string, string>();

        R_TRIG _trigger = new R_TRIG();

        public InterlockLimit(string name, bool value, string tip, Dictionary<string, string> cultureTip)
        {
            _name = name;
            _limitValue = value;
            _tip = tip;
            _cultureTip = cultureTip;
        }

        public bool IsSame(string name, bool value)
        {
            return (name == _name) && (_limitValue == value);
        }

        public bool IsSame(InterlockLimit limit)
        {
            return (limit.Name == _name) && (_limitValue == limit.LimitValue);
        }

        public bool IsTriggered()
        {
            _trigger.CLK = CurrentValue != _limitValue;

            return _trigger.Q;
        }

        public bool CanDo(out string reason)
        {
            reason = string.Empty;

            if (CurrentValue == _limitValue)
                return true;

            reason = LimitReason;

            return false;
        }
    }


    internal class DiLimit:InterlockLimit
    {
        private DIAccessor _di;

        public DiLimit(DIAccessor diItem, bool value, string tip, Dictionary<string, string> cultureTip)
        :base(diItem.Name, value, tip, cultureTip)
        {
            _di = diItem;
        }

        public override bool CurrentValue
        {
            get { return _di.Value; }
        }

        public override string LimitReason
        {
            get
            {
                return string.Format("DI-{0}({1}) = [{2}],{3}", _di.IoTableIndex, _di.Name, _di.Value ? "ON" : "OFF", Tip);

            }
        }
    }


    internal class DoLimit : InterlockLimit
    {
        private DOAccessor _do;

        public DoLimit(DOAccessor doItem, bool value, string tip, Dictionary<string, string> cultureTip)
            : base(doItem.Name, value, tip, cultureTip)
        {
            _do = doItem;
        }

        public override bool CurrentValue
        {
            get { return _do.Value; }
        }

        public override string LimitReason
        {
            get
            {
                return string.Format("DO-{0}({1}) = [{2}],{3}", _do.IoTableIndex, _do.Name, _do.Value ? "ON" : "OFF", Tip);

            }
        }
    }

    public class CustomLimitBase : InterlockLimit
    {
        public CustomLimitBase(string name, bool limitValue, string tip, Dictionary<string, string> cultureTip) : base(name, limitValue, tip, cultureTip)
        {
        }

        public override bool CurrentValue { get; }
        public override string LimitReason { get; }
    }
}
