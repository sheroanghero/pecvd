using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.UI.Client.ClientControls.Common
{
    public class RobotPosition
    {
        public int X;
        public int Z;
        public int Root;
        public int Arm;
        public int Hand;
    }

    public class StationPosition
    {
        public RobotPosition StartPosition;
        public RobotPosition EndPosition;
    }
}
