using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders
{
    public class CarrierIdReader : BaseDevice, IDevice
    {
        public event Action<ModuleName, string, string> OnCarrierIdRead;
        public event Action<ModuleName, string> OnCarrierIdReadFailed;
        public event Action<ModuleName, string/*Name*/, string/*Code*/> OnCarrierIdWrite;
        public event Action<ModuleName, string/*Name*/> OnCarrierIdWriteFailed;

        private string _loadPort;

        public CarrierIdReader(string module, string name, string display="", string deviceId="", string address="", string page="", string loadport="") :base(module, name, name, name)
        {
            Module = module;
            Name = name;
            _loadPort = loadport;
        }


        public virtual bool Initialize()
        {
            DEVICE.Register(String.Format("{0}.{1}", Name, "ReadRFID"), (out string reason, int time, object[] param) =>
            {
                bool ret = Read(out reason);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Reading ID");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "WriteRFID"), (out string reason, int time, object[] param) =>
            {
                string id = (string)param[0];
                bool ret = Write(id, out reason);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Writing ID");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "ReadCarrierID"), (out string reason, int time, object[] param) =>
            {
                bool ret = Read(out reason);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Reading carrier ID");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "WriteCarrierID"), (out string reason, int time, object[] param) =>
            {
                string id = (string)param[0];
                bool ret = Write(id, out reason);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Writing carrier ID");
                    return true;
                }

                return false;
            });
            return true;
        }

 
        public virtual bool Read(out string reason)
        {
            reason = string.Empty;
            return true;
        }
        public virtual bool Read(int startpage,int length,out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool Write(string id, out string reason)
        {
            reason = string.Empty;

            return true;
        }
        public virtual bool Write(int startpage, int length, out string reason)
        {
            reason = string.Empty;
            return true;
        }
        public virtual bool SetParameter(string paraname,string paravalue,out string reason)
        {
            reason = string.Empty;
            return true;
        }
        public virtual string ReadParameter(string paraname,out string reason)
        {
            reason = string.Empty;
            return "";
        }


        public virtual void Monitor()
        {
 
        }

        public virtual void Terminate()
        {
            
        }

        public virtual void Reset()
        {
 
        }

        public virtual bool Connect()
        {
            return true;
        }

        public bool SetManualScanCode(string code)
        {
            ReadOk(code);

            EV.PostInfoLog("System", "Manually scan code");
            return true;
        }

        public void ReadOk(string code)
        {
            if (OnCarrierIdRead != null)
            {
                OnCarrierIdRead((ModuleName)Enum.Parse(typeof(ModuleName), string.IsNullOrEmpty(Module) ? _loadPort : Module), Name, code);
            }
        }

        public void ReadFailed()
        {
            if (OnCarrierIdReadFailed != null)
            {
                OnCarrierIdReadFailed((ModuleName)Enum.Parse(typeof(ModuleName), string.IsNullOrEmpty(Module) ? _loadPort : Module), Name);
            }
        }

        public void WriteOk(string code)
        {
            if (OnCarrierIdWrite != null)
            {
                OnCarrierIdWrite((ModuleName)Enum.Parse(typeof(ModuleName), string.IsNullOrEmpty(Module) ? _loadPort : Module), Name, code);
            }
        }

        public void WriteFailed()
        {
            if (OnCarrierIdWriteFailed != null)
            {
                OnCarrierIdWriteFailed((ModuleName)Enum.Parse(typeof(ModuleName), string.IsNullOrEmpty(Module) ? _loadPort : Module), Name);
            }
        }
    }
 
}
