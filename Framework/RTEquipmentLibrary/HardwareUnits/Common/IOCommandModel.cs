using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common
{

    public class IOCommandModel
    {
        /// <summary>
        /// 
        /// </summary>
        public string CommandStr { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsBasicFunction { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string IOCommandType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool IsSelected { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool SpecialHandler { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string FunctionName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string FunctionDiscription { get; set; }


        public string CommandName { get; set; }
        public string CommandParameter { get; set; }

    }

    public class CommandModelConfig
    {
        public List<IOCommandModel> IOCommandList { get; set; }
    }

}
