using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.PMs
{
    public class PM : BaseDevice, IDevice, IPM
    {
        //如果true，robot采用extend, retract指令
        //如果false，robot采用pick，place指令
        public  bool IsActiveHandoff { get; set; }

        //如果true，robot调用收尾PostTransfer
        public bool IsNeedPostTransfer { get; set; }

        public virtual double ChamberPressure { get; }
        public virtual bool IsError { get; }
        public virtual bool IsIdle { get; }
        public virtual bool IsProcessing { get; }

        public virtual bool IsInstalled { get; set; }
        public virtual double ForelinePressure { get; set; } 

        protected readonly string _statNameWaferProcessed;
        protected readonly string _statNameWaferProcessedSincePreviousClean;

        public PM(string module) : base(module, module, module, module)
        {
            _statNameWaferProcessed = $"{module}.WaferProcessed";
            _statNameWaferProcessedSincePreviousClean = $"{module}.WaferProcessedSincePreviousClean";
        }


        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Name}.IsAtm", () => { return CheckAtm(); });
            DATA.Subscribe($"{Name}.IsVacuum", () => { return CheckVacuum(); });
            DATA.Subscribe($"{Name}.ChamberPressure", () => (float)ChamberPressure);

            IsInstalled = !SC.ContainsItem($"System.{Name}IsInstalled") || SC.GetValue<bool>($"System.{Name}IsInstalled");

            StatsDataManager.Instance.Subscribe(_statNameWaferProcessed, $"{Name} Wafer processed", 0);
            StatsDataManager.Instance.Subscribe(_statNameWaferProcessedSincePreviousClean, $"{Name} Wafer processed since previous clean", 0);

            return true;
        }

        public virtual bool CheckAtm()
        {
            return false;
        }

        public virtual bool CheckVacuum()
        {
            return false;
        }

        public virtual bool CheckIsPumping()
        {
            return false;
        }

        public virtual bool CheckChuckState()
        {
            return false;
        }

        public virtual bool CheckServoTransferPos()
        {
            return false;
        }

        public virtual bool CheckChuckPos()
        {
            return false;
        }

        public virtual bool CheckHandoff(EnumTransferType type)
        {
            return false;
        }

        public virtual bool CheckPostTransfer(EnumTransferType type)
        {
            return false;
        }

        public virtual bool CheckEnableTransfer(EnumTransferType type, out string reason)
        {
            reason = string.Empty;
            return false;
        }

        public virtual bool CheckEnableTransfer(EnumTransferType type, int slot, out string reason)
        {
            reason = string.Empty;
            return false;
        }

        public virtual bool CheckEnablePump(out  string reason)
        {
            reason = "Undefined";
            return false;
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

        public virtual bool SetPositionNumber(float value)
        {
            return true;
        }

        public virtual bool CheckPositionNumber(float value)
        {
            return true;
        }

        public virtual bool PrepareTransfer(EnumTransferType type, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool Home()
        {
            return true;
        }

        public virtual void PostTransfer(EnumTransferType type)
        {

        }

        public virtual void TransferHandoff(EnumTransferType type)
        {

        }
 
        public virtual void StartProcess(string recipeName, string recipeContent, bool isClean)
        {

        }

        public virtual bool SetPumpOn(out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual void OnProcessStart(string guid, string recipeName, bool isClean)
        {
            //ProcessDataRecorder.Start(guid, recipeName, 
            //    WaferManager.Instance.GetWafer(ModuleHelper.Converter(Module), 0).InnerId.ToString(), Module);

            if (!isClean)
            {
                int value1 = StatsDataManager.Instance.Increase(_statNameWaferProcessed);
                int value2 = StatsDataManager.Instance.Increase(_statNameWaferProcessedSincePreviousClean);

                LOG.Write($"{_statNameWaferProcessed} counter increase 1 to {value1}");
                LOG.Write($"{_statNameWaferProcessedSincePreviousClean} counter increase 1 to {value2}");
            }

            LOG.Write($"{Module} start run recipe {recipeName}, guid {guid}, clean: {isClean}");
        }

        public virtual bool CheckPumpIsOn()
        {
            throw new NotImplementedException();
        }

        public virtual bool SetPumpOff(out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual void OnProcessEnd(string guid, string recipeName, bool isClean, bool isSucceed)
        {
            if (isClean)
            {
                int value = StatsDataManager.Instance.Reset(_statNameWaferProcessedSincePreviousClean);

                LOG.Write($"{_statNameWaferProcessedSincePreviousClean} counter reset from {value}");
            }

            LOG.Write($"{Module} end run recipe {recipeName}, guid {guid}, clean: {isClean}, result {isSucceed}");

            //ProcessDataRecorder.End(guid, isSucceed ? "Complete" : "Failed");

        }
        public virtual void OnProcessAbort(string guid, string recipeName, bool isClean, bool isSucceed)
        {
            if (isClean)
            {
                int value = StatsDataManager.Instance.Reset(_statNameWaferProcessedSincePreviousClean);

                LOG.Write($"{_statNameWaferProcessedSincePreviousClean} counter reset from {value}");
            }

            LOG.Write($"{Module} end run recipe {recipeName}, guid {guid}, clean: {isClean}, result {isSucceed}");

            //ProcessDataRecorder.End(guid, "Abort" );

        }
        public virtual bool SetThrottleValvePosition(int slowPumpPosition, out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetThrottleValvePressure(float pressure, out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual bool CloseThrottleValve(out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetFastPumpValve(bool isOpen, out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetSlowPumpValve(bool isOpen, out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual bool OpenThrottleValve(out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetAllValves(bool isOpen, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckNoError(out List<string> reason)
        {
            reason = new List<string>();
            return true;
        }
    }
}
