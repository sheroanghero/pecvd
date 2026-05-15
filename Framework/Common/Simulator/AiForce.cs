using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Simulator
{
    public class AiForce : Singleton<AiForce>
    {
        public Dictionary<string, float> ForceValues { get; set; }

        public AiForce()
        {
            ForceValues = new Dictionary<string, float>();
        }

        public void Set(string ai, float value)
        {
            ForceValues[ai] = value;
        }

        public void Unset(string ai)
        {
            if (ForceValues.ContainsKey(ai))
                ForceValues.Remove(ai);
        }
    }
}
