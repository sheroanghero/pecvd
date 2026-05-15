using System;
using System.Threading;
using MECF.Framework.Simulator.Core.Driver;
using MECF.Framework.Simulator.Core.Robots;

namespace MECF.Framework.Simulator.Core.Aligners
{

    public class YaskawaAlignerSimulator : YaskawaSR100ControllerSimulator
    {
        public YaskawaAlignerSimulator()
            :base(10111)
        {
 
        }
    }

    public class YaskawaNXAlignerSimulator : YaskawaNX100ControllerSimulator
    {
        public YaskawaNXAlignerSimulator()
            : base(10111)
        {

        }
    }
}

