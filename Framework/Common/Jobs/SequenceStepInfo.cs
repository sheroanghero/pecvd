using System;
using System.Collections.Generic;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Jobs
{
    [Serializable]
    public class SequenceStepInfo
    {
        public List<ModuleName> StepModules { get; set; }

        public string RecipeName { get; set; }

        public double AlignAngle { get; set; }
        public int CleanInterval { get; set; }

        public Dictionary<string, object> StepParameter { get; set; }

        public SequenceStepInfo()
        {
            StepModules = new List<ModuleName>();
            StepParameter = new Dictionary<string, object>();
        }
    }



}
