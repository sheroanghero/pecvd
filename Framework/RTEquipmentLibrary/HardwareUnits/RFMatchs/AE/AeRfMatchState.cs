using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.AE
{
    public struct AENavigatorMatchSts
    {
        public bool Net1OutPutOn;
        public bool Net1OutPutTuned;
        public bool Net2OutPutOn;
        public bool Net2OutPutTuned;
        public bool Net1PresetsActive;
        public bool Net1ExtPresetsSelected;

        public bool Low24VDetected;
        public bool OverTempDetected;
        public bool InterlockOpen;
        public bool FanFault;
        public bool Net1AutoMode;
        public bool Net1HostCtrlMode;
        public bool Net2AutoMode;
        public bool Net2HostCtrlMode;

        public bool AuxCapOutputTuned;
        public bool AuxCapAutoModed;
        public bool AuxCapPresetsActive;
        public bool Net1UserCtrlMode;
        public bool Net2UserCtrlMode;

        public bool Faults;
        public bool Warning;
        public bool InitMotorFailed;
        public bool Net2PresetsActive;
        public bool Net2ExtPresetsSelected;
        public bool VoltageOverLimitFault;
    }
    public struct AENavioMatchSts
    {
        public bool RF_On;
        public bool Tuned;
        public bool AutoMode;
        public bool SerialMode;
        public bool AnalogMode;
        public bool EnPresets;
        public bool GenFaultSts;
        public bool PresetAct;
        public bool EnMotorMove;
        public bool Low24VFault;
        public bool NOVRAMFault;
        public bool OverTFault;
        public bool TuneVFault;
        public bool OutputIFault;
        public bool MotorInitFault;
    }
    public struct AEMatchZScanII
    {
        public float R1;
        public float X1;
        public float Voltage1;
        public float Current1;
        public float Phase1;
        public float Power1;
        public float R2;
        public float X2;
        public float Voltage2;
        public float Current2;
        public float Phase2;
        public float Power2;
    }
    public struct AEStatusData
    {
        public float BiasPeak;     //RF峰值
        public float DCBias;       //偏压值
        public float LoadPosi1;     //Load电容位置
        public float TunePosi1;     //Tune电容位置
        public float LoadPosi2;     //Load电容位置
        public float TunePosi2;     //Tune电容位置
        public float PreLoad1;      //Load电容位置
        public float PreTune1;     //Tune电容位置

        public AENavioMatchSts Status1;  //当前状态
        public AENavigatorMatchSts Status2;  //当前状态
        public AEMatchZScanII ZScanII;
        public bool Online;           //设备在线
    }
}
