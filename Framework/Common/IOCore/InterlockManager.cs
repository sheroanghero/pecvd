using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;

namespace Aitex.Core.RT.IOCore
{
    public class InterlockManager : Singleton<InterlockManager>
    {
        private List<InterlockAction> _actions = new List<InterlockAction>();
        private Dictionary<InterlockLimit, List<InterlockAction>> _dicLimit = new Dictionary<InterlockLimit, List<InterlockAction>>();
 
        public bool Initialize(string interlockFile, string module, Dictionary<string, DOAccessor> doMap, Dictionary<string, DIAccessor> diMap, out string reason)
        {
            reason = string.Empty;

            try
            {
                XmlDocument xmlConfig = new XmlDocument();
                xmlConfig.Load(interlockFile);

  //<Action do="DO_RF_power_on_off" value="true" tip="" tip.zh-CN="" tip.en-US="">
                //  <Limit di="DI_RF_hardware_interlock"  value="true" tip="" tip.zh-CN="" tip.en-US="" />
  //  <Limit di="DI_Chamber_door_sw"  value="true" tip="" tip.zh-CN="" tip.en-US="" />
  //</Action>


                XmlNode nodeInterlock = xmlConfig.SelectSingleNode("Interlock");

                bool ShutterIsInstalled = false;
                if (module.Contains("PM"))
                {
                    string PMType = SC.GetStringValue($"System.SetUp.{module}.ChamberType");
                    if (PMType == "PVD")
                    {
                        ShutterIsInstalled = SC.GetValue<bool>($"System.SetUp.{module}.ShutterIsInstalled");
                    }
                }


                bool TurboPumpIsInstalled = SC.GetValue<bool>("System.TurboPump.IsInstalled");



                foreach (XmlNode item in nodeInterlock.ChildNodes)
                {
                    if (item.NodeType == XmlNodeType.Comment)
                        continue;

                    XmlElement actionNode = item as XmlElement;
                    if (actionNode == null) continue;
                    if (actionNode.Name != "Action")
                    {
                        if (actionNode.NodeType != XmlNodeType.Comment)
                        {
                            LOG.Write("interlock config file contains no comments content, " + actionNode.InnerXml);
                        }
                        continue;
                    }
                    if (!actionNode.HasAttribute("do") || !actionNode.HasAttribute("value"))
                    {
                        reason += "action node has no [do] or [value] attribute \r\n";
                        continue;
                    }

                    string doName = module +"."+ actionNode.GetAttribute("do");
                    bool value = Convert.ToBoolean(actionNode.GetAttribute("value"));
                    string tip = string.Empty;
                    Dictionary<string, string> cultureTip  = new Dictionary<string, string>();
                    List<InterlockLimit> limits = new List<InterlockLimit>();

                    if (!doMap.ContainsKey(doName))
                    {
                        reason += "action node " + doName + " no such DO defined \r\n";
                        continue;
                    }

                    DOAccessor doItem = doMap[doName] as DOAccessor;
                    if (doItem == null)
                    {
                        reason += "action node " + doName + " no such DO defined \r\n";
                        continue;
                    }

                    if (actionNode.HasAttribute("tip"))
                        tip = actionNode.GetAttribute("tip");

                    if (actionNode.HasAttribute("tip.zh-CN"))
                        cultureTip["zh-CN"] = actionNode.GetAttribute("tip.zh-CN");

                    if (actionNode.HasAttribute("tip.en-US"))
                        cultureTip["en-US"] = actionNode.GetAttribute("tip.en-US");

                    foreach (XmlElement limitNode in actionNode.ChildNodes)
                    {
                        if (limitNode.Name != "Limit")
                        {
                            if (limitNode.NodeType != XmlNodeType.Comment)
                            {
                                LOG.Write("interlock config file contains no comments content, " + limitNode.InnerXml);
                            }
                            continue;
                        }
                        if (!(limitNode.HasAttribute("di") || limitNode.HasAttribute("do")) || !limitNode.HasAttribute("value"))
                        {
                            reason += "limit node lack of di/do or value attribute \r\n";
                            continue;
                        }

                        string stringValue = limitNode.GetAttribute("value");
                        if (stringValue.Contains("*"))
                        {
                            continue;                        
                        }
                        bool limitValue = Convert.ToBoolean(limitNode.GetAttribute("value"));
                        string limitTip = string.Empty;
                        Dictionary<string, string> limitCultureTip = new Dictionary<string, string>();

                        if (limitNode.HasAttribute("tip"))
                            limitTip = limitNode.GetAttribute("tip");

                        if (limitNode.HasAttribute("tip.zh-CN"))
                            limitCultureTip["zh-CN"] = limitNode.GetAttribute("tip.zh-CN");

                        if (limitNode.HasAttribute("tip.en-US"))
                            limitCultureTip["en-US"] = limitNode.GetAttribute("tip.en-US");

                        if (limitNode.HasAttribute("di"))
                        {
                            string diName = module + "." + limitNode.GetAttribute("di");

                            if (!diMap.ContainsKey(diName))
                            {
                                reason += "limit node " + diName + " no such DI defined \r\n";
                                continue;
                            }

                            if (diName.Contains("Shutter") && !ShutterIsInstalled)
                            {
                                continue;
                            }

                            if ((diName.Contains("IsolationValveOpenVCEA") || diName.Contains("IsolationValveCloseVCEA") ||  diName.Contains("IsolationValveOpenVCEB") || diName.Contains("IsolationValveCloseVCEB")  ) && !TurboPumpIsInstalled)
                            {
                                continue;
                            }

                       

                            DIAccessor diItem = diMap[diName] as DIAccessor;
                            if (diItem == null)
                            {
                                reason += "limit node " + diName + " no such DI defined \r\n";
                                continue;
                            }

                            limits.Add(new DiLimit(diItem, limitValue, limitTip, limitCultureTip));
                        }
                        else if (limitNode.HasAttribute("do"))
                        {
                            string ioName = module + "." + limitNode.GetAttribute("do");

                            if (!doMap.ContainsKey(ioName))
                            {
                                reason += "limit node " + ioName + " no such DO defined \r\n";
                                continue;
                            }

                            if (ioName.Contains("Shutter") && !ShutterIsInstalled)
                            {
                                continue;
                            }

                            if ((ioName.Contains("IsolationValveOpenVCEA") || ioName.Contains("IsolationValveCloseVCEA") || ioName.Contains("IsolationValveOpenVCEB") || ioName.Contains("IsolationValveCloseVCEB")) && !TurboPumpIsInstalled)
                            {
                                continue;
                            }

                            DOAccessor ioItem = doMap[ioName];
                            if (ioItem == null)
                            {
                                reason += "limit node " + ioName + " no such DO defined \r\n";
                                continue;
                            }

                            limits.Add(new DoLimit(ioItem, limitValue, limitTip, limitCultureTip));
                        }

                    }
                    
                    InterlockAction action = new InterlockAction(doItem, value, tip, cultureTip, limits);
                    _actions.Add(action);

                    foreach (var interlockLimit in limits)
                    {
                        bool isExist = false;
                        foreach (var limit in _dicLimit.Keys)
                        {
                            if (interlockLimit.IsSame(limit))
                            {
                                _dicLimit[limit].Add(action);
                                isExist = true;
                                break;
                            }
                        }

                        if (!isExist)
                        {
                            _dicLimit[interlockLimit] = new List<InterlockAction>();
                            _dicLimit[interlockLimit].Add(action);
                        }
                        
                    }
                }

            }
            catch (Exception ex)
            {
                reason += ex.Message;
            }

            if (!string.IsNullOrEmpty(reason))
                return false;

            return true;
        }

        public void Monitor()
        {
            foreach (var limit in _dicLimit)
            {
                if (SC.ContainsItem("System.BypassInterlock") && SC.GetValue<bool>("System.BypassInterlock"))
                    continue;

                if (!limit.Key.IsTriggered())
                    continue;

                string info = string.Empty;
                string module = "System";
                foreach (var action in limit.Value)
                {
                    string reason;
                    if (!action.Reverse(out reason))
                        continue;

                    if (string.IsNullOrEmpty(info))
                    {    
                        info = string.Format("Due to the {0}, {1}=[{2}]\n", limit.Key.Tip, limit.Key.Name,
                            !limit.Key.LimitValue);
                    }
                    var nameArray = action.ActionName.Split('.');
                    if (nameArray != null && nameArray.Length > 1 && ModuleHelper.IsPm(nameArray[0]))
                        module = nameArray[0];

                    info += reason + "\n";
                }

                if (!string.IsNullOrEmpty(info))
                {
                    EV.PostAlarmLog(module, info.TrimEnd('\n'));
                } 
            }
        }

        public bool CanSetDo(string doName, bool onOff, out string reason)
        {
            reason = string.Empty;
            if (SC.ContainsItem("System.BypassInterlock") && SC.GetValue<bool>("System.BypassInterlock"))
                return true;

            foreach (var interlockAction in _actions)
            {
                if (interlockAction.IsSame(doName, onOff))
                {
                    return interlockAction.CanDo(out reason);
                }
            }

            return true;
        }
    }
}
