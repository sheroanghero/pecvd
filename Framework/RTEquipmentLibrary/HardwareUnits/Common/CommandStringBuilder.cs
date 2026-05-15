using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common
{

    public class CommandStringBuilderFactory
    {
        public static CommandStringBuilder GetCommandStringBuilder(string deviceName)
        {
            if (deviceName.Contains("PfeifferHipace"))
            {
                return new PfeifferHipaceCommandStringBuilder();
            }

            return new CommandStringBuilder();
        }
    }

    public class CommandStringBuilder
    {
        public virtual void BuildCommandString()
        {

        }
    }

    public class PfeifferHipaceCommandStringBuilder : CommandStringBuilder
    {
        public override void BuildCommandString()
        {

        }
    }


}
