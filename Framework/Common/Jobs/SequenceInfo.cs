using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.RecipeCenter;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Jobs
{
    [Serializable]
    [DataContract]
    public class SequenceInfo
    {
        [DataMember]
        public List<SequenceStepInfo> Steps { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public Guid InnerId { get; set; }

        public SequenceInfo(string name)
        {
            Name = name;
            InnerId = Guid.NewGuid();
            Steps = new List<SequenceStepInfo>();
        }
    }

    public class SequenceInfoHelper
    {
        public static SequenceInfo GetInfo(string seqFile)
        {
            SequenceInfo info = new SequenceInfo(seqFile);

            string content = RecipeFileManager.Instance.GetSequence(seqFile, false);
            if (!string.IsNullOrEmpty(content))
            {
                try
                {
                    XmlDocument dom = new XmlDocument();
                    dom.LoadXml(content);

                    XmlNodeList lstStepNode = dom.SelectNodes("Aitex/TableSequenceData/Step");
                    if (lstStepNode == null)
                    {
                        LOG.Error($"{seqFile} has no step");
                        return null;
                    }

                    foreach (var nodeModelChild in lstStepNode)
                    {
                        XmlElement nodeStep = nodeModelChild as XmlElement;
                        SequenceStepInfo stepInfo = new SequenceStepInfo();

                        foreach (XmlAttribute attr in nodeStep.Attributes)
                        {
                            if (attr.Name == "Position" && attr.Value == "LL")
                            {
                                if (nodeStep.Attributes["LLSelection"].Value.Contains("LLA"))
                                {
                                    stepInfo.StepModules.Add(ModuleName.LLA);
                                    stepInfo.StepParameter["SlotSelection"] = "1";
                                }
                                if (nodeStep.Attributes["LLSelection"].Value.Contains("LLB"))
                                {
                                    stepInfo.StepModules.Add(ModuleName.LLB);
                                    stepInfo.StepParameter["SlotSelection"] = "1";
                                }

                                continue;
                            }
                            else if (attr.Name == "Position" && attr.Value == "CL")
                            {
                                if (nodeStep.Attributes["CLSelection"].Value.Contains("CLA"))
                                {
                                    stepInfo.StepModules.Add(ModuleName.LLA);
                                    stepInfo.StepParameter["SlotSelection"] = "0";
                                }
                                if (nodeStep.Attributes["CLSelection"].Value.Contains("CLB"))
                                {
                                    stepInfo.StepModules.Add(ModuleName.LLB);
                                    stepInfo.StepParameter["SlotSelection"] = "0";
                                }

                                continue;
                            }
                            if (attr.Name == "Position" || attr.Name == "LLSelection" || attr.Name == "CleanSelection"
                                || attr.Name == "PVDSelection" || attr.Name == "DegasSelection")
                            {
                                if (!Enum.TryParse(attr.Value, out ModuleName _))
                                    continue;

                                string[] pos = attr.Value.Split(',');
                                if (pos.Length < 1)
                                {
                                    LOG.Error($"{seqFile} Position {attr.Value} can not be empty");
                                    return null;
                                }

                                foreach (var po in pos)
                                {
                                    ModuleName module = ModuleHelper.Converter(po);
                                    if (module == ModuleName.System)
                                    {
                                        LOG.Error($"{seqFile} Position {po} not valid");
                                        return null;
                                    }

                                    stepInfo.StepModules.Add(module);
                                }

                                continue;
                            }

                            if (attr.Name == "AlignerAngle")
                            {
                                if (!double.TryParse(attr.Value, out double angle))
                                {
                                    LOG.Error($"{seqFile} AlignAngle {attr.Value} not valid");
                                    return null;
                                }

                                stepInfo.AlignAngle = angle;
                                continue;
                            }

                            if (attr.Name == "CleanIntervalNoWafer")
                            {
                                if (!int.TryParse(attr.Value, out int cleanInternal))
                                {
                                    LOG.Error($"{seqFile} Clean Interval {attr.Value} not valid");
                                    return null;
                                }

                                stepInfo.CleanInterval = cleanInternal;
                                continue;
                            }

                            if ((attr.Name == "Recipe") || (attr.Name == "ProcessRecipe"))
                            {
                                stepInfo.RecipeName = attr.Value;
                                continue;
                            }

                            stepInfo.StepParameter[attr.Name] = attr.Value == null ? "" : attr.Value;
                        }

                        info.Steps.Add(stepInfo);
                    }

                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                    return null;
                }
            }

            return info;
        }
    }
}
