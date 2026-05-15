using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.FAServices
{

    [Serializable]
    [XmlRoot("DataItems")]
    public class VIDItem
    {
        private string name;
        [XmlAttribute("Name")]
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                AnalyseName(value);
            }
        }

        [XmlAttribute("Index")]
        public int Index { get; set; }

        [XmlAttribute("DataType")]
        public string DataType { get; set; }

        [XmlArray("LinkableVID")]
        public int[] LinkableVid { get; set; }

        [XmlAttribute("Description")]
        public string Description { get; set; }

        [XmlAttribute("Module")]
        public string Module { get; set; } = "";

        [XmlAttribute("Type")]
        public string Type { get; set; } = "";

        [XmlAttribute("Unit")]
        public string Unit { get; set; } = "";

        [XmlAttribute("Parameter")]
        public string Parameter { get; set; }

        private int moduleIndex;
        [XmlIgnore]
        public int ModuleIndex
        {
            get { return moduleIndex; }
            set
            {
                moduleIndex = value;
            }
        }

        private int typeIndex;
        [XmlIgnore]
        public int TypeIndex
        {
            get { return typeIndex; }
            set
            {
                typeIndex = value;
            }
        }

        private int unitIndex;
        [XmlIgnore]
        public int UnitIndex
        {
            get { return unitIndex; }
            set
            {
                unitIndex = value;
            }
        }

        private int parameterIndex;
        [XmlIgnore]
        public int ParameterIndex
        {
            get { return parameterIndex; }
            set
            {
                parameterIndex = value;
            }
        }

        void AnalyseName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            string[] names = name.Split('.');

            Module = ModuleName.System.ToString();

            if (names.Length >= 1)
            {
                Parameter = names[names.Length - 1];
                if (names.Length >= 2)
                {
                    Module = names[0];
                    ModuleIndex = (Enum.TryParse(Module, out ModuleName moduleIndex) ? (int)moduleIndex : 0) + 1;
                    if (names.Length >= 3)
                    {
                        Unit = names[names.Length - 2];

                        if (names.Length >= 4)
                        {
                            for (int j = 1; j < names.Length - 2; j++)
                            {
                                Type += names[j];
                                if (j != names.Length - 3)
                                    Type += ".";
                            }
                        }
                    }

                }
            }
        }
    }
}
