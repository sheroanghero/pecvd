using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.OperationCenter;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Buffer
{
    public class BufferStationBaseDevice : BaseDevice, IDevice
    {
        public virtual BufferStateEnum CurrentState
        {
            get; set;
        } = BufferStateEnum.Idle;
        public virtual bool IsMapped { get; }

        public virtual int BufferSlotsNumber { get; set; }

        public virtual ModuleName BufferModuleName { get; set; }
        public string BufferSlotMap { get; set; }

        public BufferStationBaseDevice(string module, ModuleName name, int slotNumber)
        {
            Module = module;
            Name = name.ToString();
            BufferModuleName = name;
            BufferSlotsNumber = slotNumber;
            WaferManager.Instance.SubscribeLocation(BufferModuleName, slotNumber);

        }

        public virtual void SubscribeOperationAndData()
        {
            OP.Subscribe(String.Format("{0}.{1}", Name, "SetWaferSize"), (out string reason, int time, object[] param) =>
            {
                if (param.Length < 2)
                {
                    reason = "Invalide parameter";
                    return false;
                }
                WaferSize size = (WaferSize)Enum.Parse(typeof(WaferSize), param[1].ToString());

                bool ret = ExecuteSetWaferSize(size, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "SetWaferSize");
                    return true;
                }
                reason = "";
                return false;
            });

            DATA.Subscribe($"{Name}.WaferSize", () => CurrentWaferSize.ToString());


            DATA.Subscribe($"{Name}.State", () => CurrentState.ToString());


        }

        public virtual bool ExecuteSetWaferSize(WaferSize size, out string reason)
        {
            if(WaferManager.Instance.CheckHasWafer(BufferModuleName,0))
            {
                WaferManager.Instance.UpdateWaferSize(BufferModuleName, 0, size);
            }
            reason = "";
            return true;
        }

        public BufferStationBaseDevice(string module, string name, int slotNumber)
        {
            Module = module;
            Name = name;
            BufferModuleName = (ModuleName)Enum.Parse(typeof(ModuleName), name);
            BufferSlotsNumber = slotNumber;
            WaferManager.Instance.SubscribeLocation(BufferModuleName, slotNumber);

        }
        public bool Initialize()
        {
            SubscribeOperationAndData();
            return true;
        }

        public void Monitor()
        {


        }

        public void Terminate()
        {

        }


        public virtual void Reset()
        {

        }

        public virtual bool IsEnableTransferWafer(out string reason)
        {

            reason = "";
            return true;
        }

        public virtual bool IsEnableMapWafer(out string reason)
        {

            reason = "";
            return true;
        }

        public virtual void ConfirmWaferPresent()
        {

        }
        public virtual void OnSlotMapRead(string slotMap, int index)
        {
        }

        public virtual void OnSlotMapRead(string slotMap)
        {
        }

        public virtual bool IsWaferPresent { get; set; }

        public virtual void InformAccessStart()
        {
        }
        public virtual void InformAccessComplete()
        {
        }

        public virtual WaferSize CurrentWaferSize
        {
            get
            {
                if (WaferManager.Instance.CheckHasWafer(BufferModuleName, 0))
                {
                    return WaferManager.Instance.GetWaferSize(BufferModuleName, 0);
                }
                return WaferSize.WS0;
            }
        }

    }


    public enum BufferStateEnum
    {
        Idle,
    }
}
