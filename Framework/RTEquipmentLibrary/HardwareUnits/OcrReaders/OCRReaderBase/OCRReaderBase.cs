using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.SubstrateTrackings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders
{
    public abstract class OCRReaderBaseDevice : Entity, IDevice, IEntity
    {
        public enum PictureOptionEnum
        {
            None =0,
            All,
            Failed,
            Success,
        }
        public OCRReaderBaseDevice(string module,string name):base()
        {
            Module = module;
            Name = name;
            InitializeOCRReader();
        }       

        public ModuleName InstalledModule { get; set; }
        public event Action<bool> ActionDone;
        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;
        public bool IsBusy = false;
        public OCRReaderStateEnum DeviceState => (OCRReaderStateEnum)fsm.State;
        public string CurrentJobName { get; set; }
        public bool IsReadLaserMark1 { get; set; }
        public string LaserMark1 { get; set; } = "";
        public string LaserMark1Score { get; set; } = "";
        public string LaserMark1ReadTime { get; set; } = string.Empty;
        public string LaserMark2 { get; set; } = "";
        public string LaserMark2Score { get; set; } = "";
        public string LaserMark2ReadTime { get; set; } = string.Empty;
        public string CurrentLaserMark { get; set; } = "";
        public float CurrentLaserMarkScore { get; set; }
        public string CurrentLaserMarkReadTime { get; set; } = "";
        public string CurrentLaserMarkOnWafer 
        {
            get 
            {
                if(InstalledModule == ModuleName.System)
                    return CurrentLaserMark;
                if (WaferManager.Instance.CheckHasWafer(InstalledModule, 0))
                    return CurrentLaserMark;
                return "";
             }
        }
        public virtual string CurrentImageFileName
        {
            get
            {
                if (SC.ContainsItem($"OCRReader.{Name}.TempImageFilePath"))
                {
                    return SC.GetStringValue($"OCRReader.{Name}.TempImageFilePath");
                }
                return "";
            }
        }
        public virtual int ScoreThresholdToSaveImage 
        { 
            get
            {
                if (SC.ContainsItem($"OCRReader.{Name}.PassScoreThreshhold"))
                    return SC.GetValue<int>($"OCRReader.{Name}.PassScoreThreshhold");
                return -1;
            }

        }

        public virtual int KeepLaserMarkCharatersNumber
        {
            get
            {
                if (SC.ContainsItem($"OCRReader.{Name}.KeepLaserMarkCharatersNumber"))
                    return SC.GetValue<int>($"OCRReader.{Name}.KeepLaserMarkCharatersNumber");
                return 100;
            }
        }
        public virtual int PicturesQuanlityLimit
        {
            get
            {
                if (SC.ContainsItem($"OCRReader.{Name}.LimitForPicture"))
                    return SC.GetValue<int>($"OCRReader.{Name}.LimitForPicture");
                return 3000;
            }
        }
        public virtual PictureOptionEnum SavePictureOption
        {
            get
            {
                if (!SC.ContainsItem($"OCRReader.{Name}.SavePictureOption"))
                    return PictureOptionEnum.All;
                return (PictureOptionEnum)SC.GetValue<int>($"OCRReader.{Name}.SavePictureOption");
            }
        }

        

        public DateTime StartReadTime { get; private set; }
        public int TimeLimitForRead 
        { get

            {
                if (SC.ContainsItem($"OCRReader.{Name}.TimeLimitForWID"))
                    return SC.GetValue<int>($"OCRReader.{Name}.TimeLimitForWID");
                return 300;
            }        
        }
        public List<string> JobFileList { get; set; }
        public bool ReadOK { get; set; }

        public string AlarmWIDReadTimeout { get => Name.ToString() + "WIDReadTimeout"; }
        public string AlarmWIDLostCommunication { get => Name.ToString() + "WIDLostCommunication"; }
        public string AlarmWIDLoadJobFail { get => Name.ToString() + "WIDLoadJobFail"; }
        public string AlarmWIDSavePictureFail { get => Name.ToString() + "WIDSavePictureFail"; }


        public string LaserMark1ReadResult { get; set; }
        public string LaserMark2ReadResult { get; set; }

        public object[] CurrentParamter { get; private set; }
        public virtual bool IsReady()
        {
            if (DeviceState != OCRReaderStateEnum.Idle)
                return false;
            if (IsBusy)
                return false;
            return true;
        }
       
        private void InitializeOCRReader()
        {
            BuildTransitionTable();
            SubscribeDataVariable();

            SubscribeOperation();
            SubscribeDeviceOperation();
            Running = true;
        }
        private void SubscribeDeviceOperation()
        {
            DEVICE.Register(string.Format("{0}.{1}", Name, "SavePicture"),//DeviceOperationName.ReadWaferID),
                (out string reason, int time, object[] param) =>
                {
                    var ret = SavePicture();
                    if (ret)
                    {
                        reason = string.Format("{0}", Name, "Save picture.");
                        return true;
                    }
                    else
                        reason = string.Format("{0}", Name, "Can't Save picture.");

                    return false;
                });
            DEVICE.Register(string.Format("{0}.{1}", Name, "ReadWaferID"),//DeviceOperationName.ReadWaferID),
                (out string reason, int time, object[] param) =>
                {
                    var bLaser = bool.Parse((string)param[0]);
                    var jobName = (string)param[1];
                    var ret = ReadWaferID(jobName, bLaser?0:1);
                    if (ret)
                    {
                        reason = string.Format("{0}", Name, "Read Laser Mark.");
                        return true;
                    }
                    else
                        reason = string.Format("{0}", Name, "Can't read Laser Mark.");

                    return false;
                });

            DEVICE.Register(string.Format("{0}.{1}", Name, "ReadLM1"),//DeviceOperationName.ReadWaferID),
                (out string reason, int time, object[] param) =>
                {                   
                    var jobName = (string)param[0];
                    var ret = ReadWaferID(jobName, 0);
                    if (ret)
                    {
                        reason = string.Format("{0}", Name, "Read Laser Mark.");
                        return true;
                    }
                    else
                        reason = string.Format("{0}", Name, "Can't read Laser Mark.");

                    return false;
                });

            DEVICE.Register(string.Format("{0}.{1}", Name, "ReadLM2"),//DeviceOperationName.ReadWaferID),
                (out string reason, int time, object[] param) =>
                {
                    reason = "";
                    var jobName = (string)param[0];
                    var ret = ReadWaferID(jobName,1);
                    if (ret)
                    {
                        reason = string.Format("{0}", Name, "Read Laser Mark.");
                        return true;
                    }
                    else
                        reason = string.Format("{0}", Name, "Can't read Laser Mark.");
                    return false;
                });
            DEVICE.Register($"{Name}.RefreshJobList", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return CheckToPostMessage((int)OCRReaderMsg.ReadParameter, new object[] { "JobList" });                
            });
            DEVICE.Register($"{Name}.Reset", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return CheckToPostMessage((int)OCRReaderMsg.Reset, new object[] { "Reset" });
            });
            DEVICE.Register($"{Name}.ManualSetLaserMark", (out string reason, int time, object[] param) =>
            {
                reason = "";
                CurrentLaserMark = param[0].ToString();
                var wafer = WaferManager.Instance.GetWafer(InstalledModule, 0);
                if (!wafer.IsEmpty)
                {
                    wafer.LaserMarker = CurrentLaserMark;
                    Guid guid = Guid.NewGuid();
                    OCRDataRecorder.OcrReadComplete(guid.ToString(), wafer.WaferID, wafer.OriginStation.ToString(),
                               wafer.OriginCarrierID ?? "", wafer.OriginSlot.ToString(), "0", "0", true, CurrentLaserMark,
                              "0", "0");

                }
                return true;
            });
        }
        private void SubscribeOperation()
        {
            OP.Subscribe($"{Name}.SavePicture", (string cmd, object[] param) =>
            {               
                var ret = SavePicture();
                if (!ret)
                {
                    EV.PostWarningLog(Module, $"{Name} can not save picture.");
                    return false;
                }
                EV.PostInfoLog(Module, $"{Name} start save picture");
                return true;
            });



            OP.Subscribe($"{Name}.ReadWaferID", (string cmd, object[] param) =>
            {
                var bLaser = bool.Parse(param[0].ToString());
                var jobName = (string)param[1];
                var ret = ReadWaferID(jobName, bLaser ? 0 : 1);
                if (!ret)
                {
                    EV.PostWarningLog(Module, $"{Name} can not read laser mark.");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Name} start read laser mark");
                return true;
            });
            OP.Subscribe($"{Name}.ReadLM1", (string cmd, object[] param) =>
            {
                var jobName = (string)param[0];
                var ret = ReadWaferID(jobName, 0);
                if (ret)
                {
                    
                    return true;
                }               

                return false;
            });

            OP.Subscribe($"{Name}.RefreshJobList", (string cmd, object[] param) =>
            {               
                return CheckToPostMessage((int)OCRReaderMsg.ReadParameter, new object[] { "JobList" });
            });
            OP.Subscribe($"{Name}.Reset", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return CheckToPostMessage((int)OCRReaderMsg.Reset, new object[] { "Reset" });
            });

        }
        private void SubscribeDataVariable()
        {
            DATA.Subscribe(Name, "WIDReaderState", () => DeviceState.ToString());
            DATA.Subscribe(Name, "WIDReaderBusy", () => (DeviceState != OCRReaderStateEnum.Idle && DeviceState != OCRReaderStateEnum.Init));
            DATA.Subscribe(Name, "WRIDReaderState", () => DeviceState.ToString());
            DATA.Subscribe(Name, "WRIDReaderBusy", () => (DeviceState != OCRReaderStateEnum.Idle && DeviceState != OCRReaderStateEnum.Init));

            DATA.Subscribe(Name, "LaserMaker1", () => LaserMark1);
            DATA.Subscribe(Name, "LaserMaker2", () => LaserMark2);
            DATA.Subscribe(Name, "LaserMark1Result", () => LaserMark1ReadResult);
            DATA.Subscribe(Name, "LaserMark2Result", () => LaserMark2ReadResult);
            DATA.Subscribe(Name, "JobFileList", () => JobFileList);
            DATA.Subscribe(Name, "CurrentLaserMark", () => CurrentLaserMarkOnWafer);
            DATA.Subscribe(Name, "CurrentImageFileName", () => CurrentImageFileName);
            DATA.Subscribe(Name, "CurrentWaferID", () => WaferManager.Instance.GetWafer(InstalledModule, 0).WaferID);


            //EV.Subscribe(new EventItem("Alarm", AlarmWIDLoadJobFail, $"{Name} WID Reader load job failed.", EventLevel.Alarm, EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmWIDLostCommunication, $"{Name} WID Reader lost communication.", EventLevel.Alarm, EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmWIDReadTimeout, $"{Name} WID Reader read laser mark timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmWIDSavePictureFail, $"{Name} WID Reade save picture timeout.", EventLevel.Alarm, EventType.EventUI_Notify));

        }
        private void BuildTransitionTable()
        {
            fsm = new StateMachine<OCRReaderBaseDevice>(Module + Name + ".OCRReaderReaderStateMachine", (int)OCRReaderStateEnum.Init, 50);

            AnyStateTransition(OCRReaderMsg.Error, fError, OCRReaderStateEnum.Error);
            AnyStateTransition(OCRReaderMsg.Reset, fStartReset, OCRReaderStateEnum.Resetting);

            Transition(OCRReaderStateEnum.Resetting, OCRReaderMsg.ActionDone, fResetComplete, OCRReaderStateEnum.Idle);
            Transition(OCRReaderStateEnum.Resetting, OCRReaderMsg.ResetComplete, fResetComplete, OCRReaderStateEnum.Idle);
            Transition(OCRReaderStateEnum.Resetting, FSM_MSG.TIMER, fMonitorReset, OCRReaderStateEnum.Idle);

            Transition(OCRReaderStateEnum.Init, OCRReaderMsg.StartInit, fStartInit, OCRReaderStateEnum.Initializing);
            Transition(OCRReaderStateEnum.Initializing, OCRReaderMsg.InitComplete, fInitComplete, OCRReaderStateEnum.Initializing);
            Transition(OCRReaderStateEnum.Initializing, OCRReaderMsg.ActionDone, fInitComplete, OCRReaderStateEnum.Idle);
            Transition(OCRReaderStateEnum.Initializing, FSM_MSG.TIMER, fMonitorInit, OCRReaderStateEnum.Idle);

            Transition(OCRReaderStateEnum.Error, OCRReaderMsg.Clear, fStartClear, OCRReaderStateEnum.Idle);
            
            Transition(OCRReaderStateEnum.Idle, OCRReaderMsg.SetParameter, fStartSetParameter, OCRReaderStateEnum.SetParameter);
            Transition(OCRReaderStateEnum.SetParameter, OCRReaderMsg.SetComplete, fSetParameterComplete, OCRReaderStateEnum.SetParameter);
            Transition(OCRReaderStateEnum.SetParameter, OCRReaderMsg.ActionDone, fSetParameterComplete, OCRReaderStateEnum.SetParameter);

            Transition(OCRReaderStateEnum.Idle, OCRReaderMsg.ReadParameter, fStartReadParameter, OCRReaderStateEnum.ReadParameter);
            Transition(OCRReaderStateEnum.ReadParameter, OCRReaderMsg.ReadParaComplete, fReadParameterComplete, OCRReaderStateEnum.Idle);
            Transition(OCRReaderStateEnum.ReadParameter, OCRReaderMsg.ActionDone, fReadParameterComplete, OCRReaderStateEnum.Idle);
            Transition(OCRReaderStateEnum.ReadParameter, FSM_MSG.TIMER, fMonitorReadParameter, OCRReaderStateEnum.Idle);


            Transition(OCRReaderStateEnum.Idle, OCRReaderMsg.ReadWaferID, fStartReadWaferID, OCRReaderStateEnum.ReadWaferID);
            Transition(OCRReaderStateEnum.ReadWaferID, OCRReaderMsg.ReadCarrierIDComplete, fReadWaferIDComplete, OCRReaderStateEnum.Idle);
            Transition(OCRReaderStateEnum.ReadWaferID, OCRReaderMsg.ActionDone, fReadWaferIDComplete, OCRReaderStateEnum.Idle);
            Transition(OCRReaderStateEnum.ReadWaferID, FSM_MSG.TIMER, fMonitorReading, OCRReaderStateEnum.Idle);

            Transition(OCRReaderStateEnum.Idle, OCRReaderMsg.SavePicture, fStartSavePicture, OCRReaderStateEnum.SavingPicture);
            Transition(OCRReaderStateEnum.SavingPicture, OCRReaderMsg.SavePictureComplete, fSavePictureComplete, OCRReaderStateEnum.Idle);
            Transition(OCRReaderStateEnum.SavingPicture, OCRReaderMsg.ActionDone, fSavePictureComplete, OCRReaderStateEnum.Idle);
            Transition(OCRReaderStateEnum.SavingPicture, FSM_MSG.TIMER, fMonitorSavingPicture, OCRReaderStateEnum.Idle);

        }

        protected virtual void OnWaferIDRead(string wid, string score, string readtime)
        {
            EV.PostInfoLog(Name, $"{Name} read laser mark successfully,ID:{wid},score:{score},read time:{readtime}.");
            CheckToPostMessage((int)OCRReaderMsg.ReadCarrierIDComplete, null);
        }

        protected virtual bool fMonitorSavingPicture(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorReading(object[] param)
        {
            if (DateTime.Now - StartReadTime > TimeSpan.FromSeconds((double)TimeLimitForRead))
            {
                OnError(AlarmWIDReadTimeout);
            }
            return false;
        }

        protected virtual bool fMonitorReadParameter(object[] param)
        {
            return false;
        }

        protected virtual bool fSavePictureComplete(object[] param)
        {            
            return true; ;
        }

        protected abstract bool fStartSavePicture(object[] param);
        

        protected virtual bool fError(object[] param)
        {
            return true;
        }

        protected abstract bool fStartReset(object[] param);

        protected virtual bool fResetComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorReset(object[] param)
        {
            return true;
        }

        protected abstract bool fStartInit(object[] param);

        protected virtual bool fInitComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorInit(object[] param)
        {
            return true;
        }

        protected virtual bool fStartClear(object[] param)
        {
            return true;
        }

        protected abstract bool fStartSetParameter(object[] param);

        protected virtual bool fSetParameterComplete(object[] param)
        {
            return true;
        }

        protected abstract bool fStartReadParameter(object[] param);

        protected virtual bool fReadParameterComplete(object[] param)
        {
            return true;
        }

        protected abstract bool fStartReadWaferID(object[] param);


        protected virtual bool fReadWaferIDComplete(object[] param)
        {
            return true;
        }

        public virtual bool ReadWaferID(string jobname,int lasermarkindex=0)
        {
            StartReadTime = DateTime.Now;
            return CheckToPostMessage((int)OCRReaderMsg.ReadWaferID, new object[] { jobname, lasermarkindex });
        }
    
        public abstract string[] GetJobFileList();

        public bool SavePicture(string path = "",string filename = "")
        {
            return CheckToPostMessage((int)OCRReaderMsg.SavePicture, new object[] { "SavePicture", path, filename });
        }

        protected override bool Init()
        {
            return base.Init();
        }

        public virtual void Monitor()
        {
            return;
        }

        public virtual void Reset()
        {
            
        }

        public virtual bool OcrReset()
        {
            return CheckToPostMessage((int)OCRReaderMsg.Reset, new object[] { "Reset" });
        }
        public virtual bool OnActionDone(object[] param)
        {
            IsBusy = false;
            CheckToPostMessage((int)OCRReaderMsg.ActionDone, new object[] { "ActionDone" });
            if (ActionDone != null)
                ActionDone(true);
            return true;
        }

        public void OnActionDone(bool result)
        {
            if (ActionDone != null)
                ActionDone(result);
        }
        public virtual bool OnError(string errorMsg)
        {
            EV.PostAlarmLog(Module, $"{Name} occured error:{errorMsg}");
            return CheckToPostMessage((int)OCRReaderMsg.Error, null);

        }
        public bool CheckToPostMessage(int msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                EV.PostWarningLog(Name, $"{Name} is in { (OCRReaderStateEnum)fsm.State} state，can not do {(OCRReaderMsg)msg}");
                return false;
            }
            if ((OCRReaderMsg)msg == OCRReaderMsg.Reset || (OCRReaderMsg)msg == OCRReaderMsg.ReadParameter ||
                (OCRReaderMsg)msg == OCRReaderMsg.ReadWaferID || (OCRReaderMsg)msg == OCRReaderMsg.SavePicture ||
                (OCRReaderMsg)msg == OCRReaderMsg.SetParameter || (OCRReaderMsg)msg == OCRReaderMsg.StartInit)
            {
                CurrentParamter = args;
                IsBusy = true;
            }
            else
                IsBusy = false;
            fsm.PostMsg(msg, args);

            return true;
        }

        public bool Check(int msg, out string reason, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                reason = String.Format("{0} is in {1} state，can not do {2}", Name, (OCRReaderStateEnum)fsm.State, (OCRReaderMsg)msg);
                return false;
            }
            reason = "";

            return true;
        }

        
        public virtual bool Error
        {
            get;
        }
        public string Module { get ; set ; }
        public string Name { get; set ; }

        public bool HasAlarm { get; set; }
    }
    public enum OCRReaderStateEnum
    {
        Undefined = 0,
        Init,
        Initializing,
        Idle,
        SetParameter,
        ReadParameter,
        ReadWaferID, 
        SavingPicture,
        Error,
        Resetting,
    }
    public enum OCRReaderMsg
    {
        Reset,
        ResetComplete,
        Clear,
        StartInit,
        InitComplete,
        SetParameter,
        SetComplete,
        ReadParameter,
        ReadParaComplete,
        ReadWaferID,
        ReadCarrierIDComplete,
        SavePicture,
        SavePictureComplete,
        ActionDone,
        Error,

    }

}
