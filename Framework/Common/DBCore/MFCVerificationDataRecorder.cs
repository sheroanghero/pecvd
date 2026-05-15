using Aitex.Core.RT.DBCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.DBCore
{
    public class MFCVerificationDataRecorder
    {
        public static void Add(MFCVerificationData data)
        {
            string sql = string.Format(
                "INSERT INTO \"mfc_verification_data\"(\"module\" , \"name\" ,\"operate_time\", \"percent10_setpoint\" , \"percent10_calculate\", \"percent20_setpoint\", \"percent20_calculate\", " +
                                                                       "\"percent30_setpoint\" , \"percent30_calculate\", \"percent40_setpoint\", \"percent40_calculate\", " +
                                                                       "\"percent50_setpoint\" , \"percent50_calculate\", \"percent60_setpoint\", \"percent60_calculate\", " +
                                                                       "\"percent70_setpoint\" , \"percent70_calculate\", \"percent80_setpoint\", \"percent80_calculate\", " +
                                                                       "\"percent90_setpoint\" , \"percent90_calculate\", \"percent100_setpoint\", \"percent100_calculate\", \"setpoint\", \"calculate\"" +
                                                                       ")VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}' " +
                                                                       ", '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}'" +
                                                                       ", '{16}', '{17}', '{18}', '{19}', '{20}', '{21}', '{22}', '{23}', '{24}');",
                data.Module,
                data.Name,
                DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                data.Percent10Setpoint,
                data.Percent10Calculate,
                data.Percent20Setpoint,
                data.Percent20Calculate,
                data.Percent30Setpoint,
                data.Percent30Calculate,
                data.Percent40Setpoint,
                data.Percent40Calculate,
                data.Percent50Setpoint,
                data.Percent50Calculate,
                data.Percent60Setpoint,
                data.Percent60Calculate,
                data.Percent70Setpoint,
                data.Percent70Calculate,
                data.Percent80Setpoint,
                data.Percent80Calculate,
                data.Percent90Setpoint,
                data.Percent90Calculate,
                data.Percent100Setpoint,
                data.Percent100Calculate,
                data.Setpoint,
                data.Calculate);

            DB.Insert(sql);
        }
    }

    public class MFCVerificationData
    {
        public string Module { get; set; }
        public string Name { get; set; }
        public float Percent10Setpoint { get; set; }
        public float Percent10Calculate { get; set; }
        public float Percent20Setpoint { get; set; }
        public float Percent20Calculate { get; set; }
        public float Percent30Setpoint { get; set; }
        public float Percent30Calculate { get; set; }
        public float Percent40Setpoint { get; set; }
        public float Percent40Calculate { get; set; }
        public float Percent50Setpoint { get; set; }
        public float Percent50Calculate { get; set; }
        public float Percent60Setpoint { get; set; }
        public float Percent60Calculate { get; set; }
        public float Percent70Setpoint { get; set; }
        public float Percent70Calculate { get; set; }
        public float Percent80Setpoint { get; set; }
        public float Percent80Calculate { get; set; }
        public float Percent90Setpoint { get; set; }
        public float Percent90Calculate { get; set; }
        public float Percent100Setpoint { get; set; }
        public float Percent100Calculate { get; set; }
        public float Setpoint { get; set; }
        public float Calculate { get; set; }
    }
}
