using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Simulator
{
    public class DiForce : Singleton<DiForce>
    {
        public Dictionary<string, bool> ForceValues { get; set; }

        public DiForce()
        {
            ForceValues = new Dictionary<string, bool>();
        }

        public void Set(string di, bool value)
        {
            ForceValues[di] = value;
        }

        public void Unset(string di)
        {
            if (ForceValues.ContainsKey(di))
                ForceValues.Remove(di);
        }
    }
}
