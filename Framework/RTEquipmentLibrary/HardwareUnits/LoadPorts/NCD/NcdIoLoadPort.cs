using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Routine;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDKB;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.VCE.BrooksVCE;

//namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.NCD
namespace Aitex.Core.RT.Device.Unit
{
    public class NcdIoLoadPort:LoadPortBaseDevice
    {
        public NcdIoLoadPort(string module, XmlElement node, string ioModule = ""):
            base(node.GetAttribute("module"), node.GetAttribute("id"))
        {
            base.Module = node.GetAttribute("module");//string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            
            ioModule = node.GetAttribute("ioModule");
            _doRecipeSaveCommand = ParseDoNode("DO_RecipeSaveCommand", node, ioModule);
            _doRecipeLoadCommand = ParseDoNode("DO_RecipeLoadCommand", node, ioModule);
            _doIntialCommand = ParseDoNode("DO_IntialCommand", node, ioModule);
            _doResetCommand = ParseDoNode("DO_ResetCommand", node, ioModule);
            _doSingleMappingCommand = ParseDoNode("DO_SingleMappingCommand", node, ioModule);
            _doLoadLight = ParseDoNode("DO_LoadLight", node, ioModule);
            _doUnloadLight = ParseDoNode("DO_UnloadLight", node, ioModule);
            _doStatu1Light = ParseDoNode("DO_Statu1Light", node, ioModule);
            _doStatu2Light = ParseDoNode("DO_Statu2Light", node, ioModule);
            _doLoadCommand = ParseDoNode("DO_LoadCommand", node, ioModule);
            _doUnloadCommand = ParseDoNode("DO_UnloadCommand", node, ioModule);
            _doMapDisableCommand = ParseDoNode("DO_MapDisableCommand", node, ioModule);
            _doMapEnableCommand = ParseDoNode("DO_MapEnableCommand", node, ioModule);
            _doClampOnCommand = ParseDoNode("DO_ClampOnCommand", node, ioModule);
            _doClampOffCommand = ParseDoNode("DO_ClampOffCommand", node, ioModule);
            _doDOCKForwardCommand = ParseDoNode("DO_DOCKForwardCommand", node, ioModule);
            _doDOCKHomeCommand = ParseDoNode("DO_DOCKHomeCommand", node, ioModule);
            _doLatchOnCommand = ParseDoNode("DO_LatchOnCommand", node, ioModule);
            _doLatchOffCommand = ParseDoNode("DO_LatchOffCommand", node, ioModule);
            _doDoorCloseCommand = ParseDoNode("DO_DoorCloseCommand", node, ioModule);
            _doDoorOpenCommand = ParseDoNode("DO_DoorOpenCommand", node, ioModule);
            _doVACOnCommand = ParseDoNode("DO_VACOnCommand", node, ioModule);
            _doVACOffCommand = ParseDoNode("DO_VACOffCommand", node, ioModule);
            _doVertiaclDownCommand = ParseDoNode("DO_VertiaclDownCommand", node, ioModule);
            _doVertiaclUpCommand = ParseDoNode("DO_VertiaclUpCommand", node, ioModule);
            _doZtoMAPPINGSTARTPOSI = ParseDoNode("DO_ZtoMAPPINGSTARTPOSI", node, ioModule);
            _doZtoMAPPINGENDPOSI = ParseDoNode("DO_ZtoMAPPINGENDPOSI", node, ioModule);
            _doMAPIN = ParseDoNode("DO_MAPIN", node, ioModule);
            _doMAPOUT = ParseDoNode("DO_MAPOUT", node, ioModule);

            _aoSAVECurrentRecipeNumber = ParseAoNode("AO_SAVECurrentRecipeNumber", node, ioModule);
            _aoMAPSTAR = ParseAoNode("AO_MAPSTAR", node, ioModule);
            _aoMAPEND = ParseAoNode("AO_MAPEND", node, ioModule);
            _aoSENSOR = ParseAoNode("AO_SENSOR", node, ioModule);
            _aoSLOT = ParseAoNode("AO_SLOT", node, ioModule);
            _aoPITCH = ParseAoNode("AO_PITCH", node, ioModule);
            _aoPOSITIONRANGE = ParseAoNode("AO_POSITIONRANGE", node, ioModule);
            _aoPOSITIONRANGEUPPER = ParseAoNode("AO_POSITIONRANGEUPPER", node, ioModule);
            _aoPOSITIONRANGELOWER = ParseAoNode("AO_POSITIONRANGELOWER", node, ioModule);
            _aoTHICK = ParseAoNode("AO_THICK", node, ioModule);
            _aoTHICKRANGE = ParseAoNode("AO_THICKRANGE", node, ioModule);
            _aoOFFSET = ParseAoNode("AO_OFFSET", node, ioModule);
            _aoCarrierType = ParseAoNode("AO_CarrierType", node, ioModule);

            _aiSlot1MapResult = ParseAiNode("AI_Slot1MapResult", node, ioModule);
            _aiSlot2MapResult = ParseAiNode("AI_Slot2MapResult", node, ioModule);
            _aiSlot3MapResult = ParseAiNode("AI_Slot3MapResult", node, ioModule);
            _aiSlot4MapResult = ParseAiNode("AI_Slot4MapResult", node, ioModule);
            _aiSlot5MapResult = ParseAiNode("AI_Slot5MapResult", node, ioModule);
            _aiSlot6MapResult = ParseAiNode("AI_Slot6MapResult", node, ioModule);
            _aiSlot7MapResult = ParseAiNode("AI_Slot7MapResult", node, ioModule);
            _aiSlot8MapResult = ParseAiNode("AI_Slot8MapResult", node, ioModule);
            _aiSlot9MapResult = ParseAiNode("AI_Slot9MapResult", node, ioModule);
            _aiSlot10MapResult = ParseAiNode("AI_Slot10MapResult", node, ioModule);
            _aiSlot11MapResult = ParseAiNode("AI_Slot11MapResult", node, ioModule);
            _aiSlot12MapResult = ParseAiNode("AI_Slot12MapResult", node, ioModule);
            _aiSlot13MapResult = ParseAiNode("AI_Slot13MapResult", node, ioModule);
            _aiSlot14MapResult = ParseAiNode("AI_Slot14MapResult", node, ioModule);
            _aiSlot15MapResult = ParseAiNode("AI_Slot15MapResult", node, ioModule);
            _aiSlot16MapResult = ParseAiNode("AI_Slot16MapResult", node, ioModule);
            _aiSlot17MapResult = ParseAiNode("AI_Slot17MapResult", node, ioModule);
            _aiSlot18MapResult = ParseAiNode("AI_Slot18MapResult", node, ioModule);
            _aiSlot19MapResult = ParseAiNode("AI_Slot19MapResult", node, ioModule);
            _aiSlot20MapResult = ParseAiNode("AI_Slot20MapResult", node, ioModule);
            _aiSlot21MapResult = ParseAiNode("AI_Slot21MapResult", node, ioModule);
            _aiSlot22MapResult = ParseAiNode("AI_Slot22MapResult", node, ioModule);
            _aiSlot23MapResult = ParseAiNode("AI_Slot23MapResult", node, ioModule);
            _aiSlot24MapResult = ParseAiNode("AI_Slot24MapResult", node, ioModule);
            _aiSlot25MapResult = ParseAiNode("AI_Slot25MapResult", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot1 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot1", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot1 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot1", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot2 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot2", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot2 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot2", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot3 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot3", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot3 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot3", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot4 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot4", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot4 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot4", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot5 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot5", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot5 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot5", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot6 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot6", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot6 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot6", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot7 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot7", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot7 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot7", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot8 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot8", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot8 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot8", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot9 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot9", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot9 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot9", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot10 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot10", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot10 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot10", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot11 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot11", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot11 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot11", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot12 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot12", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot12 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot12", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot13 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot13", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot13 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot13", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot14 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot14", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot14 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot14", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot15 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot15", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot15 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot15", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot16 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot16", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot16 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot16", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot17 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot17", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot17 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot17", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot18 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot18", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot18 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot18", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot19 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot19", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot19 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot19", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot20 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot20", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot20 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot20", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot21 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot21", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot21 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot21", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot22 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot22", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot22 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot22", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot23 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot23", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot23 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot23", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot24 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot24", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot24 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot24", node, ioModule);
            _aiWAFERPOSITIONUPPEREDGE_Slot25 = ParseAiNode("AI_WAFERPOSITIONUPPEREDGE_Slot25", node, ioModule);
            _aiWAFERPOSITIONLOWEREDGE_Slot25 = ParseAiNode("AI_WAFERPOSITIONLOWEREDGE_Slot25", node, ioModule);
            _aiErrorCode = ParseAiNode("AI_ErrorCode", node, ioModule);
            _aiLastErrorCode = ParseAiNode("AI_LastErrorCode", node, ioModule);

            _aiRECIPE_TYPE_FEEDBACK = ParseAiNode("AI_RECIPE_TYPE_FEEDBACK", node, ioModule);



            _diAck_RecieSave = ParseDiNode("DI_Ack_RecieSave", node, ioModule);
            _diAck_RecipeLoad = ParseDiNode("DI_Ack_RecipeLoad", node, ioModule);
            _diAck_Init = ParseDiNode("DI_Ack_Init", node, ioModule);
            _diAck_Reset = ParseDiNode("DI_Ack_Reset", node, ioModule);
            _diAck_SingleMap = ParseDiNode("DI_Ack_SingleMap", node, ioModule);


            _diAck_HomePos = ParseDiNode("DI_Ack_HomePos", node, ioModule);
            _diSensor_Presence = ParseDiNode("DI_Sensor_Presence", node, ioModule);
            _diSensor_Placement = ParseDiNode("DI_Sensor_Placement", node, ioModule);
            _diSensor_Protrude = ParseDiNode("DI_Sensor_Protrude", node, ioModule);
            _diSensor_VerticalDown = ParseDiNode("DI_Sensor_VerticalDown", node, ioModule);
            _diAck_Load = ParseDiNode("DI_Ack_Load", node, ioModule);
            _diAck_Unload = ParseDiNode("DI_Ack_Unload", node, ioModule);
            _diAck_MapDisable = ParseDiNode("DI_Ack_MapDisable", node, ioModule);
            _diAck_MapEnable = ParseDiNode("DI_Ack_MapEnable", node, ioModule);
            _diAck_ClampOn = ParseDiNode("DI_Ack_ClampOn", node, ioModule);
            _diAck_ClampOff = ParseDiNode("DI_Ack_ClampOff", node, ioModule);
            _diAck_DockFwd = ParseDiNode("DI_Ack_DockFwd", node, ioModule);
            _diAck_DockHome = ParseDiNode("DI_Ack_DockHome", node, ioModule);
            _diAck_LatchOn = ParseDiNode("DI_Ack_LatchOn", node, ioModule);
            _diAck_LatchOff = ParseDiNode("DI_Ack_LatchOff", node, ioModule);
            _diAck_DoorClose = ParseDiNode("DI_Ack_DoorClose", node, ioModule);
            _diAck_DoorOpen = ParseDiNode("DI_Ack_DoorOpen", node, ioModule);
            _diAck_VacOn = ParseDiNode("DI_Ack_VacOn", node, ioModule);
            _diAck_VacOff = ParseDiNode("DI_Ack_VacOff", node, ioModule);
            _diAck_VerticalDown = ParseDiNode("DI_Ack_VerticalDown", node, ioModule);
            _diAck_VerticalUp = ParseDiNode("DI_Ack_VerticalUp", node, ioModule);
            _diAck_ZtoMapStart = ParseDiNode("DI_Ack_ZtoMapStart", node, ioModule);
            _diAck_ZtoMapEnd = ParseDiNode("DI_Ack_ZtoMapEnd", node, ioModule);
            _diAck_Mapin = ParseDiNode("DI_Ack_Mapin", node, ioModule);
            _diAck_MapOut = ParseDiNode("DI_Ack_MapOut", node, ioModule);
            _diAck_CmdClear = ParseDiNode("DI_Ack_CmdClear", node, ioModule);
            _diAck_E84Disable = ParseDiNode("DI_Ack_E84Disable", node, ioModule);
            _diAck_E84Enable = ParseDiNode("DI_Ack_E84Enable", node, ioModule);


            _di12MapSensor = ParseDiNode("DI_12MapSensor", node, ioModule);
            _di8MapSensor = ParseDiNode("DI_8MapSensor", node, ioModule);
            _diSERVOHOME = ParseDiNode("DI_SERVOHOME", node, ioModule);
            _diFoupPlacementSensor = ParseDiNode("DI_FoupPlacementSensor", node, ioModule);
            _diDoorCheckSensor = ParseDiNode("DI_DoorCheckSensor", node, ioModule);
            _diCDASensor = ParseDiNode("DI_CDASensor", node, ioModule);
            _diDockHomeSensor = ParseDiNode("DI_DockHomeSensor", node, ioModule);
            _diClampOffSensor = ParseDiNode("DI_ClampOffSensor", node, ioModule);
            _diPodForwardSensor = ParseDiNode("DI_PodForwardSensor", node, ioModule);
            _diClampOnSensor = ParseDiNode("DI_ClampOnSensor", node, ioModule);
            _diVACSensor = ParseDiNode("DI_VACSensor", node, ioModule);
            _diLatchLockSensor = ParseDiNode("DI_LatchLockSensor", node, ioModule);
            _diLatchUnlockSensor = ParseDiNode("DI_LatchUnlockSensor", node, ioModule);
            _diDoorOpenSensor = ParseDiNode("DI_DoorOpenSensor", node, ioModule);
            _diDoorCloseSensor = ParseDiNode("DI_DoorCloseSensor", node, ioModule);
            _diVerticalMiddleSensor = ParseDiNode("DI_VerticalMiddleSensor", node, ioModule);
            _diVerticalDownSensor = ParseDiNode("DI_VerticalDownSensor", node, ioModule);
            _diOPSwitchButton = ParseDiNode("DI_OPSwitchButton", node, ioModule);
            _diMapin = ParseDiNode("DI_Mapin", node, ioModule);
            _diMapout = ParseDiNode("DI_Mapout", node, ioModule);
            _diProtrudesensor = ParseDiNode("DI_Protrudesensor", node, ioModule);
            _diInfoPadA = ParseDiNode("DI_InfoPadA", node, ioModule);
            _diInfoPadB = ParseDiNode("DI_InfoPadB", node, ioModule);
            _diInfoPadC = ParseDiNode("DI_InfoPadC", node, ioModule);
            _diInfoPadD = ParseDiNode("DI_InfoPadD", node, ioModule);
            _diServoOn = ParseDiNode("DI_ServoOn", node, ioModule);
            _diPresence = ParseDiNode("DI_Presence", node, ioModule);
            _diStepError = ParseDiNode("DI_StepError", node, ioModule);
            _diServoerror = ParseDiNode("DI_Servoerror", node, ioModule);
            _diDoorSafe = ParseDiNode("DI_DoorSafe", node, ioModule);
            _diSERVO = ParseDiNode("DI_SERVO", node, ioModule);
            _diSTEP = ParseDiNode("DI_STEP", node, ioModule);

            _diClampOnCylinder = ParseDiNode("DI_ClampOnCylinder", node, ioModule);
            _diClampOffCylinder = ParseDiNode("DI_ClampOffCylinder", node, ioModule);
            _diLatchOnCylinder = ParseDiNode("DI_LatchOnCylinder", node, ioModule);
            _diLatchOffCylinder = ParseDiNode("DI_LatchOffCylinder", node, ioModule);
            _diDoorOpenCylinder = ParseDiNode("DI_DoorOpenCylinder", node, ioModule);
            _diDoorCloseCylinder = ParseDiNode("DI_DoorCloseCylinder", node, ioModule);
            _diSERVOCLEAR1 = ParseDiNode("DI_SERVOCLEAR1", node, ioModule);
            _diSERVOON1 = ParseDiNode("DI_SERVOON1", node, ioModule);
            _diD3UBIN = ParseDiNode("DI_D3UBIN", node, ioModule);
            _diMapForwardCylinder = ParseDiNode("DI_MapForwardCylinder", node, ioModule);
            _diSERVOCLEAR2 = ParseDiNode("DI_SERVOCLEAR2", node, ioModule);
            _diSERVOON2 = ParseDiNode("DI_SERVOON2", node, ioModule);
            _diSERVOCLCOUNT = ParseDiNode("DI_SERVOCLCOUNT", node, ioModule);
            _diVAC = ParseDiNode("DI_VAC", node, ioModule);
            _diSERVOBRAKE = ParseDiNode("DI_SERVOBRAKE", node, ioModule);

            _diLoadLight = ParseDiNode("DI_LoadLight", node, ioModule);
            _diUnloadLight = ParseDiNode("DI_UnloadLight", node, ioModule);
            _diPOWERLIGHT = ParseDiNode("DI_POWERLIGHT", node, ioModule);
            _diFoupPlacementLight = ParseDiNode("DI_FoupPlacementLight", node, ioModule);
            _diFoupPresenceLight = ParseDiNode("DI_FoupPresenceLight", node, ioModule);
            _diAlarmOccurLight = ParseDiNode("DI_AlarmOccurLight", node, ioModule);
            _diStatu1Light = ParseDiNode("DI_Statu1Light", node, ioModule);
            _diStatu2Light = ParseDiNode("DI_Statu2Light", node, ioModule);


            SubscribeData();
            SubscribeEventAndAlarm();
            IsMapWaferByLoadPort = true;
            _carrierArrived = new R_TRIG();
            _errorOccurredTrig = new R_TRIG();
            _lastErrorOccurredTrig = new R_TRIG();
            _alarmWaferProtrudeTrig = new R_TRIG();
            _thread = new PeriodicJob(10, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            

        }        

        public string[] AlarmNcdLoadPortErrors;
        public string[] AlarmNcdLoadPortLastErrors;

        private void SubscribeEventAndAlarm()
        {
            AlarmNcdLoadPortErrors = new string[73];
            for(int i=0;i<73;i++)
            {
                AlarmNcdLoadPortErrors[i] = "Ncd " + LPModuleName.ToString() + "Error" + (i + 1).ToString();
                EV.Subscribe(new EventItem(64000 + (((int)LPModuleName-1) *100) + i+1, "Alarm", AlarmNcdLoadPortErrors[i], $"Load Port {Name} occurred error, D2801 error code is {i+1}.", EventLevel.Alarm, EventType.EventUI_Notify));
            }
            AlarmNcdLoadPortLastErrors = new string[29];
            for (int i = 0; i < 29; i++)
            {
                AlarmNcdLoadPortLastErrors[i] = "Ncd " + LPModuleName.ToString() + "LastError" + (i + 1).ToString();
                EV.Subscribe(new EventItem("Alarm", AlarmNcdLoadPortLastErrors[i], $"Load Port {Name} occurred error, D2802 error code is {i + 1}.", EventLevel.Alarm, EventType.EventUI_Notify));
            }

            EV.Subscribe(new EventItem("Alarm", AlarmLoadPortLoadRecipeFailed, $"Load Port {Name} load recipe Timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
        }

        private string AlarmLoadPortLoadRecipeFailed 
        { 
            get => LPModuleName.ToString() + "LoadRecipeFailed";
    
        }

        private void SubscribeData()
        {

            DATA.Subscribe($"{Module}.{Name}.SystemStatus", () => CurrentState.ToString());
            DATA.Subscribe($"{Module}.{Name}.Mode", () => Mode.ToString());
            DATA.Subscribe($"{Module}.{Name}.InitPosMovement", () => InitPosMovement.ToString());
            DATA.Subscribe($"{Module}.{Name}.OperationStatus", () => OperationStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => ErrorCode.ToString());
            DATA.Subscribe($"{Module}.{Name}.ContainerStatus", () => ContainerStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.ClampPosition", () => ClampPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.LPDoorLatchPosition", () => LPDoorLatchPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.VacuumStatus", () => VacuumStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.LPDoorState", () => LPDoorState.ToString());
            DATA.Subscribe($"{Module}.{Name}.WaferProtrusion", () => WaferProtrusion.ToString());
            DATA.Subscribe($"{Module}.{Name}.ElevatorAxisPosition", () => ElevatorAxisPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.MapperPostion", () => MapperPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingStatus", () => MappingStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.Model", () => Model.ToString());
            DATA.Subscribe($"{Module}.{Name}.IsFosbModeActual", () => IsFosbModeActual.ToString());

            DATA.Subscribe($"{Module}.{Name}.DockPosition", () => DockPosition.ToString());


            DATA.Subscribe($"{Name}.SystemStatus", () => CurrentState.ToString());
            DATA.Subscribe($"{Name}.Mode", () => Mode.ToString());
            DATA.Subscribe($"{Name}.InitPosMovement", () => InitPosMovement.ToString());
            DATA.Subscribe($"{Name}.OperationStatus", () => OperationStatus.ToString());
            DATA.Subscribe($"{Name}.ErrorCode", () => ErrorCode.ToString());
            DATA.Subscribe($"{Name}.ContainerStatus", () => ContainerStatus.ToString());
            DATA.Subscribe($"{Name}.ClampPosition", () => ClampPosition.ToString());
            DATA.Subscribe($"{Name}.LPDoorLatchPosition", () => LPDoorLatchPosition.ToString());
            DATA.Subscribe($"{Name}.VacuumStatus", () => VacuumStatus.ToString());
            DATA.Subscribe($"{Name}.LPDoorState", () => LPDoorState.ToString());
            DATA.Subscribe($"{Name}.WaferProtrusion", () => WaferProtrusion.ToString());
            DATA.Subscribe($"{Name}.ElevatorAxisPosition", () => ElevatorAxisPosition.ToString());
            DATA.Subscribe($"{Name}.MapperPostion", () => MapperPostion.ToString());
            DATA.Subscribe($"{Name}.MappingStatus", () => MappingStatus.ToString());
            DATA.Subscribe($"{Name}.Model", () => Model.ToString());
            DATA.Subscribe($"{Name}.IsFosbModeActual", () => IsFosbModeActual.ToString());

            DATA.Subscribe($"{Name}.DockPosition", () => DockPosition.ToString());
        }

        private PeriodicJob _thread;
        private DOAccessor _doRecipeSaveCommand;
        private DOAccessor _doRecipeLoadCommand;
        private DOAccessor _doIntialCommand;
        private DOAccessor _doResetCommand;
        private DOAccessor _doSingleMappingCommand;


        private DOAccessor _doLoadLight;
        private DOAccessor _doUnloadLight;
        private DOAccessor _doStatu1Light;
        private DOAccessor _doStatu2Light;
        private DOAccessor _doLoadCommand;
        private DOAccessor _doUnloadCommand;
        private DOAccessor _doMapDisableCommand;
        private DOAccessor _doMapEnableCommand;
        private DOAccessor _doClampOnCommand;
        private DOAccessor _doClampOffCommand;
        private DOAccessor _doDOCKForwardCommand;
        private DOAccessor _doDOCKHomeCommand;
        private DOAccessor _doLatchOnCommand;
        private DOAccessor _doLatchOffCommand;
        private DOAccessor _doDoorCloseCommand;
        private DOAccessor _doDoorOpenCommand;
        private DOAccessor _doVACOnCommand;
        private DOAccessor _doVACOffCommand;
        private DOAccessor _doVertiaclDownCommand;
        private DOAccessor _doVertiaclUpCommand;
        private DOAccessor _doZtoMAPPINGSTARTPOSI;
        private DOAccessor _doZtoMAPPINGENDPOSI;
        private DOAccessor _doMAPIN;
        private DOAccessor _doMAPOUT;

        private AOAccessor _aoSAVECurrentRecipeNumber;
        private AOAccessor _aoMAPSTAR; //MAP STAR(10um)    [ 2000<VALUE<30000]		
        private AOAccessor _aoMAPEND;//MAP END(10um)   [2000<VALUE<30000]	
        private AOAccessor _aoSENSOR; //SENSOR (1：12寸   ;    2：8寸）	
        private AOAccessor _aoSLOT;
        private AOAccessor _aoPITCH;
        private AOAccessor _aoPOSITIONRANGE; //POSITION RANGE(10um)
        private AOAccessor _aoPOSITIONRANGEUPPER; //POSITION RANGE UPPER(％)	
        private AOAccessor _aoPOSITIONRANGELOWER; //POSITION RANGE LOWER(％)		
        private AOAccessor _aoTHICK;//THICK(10um)		
        private AOAccessor _aoTHICKRANGE;//THICK RANGE(10um)	
        private AOAccessor _aoOFFSET;//OFFSET(10um)
        private AOAccessor _aoCarrierType;//料盒种类（1-FOUP，2-FOSB，3-Open Cassette）

        private AIAccessor _aiSlot1MapResult;    // ( 0:No Wafer, 1:Wafer, 2:Crossed )
        private AIAccessor _aiSlot2MapResult;
        private AIAccessor _aiSlot3MapResult;
        private AIAccessor _aiSlot4MapResult;
        private AIAccessor _aiSlot5MapResult;
        private AIAccessor _aiSlot6MapResult;
        private AIAccessor _aiSlot7MapResult;
        private AIAccessor _aiSlot8MapResult;
        private AIAccessor _aiSlot9MapResult;
        private AIAccessor _aiSlot10MapResult;
        private AIAccessor _aiSlot11MapResult;
        private AIAccessor _aiSlot12MapResult;
        private AIAccessor _aiSlot13MapResult;
        private AIAccessor _aiSlot14MapResult;
        private AIAccessor _aiSlot15MapResult;
        private AIAccessor _aiSlot16MapResult;
        private AIAccessor _aiSlot17MapResult;
        private AIAccessor _aiSlot18MapResult;
        private AIAccessor _aiSlot19MapResult;
        private AIAccessor _aiSlot20MapResult;
        private AIAccessor _aiSlot21MapResult;
        private AIAccessor _aiSlot22MapResult;
        private AIAccessor _aiSlot23MapResult;
        private AIAccessor _aiSlot24MapResult;
        private AIAccessor _aiSlot25MapResult;

        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot1;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot1;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot2;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot2;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot3;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot3;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot4;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot4;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot5;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot5;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot6;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot6;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot7;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot7;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot8;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot8;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot9;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot9;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot10;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot10;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot11;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot11;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot12;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot12;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot13;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot13;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot14;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot14;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot15;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot15;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot16;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot16;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot17;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot17;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot18;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot18;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot19;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot19;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot20;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot20;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot21;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot21;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot22;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot22;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot23;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot23;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot24;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot24;
        private AIAccessor _aiWAFERPOSITIONUPPEREDGE_Slot25;
        private AIAccessor _aiWAFERPOSITIONLOWEREDGE_Slot25;
        private AIAccessor _aiErrorCode;
        private AIAccessor _aiLastErrorCode;




        private AIAccessor _aiTYPE1_CurrentRecipeNumber;
        private AIAccessor _aiTYPE1_MAPSTAR;
        private AIAccessor _aiTYPE1_MAPEND;
        private AIAccessor _aiTYPE1_SENSOR_WaferSize;
        private AIAccessor _aiTYPE1_SLOT;
        private AIAccessor _aiTYPE1_PITCH;
        private AIAccessor _aiTYPE1_POSITIONRANGE;
        private AIAccessor _aiTYPE1_POSITIONRANGEUPPER;
        private AIAccessor _aiTYPE1_POSITIONRANGELOWER;
        private AIAccessor _aiTYPE1_THICK;
        private AIAccessor _aiTYPE1_THICKRANGE;
        private AIAccessor _aiTYPE1_OFFSET;


        private AIAccessor _aiTYPE2_CurrentRecipeNumber;
        private AIAccessor _aiTYPE2_MAPSTAR;
        private AIAccessor _aiTYPE2_MAPEND;
        private AIAccessor _aiTYPE2_SENSOR_WaferSize;
        private AIAccessor _aiTYPE2_SLOT;
        private AIAccessor _aiTYPE2_PITCH;
        private AIAccessor _aiTYPE2_POSITIONRANGE;
        private AIAccessor _aiTYPE2_POSITIONRANGEUPPER;
        private AIAccessor _aiTYPE2_POSITIONRANGELOWER;
        private AIAccessor _aiTYPE2_THICK;
        private AIAccessor _aiTYPE2_THICKRANGE;
        private AIAccessor _aiTYPE2_OFFSET;


        private AIAccessor _aiTYPE3_CurrentRecipeNumber;
        private AIAccessor _aiTYPE3_MAPSTAR;
        private AIAccessor _aiTYPE3_MAPEND;
        private AIAccessor _aiTYPE3_SENSOR_WaferSize;
        private AIAccessor _aiTYPE3_SLOT;
        private AIAccessor _aiTYPE3_PITCH;
        private AIAccessor _aiTYPE3_POSITIONRANGE;
        private AIAccessor _aiTYPE3_POSITIONRANGEUPPER;
        private AIAccessor _aiTYPE3_POSITIONRANGELOWER;
        private AIAccessor _aiTYPE3_THICK;
        private AIAccessor _aiTYPE3_THICKRANGE;
        private AIAccessor _aiTYPE3_OFFSET;


        private AIAccessor _aiTYPE4_CurrentRecipeNumber;
        private AIAccessor _aiTYPE4_MAPSTAR;
        private AIAccessor _aiTYPE4_MAPEND;
        private AIAccessor _aiTYPE4_SENSOR_WaferSize;
        private AIAccessor _aiTYPE4_SLOT;
        private AIAccessor _aiTYPE4_PITCH;
        private AIAccessor _aiTYPE4_POSITIONRANGE;
        private AIAccessor _aiTYPE4_POSITIONRANGEUPPER;
        private AIAccessor _aiTYPE4_POSITIONRANGELOWER;
        private AIAccessor _aiTYPE4_THICK;
        private AIAccessor _aiTYPE4_THICKRANGE;
        private AIAccessor _aiTYPE4_OFFSET;

        private AIAccessor _aiTYPE5_CurrentRecipeNumber;
        private AIAccessor _aiTYPE5_MAPSTAR;
        private AIAccessor _aiTYPE5_MAPEND;
        private AIAccessor _aiTYPE5_SENSOR_WaferSize;
        private AIAccessor _aiTYPE5_SLOT;
        private AIAccessor _aiTYPE5_PITCH;
        private AIAccessor _aiTYPE5_POSITIONRANGE;
        private AIAccessor _aiTYPE5_POSITIONRANGEUPPER;
        private AIAccessor _aiTYPE5_POSITIONRANGELOWER;
        private AIAccessor _aiTYPE5_THICK;
        private AIAccessor _aiTYPE5_THICKRANGE;
        private AIAccessor _aiTYPE5_OFFSET;

        private AIAccessor _aiTYPE6_CurrentRecipeNumber;
        private AIAccessor _aiTYPE6_MAPSTAR;
        private AIAccessor _aiTYPE6_MAPEND;
        private AIAccessor _aiTYPE6_SENSOR_WaferSize;
        private AIAccessor _aiTYPE6_SLOT;
        private AIAccessor _aiTYPE6_PITCH;
        private AIAccessor _aiTYPE6_POSITIONRANGE;
        private AIAccessor _aiTYPE6_POSITIONRANGEUPPER;
        private AIAccessor _aiTYPE6_POSITIONRANGELOWER;
        private AIAccessor _aiTYPE6_THICK;
        private AIAccessor _aiTYPE6_THICKRANGE;
        private AIAccessor _aiTYPE6_OFFSET;

        private AIAccessor _aiRECIPE_TYPE_FEEDBACK;

       
        private DIAccessor _diAck_RecieSave;
        private DIAccessor _diAck_RecipeLoad;
        private DIAccessor _diAck_Init;
        private DIAccessor _diAck_Reset;
        private DIAccessor _diAck_SingleMap;


        private DIAccessor _diAck_HomePos;
        private DIAccessor _diSensor_Presence;
        private DIAccessor _diSensor_Placement;
        private DIAccessor _diSensor_Protrude;
        private DIAccessor _diSensor_VerticalDown;
        private DIAccessor _diAck_Load;
        private DIAccessor _diAck_Unload;
        private DIAccessor _diAck_MapDisable;
        private DIAccessor _diAck_MapEnable;
        private DIAccessor _diAck_ClampOn;
        private DIAccessor _diAck_ClampOff;
        private DIAccessor _diAck_DockFwd;
        private DIAccessor _diAck_DockHome;
        private DIAccessor _diAck_LatchOn;
        private DIAccessor _diAck_LatchOff;
        private DIAccessor _diAck_DoorClose;
        private DIAccessor _diAck_DoorOpen;
        private DIAccessor _diAck_VacOn;
        private DIAccessor _diAck_VacOff;
        private DIAccessor _diAck_VerticalDown;
        private DIAccessor _diAck_VerticalUp;
        private DIAccessor _diAck_ZtoMapStart;
        private DIAccessor _diAck_ZtoMapEnd;
        private DIAccessor _diAck_Mapin;
        private DIAccessor _diAck_MapOut;
        private DIAccessor _diAck_CmdClear;
        private DIAccessor _diAck_E84Disable;
        private DIAccessor _diAck_E84Enable;

        //原X,Y
        private DIAccessor _di12MapSensor;
        private DIAccessor _di8MapSensor;
        private DIAccessor _diSERVOHOME;
        private DIAccessor _diFoupPlacementSensor;
        private DIAccessor _diDoorCheckSensor;
        private DIAccessor _diCDASensor;
        private DIAccessor _diDockHomeSensor;
        private DIAccessor _diClampOffSensor;
        private DIAccessor _diPodForwardSensor;
        private DIAccessor _diClampOnSensor;
        private DIAccessor _diVACSensor;
        private DIAccessor _diLatchLockSensor;
        private DIAccessor _diLatchUnlockSensor;
        private DIAccessor _diDoorOpenSensor;
        private DIAccessor _diDoorCloseSensor;
        private DIAccessor _diVerticalMiddleSensor;
        private DIAccessor _diVerticalDownSensor;
        private DIAccessor _diOPSwitchButton;
        private DIAccessor _diMapin;
        private DIAccessor _diMapout;
        private DIAccessor _diProtrudesensor;
        private DIAccessor _diInfoPadA;
        private DIAccessor _diInfoPadB;
        private DIAccessor _diInfoPadC;
        private DIAccessor _diInfoPadD;
        private DIAccessor _diServoOn;
        private DIAccessor _diPresence;
        private DIAccessor _diStepError;
        private DIAccessor _diServoerror;
        private DIAccessor _diDoorSafe;
        private DIAccessor _diSERVO;
        private DIAccessor _diSTEP;






        private DIAccessor _diClampOnCylinder;
        private DIAccessor _diClampOffCylinder;
        private DIAccessor _diLatchOnCylinder;
        private DIAccessor _diLatchOffCylinder;
        private DIAccessor _diDoorOpenCylinder;
        private DIAccessor _diDoorCloseCylinder;
        private DIAccessor _diSERVOCLEAR1;
        private DIAccessor _diSERVOON1;
        private DIAccessor _diD3UBIN;
        private DIAccessor _diMapForwardCylinder;
        private DIAccessor _diSERVOCLEAR2;
        private DIAccessor _diSERVOON2;
        private DIAccessor _diSERVOCLCOUNT;
        private DIAccessor _diVAC;
        private DIAccessor _diSERVOBRAKE;

        private DIAccessor _diLoadLight;
        private DIAccessor _diUnloadLight;
        private DIAccessor _diPOWERLIGHT;
        private DIAccessor _diFoupPlacementLight;
        private DIAccessor _diFoupPresenceLight;
        private DIAccessor _diAlarmOccurLight;
        private DIAccessor _diStatu1Light;
        private DIAccessor _diStatu2Light;

        private IoSensor _diIronCassetteDoorClose;
        public IoSensor DiIronCassetteDoorClose 
        {
            get => _diIronCassetteDoorClose;
            set
            {
                _diIronCassetteDoorClose = value;
                _diIronCassetteDoorClose.OnSignalChanged += _diIronCassetteDoorClose_OnSignalChanged;
            } 
        }

        private void _diIronCassetteDoorClose_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if (!IsPlacement || !IsMapped) return;
            if (arg2) return;
            if (_diIronCassetteDoorClose.Value) return;
            if (!_diDockHomeSensor.Value) return;
            if (!IsReady()) return;
            if (!_isNeedCheckIronDoor) return;
            Unclamp(out _);
        }        

        private DateTime _actionStartTime;
        private int _actionTimeLimit;

        protected R_TRIG _carrierArrived;
        protected R_TRIG _errorOccurredTrig;
        protected R_TRIG _lastErrorOccurredTrig;

        protected R_TRIG _alarmWaferProtrudeTrig;

        private int _errorCode = 0;
        private int _lastErrorCode = 0;

        public override string ErrorCode 
        {
            get => _aiErrorCode.Value.ToString();
         }

        public TDKMode Mode { get; set; } = TDKMode.Online;
        public TDKInitPosMovement InitPosMovement { get; set; }
        public TDKOperationStatus OperationStatus { get; set; }
        public TDKContainerStatus ContainerStatus 
        {
            get 
            { 
                if (_isPlaced && _isPresent)
                    return TDKContainerStatus.NormalMount;
                if (!_isPlaced && !_isPresent)
                    return TDKContainerStatus.Absence;
                return TDKContainerStatus.MountError;
            }
        }
        public TDKPosition ClampPosition
        {
            get
            {
                if (_diClampOffSensor.Value && !_diClampOnCylinder.Value)
                    return TDKPosition.Open;
                if (!_diClampOffSensor.Value && _diClampOnCylinder.Value)
                    return TDKPosition.Close;
                return TDKPosition.TBD;
            }
        }
        public TDKPosition LPDoorLatchPosition
        {
            get
            {
                if (_diLatchLockSensor.Value && !_diLatchUnlockSensor.Value)
                    return TDKPosition.Close;
                if (!_diLatchLockSensor.Value && _diLatchUnlockSensor.Value)
                    return TDKPosition.Open;
                return TDKPosition.TBD;
            }
        }
        public TDKVacummStatus VacuumStatus 
        {
            get
            {
                if (_diVACSensor.Value)
                    return TDKVacummStatus.ON;
                return TDKVacummStatus.OFF;
            }
        }
        public TDKPosition LPDoorState 
        {
            get
            {
                if (_diDoorOpenSensor.Value && !_diDoorCloseSensor.Value)
                    return TDKPosition.Open;
                if(!_diDoorOpenSensor.Value && _diDoorCloseSensor.Value)
                    return TDKPosition.Close;
                return TDKPosition.TBD;
            }
        }

        

        public TDKWaferProtrusion WaferProtrusion 
        {
            get
            {
                if (!_diProtrudesensor.Value)
                    return TDKWaferProtrusion.ShadingStatus;
                return TDKWaferProtrusion.LightIncidentStatus;
            }
        }
        public TDKElevatorAxisPosition ElevatorAxisPosition 
        { 
            get
            {
                if (_diVerticalDownSensor.Value)
                    return TDKElevatorAxisPosition.Down;
                if (_diAck_ZtoMapEnd.Value)
                    return TDKElevatorAxisPosition.MappingStartPos;
                if (_diAck_ZtoMapStart.Value)
                    return TDKElevatorAxisPosition.MappingEndPos;
                return TDKElevatorAxisPosition.TBD;
            }
        }
        public TDKDockPosition DockPosition 
        { 
            get
            {
                if (_diDockHomeSensor.Value && !_diPodForwardSensor.Value)
                    return TDKDockPosition.Undock;
                if (!_diDockHomeSensor.Value && _diPodForwardSensor.Value)
                    return TDKDockPosition.Dock;
                return TDKDockPosition.TBD;
            }                
        }
        public TDKMapPosition MapperPostion
        {
            get => TDKMapPosition.TBD;
         }
        public TDKMappingStatus MappingStatus { get; set; } = TDKMappingStatus.NormalEnd;

        public TDKModel Model { get; set; }

        public CarrierMode LPCarrierMode
        {
            get
            {
                int intvalue = 0;

                if (SC.ContainsItem($"CarrierInfo.CarrierFosbMode{InfoPadCarrierIndex}"))
                    intvalue = SC.GetValue<int>($"CarrierInfo.CarrierFosbMode{InfoPadCarrierIndex}");
                if(SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierFosbMode"))
                {
                    bool bvalue = SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierFosbMode");
                    return bvalue ? CarrierMode.Fosb : CarrierMode.Foup;
                }

                if (intvalue == 0)
                    return CarrierMode.Foup;
                if (intvalue == 1)
                    return CarrierMode.Fosb;
                if (intvalue == 2)
                    return CarrierMode.OpenCassette;
               
                return CarrierMode.Foup;
            }
        }
        public override bool IsWaferProtrude
        { 
            get 
            {
                //_alarmWaferProtrudeTrig.CLK = !_diProtrudesensor.Value;
                //if(_alarmWaferProtrudeTrig.Q)
                //{
                //    EV.Notify(AlarmLoadPortWaferProtrusion);
                //}
                return (!_diProtrudesensor.Value);
            }
            
        }

        public override bool IsEnableLoad(out string reason)
        {
            //if(IsWaferProtrude)
            //{
            //    reason = "Wafer Protrusion";
            //    return false;
            //}
            if(_isNeedCheckIronDoor)
            {
                if(DiIronCassetteDoorClose!=null && !DiIronCassetteDoorClose.Value)
                {
                    reason = "Iron Door Closed";
                    return false;
                }                
            }

            return base.IsEnableLoad(out reason);
        }
        public override bool IsEnableMapWafer(out string reason)
        {
            if (IsWaferProtrude)
            {
                reason = "Wafer Protrusion";
                return false;
            }
            if (_isNeedCheckIronDoor)
            {
                if (DiIronCassetteDoorClose != null && !DiIronCassetteDoorClose.Value)
                {
                    reason = "Iron Door Closed";
                    return false;
                }
            }

            return base.IsEnableMapWafer(out reason);
        }

        private bool _isNeedCheckIronDoor
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.NeedCheckIronDoorCarrier{InfoPadCarrierIndex}"))
                    return SC.GetValue<bool>($"CarrierInfo.NeedCheckIronDoorCarrier{InfoPadCarrierIndex}");

                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.NeedCheckIronDoorCarrier"))
                    return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.NeedCheckIronDoorCarrier");

                return false;

            }
        }
        public override bool IsEnableTransferWafer(out string reason)
        {  
            if(!IsLoaded)
            {
                reason = "Carrier Is not loaded";
                return false;
            }
            if(!IsMapped)
            {
                reason = "Not Mapped";
                return false;
            }
            if(!IsReady())
            {
                reason = "Not Ready";
                return false;
            }

            if (IsWaferProtrude)
            {
                EV.Notify(AlarmLoadPortWaferProtrusion);
                OnError("Wafer Protrusion");
                reason = "Wafer Protrusion";
                return false;
            }           

            if (_isNeedCheckIronDoor)
            {
                if (DiIronCassetteDoorClose != null && !DiIronCassetteDoorClose.Value)
                {
                    reason = "Iron Door Closed";
                    return false;
                }
            }
            if(ErrorCode !="0")
            {
                reason = "ErrorCode is " + ErrorCode;
                return false;
            }
            if (_isDisableEvenSlot)
            {
                for(int i =0;i<SlotMap.ToArray().Length;i++)
                {
                    if (i % 2 == 0) continue;
                    if (SlotMap[i] != '0')
                    {
                        OnError("Detect wafer on even slot.");
                        reason = "Detect wafer on even slot";
                        return false;
                    }
                }                
            }
            if (IsVerifyPreDefineWaferCount && WaferCount != PreDefineWaferCount)
            {
                reason = "Mapping Error:WaferCount not matched";
                return false;
            }

            return base.IsEnableTransferWafer(out reason);
        }
       

        public override FoupClampState ClampState
        {
            get
            {
                if (_diClampOffSensor.Value && !_diClampOnCylinder.Value)
                    return FoupClampState.Open;
                if (!_diClampOffSensor.Value && _diClampOnCylinder.Value)
                    return FoupClampState.Close;
                return FoupClampState.Unknown;
            }
        }
        public override FoupDockState DockState
        {
            get
            {
                if(_diDockHomeSensor.Value && !_diPodForwardSensor.Value)
                    return FoupDockState.Undocked;
                if(!_diDockHomeSensor.Value && _diPodForwardSensor.Value)
                    return FoupDockState.Docked;
                return FoupDockState.Unknown;
            }
        }

        public override LoadportCassetteState CassetteState
        {
            get
            {
                if (_isPlaced && _isPresent)
                    return LoadportCassetteState.Normal;
                if (!_isPlaced)
                    return LoadportCassetteState.Absent;
                return LoadportCassetteState.Unknown;
            }
        }
        public override FoupDoorState DoorState
        {
            get
            {
                if (LPDoorState == TDKPosition.Close)
                    return FoupDoorState.Close;
                if (LPDoorState == TDKPosition.Open)
                    return FoupDoorState.Open;
                return FoupDoorState.Unknown;
            }
        }

        public override FoupDoorPostionEnum DoorPosition 
        { 
            get
            {
                if (_diVerticalDownSensor.Value && !_diVerticalMiddleSensor.Value)
                    return FoupDoorPostionEnum.Down;
                if (!_diAck_ZtoMapEnd.Value && !_diVerticalMiddleSensor.Value)
                    return FoupDoorPostionEnum.Unknown;
                return FoupDoorPostionEnum.Unknown;
            }
        }


        public override bool IsVacuumON
        {
            get
            {
                return _diVACSensor.Value;
            }
        }

        public override WaferSize GetCurrentWaferSize()
        {
            int intvalue = 0;

            if(SC.ContainsItem($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}"))
                intvalue = SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}");
            if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierWaferSize"))
                intvalue = SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierWaferSize");
            if (intvalue == 0)
                return WaferSize.WS0;
            if (intvalue == 6)
                return WaferSize.WS6;
            if (intvalue == 8)
                return WaferSize.WS8;
            if (intvalue == 12)
                return WaferSize.WS12;
            return WaferSize.WS0;
            
            
        }
        public override int InfoPadCarrierIndex 
        {
            get
            {
                if (IsAutoDetectCarrierType)
                {

                    return (_diInfoPadA.Value ? 8 : 0) + (_diInfoPadB.Value ? 4 : 0) +
                        (_diInfoPadC.Value ? 2 : 0) + (_diInfoPadD.Value ? 1 : 0);
                }
                return base.InfoPadCarrierIndex;
            }
            set
            {
                base.InfoPadCarrierIndex = value;
            }
        }

        public override bool IsVerifyPreDefineWaferCount
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{LPModuleName}.IsVerifyPreDefineWaferCount"))
                    return SC.GetValue<bool>($"LoadPort.{LPModuleName}.IsVerifyPreDefineWaferCount");
                return false;
            }
        }

        public override string SpecCarrierType 
        {
            get 
            {
                if (!_isPlaced) return "";
                if (SC.ContainsItem($"CarrierInfo.CarrierName{InfoPadCarrierIndex}"))
                    return SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadCarrierIndex}");
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierName"))
                    return SC.GetStringValue($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierName");
                return "";
            }
        }
        private bool OnTimer()
        {
            try
            {
                _errorOccurredTrig.CLK = (int)_aiErrorCode.Value != _errorCode;
                _errorCode = (int)_aiErrorCode.Value;

                _lastErrorOccurredTrig.CLK = (int)_aiLastErrorCode.Value != _lastErrorCode;
                _lastErrorCode = (int)_aiLastErrorCode.Value;
               

                if(_errorOccurredTrig.Q && _errorCode >0)
                {
                    if (AlarmNcdLoadPortErrors.Length >= _errorCode - 1000)
                    {
                        EV.Notify(AlarmNcdLoadPortErrors[_errorCode - 1001]);
                        OnError(AlarmNcdLoadPortErrors[_errorCode - 1001]);
                    }
                }
                if(_lastErrorOccurredTrig.Q && _lastErrorCode>0)
                {
                    if (AlarmNcdLoadPortLastErrors.Length > _lastErrorCode - 1000)
                    {
                        EV.Notify(AlarmNcdLoadPortLastErrors[_lastErrorCode - 1001]);
                        OnError(AlarmNcdLoadPortLastErrors[_errorCode - 1001]);
                    }
                }




                _isPresent = _diSensor_Presence.Value;
                _isPlaced = _diSensor_Placement.Value;
                SetPresent(_isPresent);

                SetPlaced(_diSensor_Placement.Value);   

                _carrierArrived.CLK = _diSensor_Placement.Value;                

              

                if (_carrierArrived.Q)
                {
                    CheckAndLoadRecipe();
                }


                MonitorInterlock();





            }
            catch(Exception ex)
            {
                LOG.Write(ex);
            }


            return true;
        }

        private void MonitorInterlock()
        {
            if ( CurrentState ==  LoadPortStateEnum.Error) return;                
            if(DockState == FoupDockState.Docked)
            {
                if (!_isPlaced) 
                    OnError("Carrier removed unexpectly.");
            }
        }

        private int _intRecipeNum
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.CarrierRecipeNumber{InfoPadCarrierIndex}"))
                    return SC.GetValue<int>($"CarrierInfo.CarrierRecipeNumber{InfoPadCarrierIndex}");
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.LPRecipeNumber"))
                    return SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.LPRecipeNumber");
                return 1;
            }
        }

        private bool CheckAndLoadRecipe()
        { 
            
            int timelimit = SC.ContainsItem($"LoadPort.{Name}.TimeLimitAction") ?
                SC.GetValue<int>($"LoadPort.{Name}.TimeLimitAction") : 45;

            if (_aiRECIPE_TYPE_FEEDBACK.Value == _intRecipeNum)
                return true;

            if (_doRecipeLoadCommand.Value)
            {
                _doRecipeLoadCommand.SetValue(false, out _);
                Thread.Sleep(1000);
            }
            _aoSAVECurrentRecipeNumber.Value = (short)_intRecipeNum;
            Thread.Sleep(200);
            _doRecipeLoadCommand.SetValue(true, out _);
            DateTime _stTime = DateTime.Now;
            while(!_diAck_RecipeLoad.Value || _aiRECIPE_TYPE_FEEDBACK.Value != _intRecipeNum)
            {
                if (DateTime.Now - _stTime > TimeSpan.FromSeconds(timelimit))
                {                    
                    _doRecipeLoadCommand.SetValue(false, out _);
                    EV.Notify($"{LPModuleName}LoadRecipeFailed");
                    OnError($"{LPModuleName}LoadRecipeFailed");
                    return false;
                }
            }
            _doRecipeLoadCommand.SetValue(false, out _);
            _stTime = DateTime.Now;
            while (_diAck_RecipeLoad.Value)
            {
                if (DateTime.Now - _stTime > TimeSpan.FromSeconds(timelimit))
                {
                    _doRecipeLoadCommand.SetValue(false, out _);
                    EV.Notify($"{LPModuleName}LoadRecipeFailed");
                    OnError($"{LPModuleName}LoadRecipeFailed");
                    return false;
                }
            }
            return true;

        }



        protected override bool fStartInit(object[] param)
        {
            ResetRoutine();
            _actionTimeLimit = SC.ContainsItem($"LoadPort.{Name}.TimeLimitLoadportHome") ?
                SC.GetValue<int>($"LoadPort.{Name}.TimeLimitLoadportHome") : 45;

            //if(!_diProtrudesensor.Value)
            //{
            //    EV.Notify(AlarmLoadPortWaferProtrusion);
            //    return false;
            //}
            return true;


            //_actionStartTime = DateTime.Now;



            //if (!CheckAndLoadRecipe())
            //    return false;
            //return _doIntialCommand.SetValue(true, out _);

        }

        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;            

            try
            {
                SetAoValue((int)LoadPortStepEnum.LoadRecipe1, _aoSAVECurrentRecipeNumber, (short)_intRecipeNum, Notify);
                SetDoState((int)LoadPortStepEnum.LoadRecipe2, _doRecipeLoadCommand, true, Notify);
                WaitDiState((int)LoadPortStepEnum.LoadRecipe3, _actionTimeLimit, _diAck_RecipeLoad, true, Notify, Stop);
                WaitAiValue((int)LoadPortStepEnum.LoadRecipe4, _actionTimeLimit, _aiRECIPE_TYPE_FEEDBACK, (short)_intRecipeNum, Notify, Stop);
                SetDoState((int)LoadPortStepEnum.LoadRecipe5, _doRecipeLoadCommand, false, Notify);
                WaitDiState((int)LoadPortStepEnum.LoadRecipe6, _actionTimeLimit, _diAck_RecipeLoad, false, Notify, Stop);

                SetDoState((int)LoadPortStepEnum.ActionStep1, _doIntialCommand, true, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep2, _actionTimeLimit, _diAck_Init, true, Notify, Stop);
                SetDoState((int)LoadPortStepEnum.ActionStep3, _doIntialCommand, false, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep4, _actionTimeLimit, _diAck_Init, false, Notify, Stop);



            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.Notify(AlarmLoadPortHomeTimeout);
                OnError("Home failed");
                return true;
            }
            OnHomed();
            return true;



        }


        

        protected override bool fStartLoad(object[] param)
        {
            _actionTimeLimit = SC.ContainsItem($"LoadPort.{Name}.TimeLimitLoadportLoad") ?
                SC.GetValue<int>($"LoadPort.{Name}.TimeLimitLoadportLoad") : 45;

            ResetRoutine();
            if (param == null || param.Length == 0)
            {
                _doMapEnableCommand.SetValue(true, out _);
                _doMapDisableCommand.SetValue(false, out _);
                _currentloadCommand = "LoadWithMap";
            }
            if (param.Length >= 1 && param[0].ToString() == "LoadWithMap")
            {
                _doMapEnableCommand.SetValue(true, out _);
                _doMapDisableCommand.SetValue(false, out _);
                _currentloadCommand = "LoadWithMap";

            }
            if (param.Length >= 1 && param[0].ToString() == "LoadWithoutMap")
            {
                _doMapEnableCommand.SetValue(false, out _);
                _doMapDisableCommand.SetValue(true, out _);
                _currentloadCommand = "LoadWithoutMap";
            }
            if (param.Length >= 1 && param[0].ToString() == "LoadWithCloseDoor")
            {
                _doMapEnableCommand.SetValue(true, out _);
                _doMapDisableCommand.SetValue(false, out _);
                _currentloadCommand = "LoadWithCloseDoor";
            }
            if (param.Length >= 1 && param[0].ToString() == "LoadWithoutMapWithCloseDoor")
            {
                _doMapEnableCommand.SetValue(false, out _);
                _doMapDisableCommand.SetValue(true, out _);
                _currentloadCommand = "LoadWithoutMapWithCloseDoor";
            }
            _isNeedLoadRecipe = _aiRECIPE_TYPE_FEEDBACK.Value != (short)_intRecipeNum;
            return true;
        }
        private string _currentloadCommand = "";
        private bool _isNeedLoadRecipe;
        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;

            try
            {
                


                if (_isNeedLoadRecipe)
                {
                    SetAoValue((int)LoadPortStepEnum.LoadRecipe1, _aoSAVECurrentRecipeNumber, (short)_intRecipeNum, Notify);
                    SetDoState((int)LoadPortStepEnum.LoadRecipe2, _doRecipeLoadCommand, true, Notify);
                    WaitDiState((int)LoadPortStepEnum.LoadRecipe3, _actionTimeLimit, _diAck_RecipeLoad, true, Notify, Stop);
                    WaitAiValue((int)LoadPortStepEnum.LoadRecipe4, _actionTimeLimit, _aiRECIPE_TYPE_FEEDBACK, (short)_intRecipeNum, Notify, Stop);
                    SetDoState((int)LoadPortStepEnum.LoadRecipe5, _doRecipeLoadCommand, false, Notify);
                    WaitDiState((int)LoadPortStepEnum.LoadRecipe6, _actionTimeLimit, _diAck_RecipeLoad, false, Notify, Stop);
                }

                SetDoState((int)LoadPortStepEnum.ActionStep1, _doLoadCommand, true, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep2, _actionTimeLimit, _diAck_Load, true, Notify, Stop);
                SetDoState((int)LoadPortStepEnum.ActionStep3, _doLoadCommand, false, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep4, _actionTimeLimit, _diAck_Load, false, Notify, Stop);

                if(_currentloadCommand == "LoadWithCloseDoor")
                {
                    SetDoState((int)LoadPortStepEnum.ActionStep5, _doVertiaclUpCommand, true, Notify);
                    WaitDiState((int)LoadPortStepEnum.ActionStep6, _actionTimeLimit, _diAck_VerticalUp, true, Notify, Stop);
                    SetDoState((int)LoadPortStepEnum.ActionStep7, _doVertiaclUpCommand, false, Notify);
                    WaitDiState((int)LoadPortStepEnum.ActionStep8, _actionTimeLimit, _diAck_VerticalUp, false, Notify, Stop);

                    SetDoState((int)LoadPortStepEnum.ActionStep9, _doVertiaclUpCommand, true, Notify);
                    WaitDiState((int)LoadPortStepEnum.ActionStep10, _actionTimeLimit, _diAck_VerticalUp, true, Notify, Stop);
                    SetDoState((int)LoadPortStepEnum.ActionStep11, _doVertiaclUpCommand, false, Notify);
                    WaitDiState((int)LoadPortStepEnum.ActionStep12, _actionTimeLimit, _diAck_VerticalUp, false, Notify, Stop);

                    SetDoState((int)LoadPortStepEnum.ActionStep13, _doDoorCloseCommand, true, Notify);
                    WaitDiState((int)LoadPortStepEnum.ActionStep14, _actionTimeLimit, _diAck_DoorClose, true, Notify, Stop);
                    SetDoState((int)LoadPortStepEnum.ActionStep15, _doDoorCloseCommand, false, Notify);
                    WaitDiState((int)LoadPortStepEnum.ActionStep16, _actionTimeLimit, _diAck_DoorClose, false, Notify, Stop);

                }
            }

            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.Notify(AlarmLoadPortLoadTimeOut);
                OnError("LoadFailed");
                return true;
            }
            OnLoaded();
            if(_currentloadCommand == "LoadWithMap" || _currentloadCommand == "LoadWithCloseDoor")
            {
                ReadSlotMap();
            }
            return true;











            //if (DateTime.Now - _actionStartTime > TimeSpan.FromSeconds(timelimit))
            //{
            //    _doLoadCommand.SetValue(false, out _);
            //    OnError("Load timeout");
            //    return false;
            //}
           


            //if (_diAck_Load.Value && _diPodForwardSensor.Value && _diDoorOpenSensor.Value && _diVerticalDownSensor.Value)
            //{
            //    _doLoadCommand.SetValue(false, out _);
            //    switch (_currentloadCommand)
            //    {
            //        case "LoadWithMap":
            //            //if (!IsMapped)
            //                ReadSlotMap();
            //            return true;
            //        case "LoadWithCloseDoor":
            //            if (!_diAck_VerticalUp.Value || _diVerticalDownSensor.Value)
            //            {
            //                _doVertiaclUpCommand.SetValue(true, out _);
            //                _doVertiaclDownCommand.SetValue(false, out _);
            //            }
            //            if(_diAck_VerticalUp.Value && !_diVerticalDownSensor.Value)
            //            {
            //                _doVertiaclUpCommand.SetValue(false, out _);
            //                _doVertiaclDownCommand.SetValue(false, out _);

            //                _doDoorOpenCommand.SetValue(false, out _);
            //                _doDoorCloseCommand.SetValue(true, out _);
            //            }

            //            if(!_diVerticalDownSensor.Value && _diAck_DoorClose.Value && _diDoorCloseSensor.Value)
            //            {
            //                _doDoorOpenCommand.SetValue(false, out _);
            //                _doDoorCloseCommand.SetValue(false, out _);
            //                ReadSlotMap();
            //                return true;
            //            }
            //            break;
            //        default:
            //            break;
            //    }
            //    _doLoadCommand.SetValue(false, out _);
            //    return true;
            //}
            //return base.fMonitorLoad(param);
        }

        protected override bool fMonitorTransferBlock(object[] param)
        {
            if (!_isPlaced)
            {
                OnError("Placement Error");
                return false;
            }
            return base.fMonitorTransferBlock(param);
        }

        public override int ValidStartSlotIndex => base.ValidStartSlotIndex;
        public override int ValidEndSlotIndex
        {
            get
            {
                return ValidStartSlotIndex + ValidSlotsNumber - 1;
            }
        }
        private void ReadSlotMap()
        {
            string strslotmap = "";
            AIAccessor[] ais = new AIAccessor[]
            {
               _aiSlot25MapResult,_aiSlot24MapResult,_aiSlot23MapResult,_aiSlot22MapResult ,_aiSlot21MapResult,
               _aiSlot20MapResult,_aiSlot19MapResult, _aiSlot18MapResult, _aiSlot17MapResult ,_aiSlot16MapResult,
               _aiSlot15MapResult,_aiSlot14MapResult,_aiSlot13MapResult,_aiSlot12MapResult ,_aiSlot11MapResult,
               _aiSlot10MapResult,_aiSlot9MapResult,_aiSlot8MapResult,_aiSlot7MapResult ,_aiSlot6MapResult,
               _aiSlot5MapResult,_aiSlot4MapResult,_aiSlot3MapResult,_aiSlot2MapResult,_aiSlot1MapResult,
            };
            WaferCount = 0;
            for(int i= 24 - ValidEndSlotIndex; i<=24;i++)
            {                
                if (ais[i].Value != 0)
                {
                    WaferCount++;
                    if (_isDisableEvenSlot && i % 2 == 1)
                    {
                        strslotmap = strslotmap + "?";
                        OnError("Detect wafer on even slot;");
                        continue;
                    }                  

                }
                strslotmap = strslotmap + ais[i].Value.ToString("D1");
            }

            if(IsVerifyPreDefineWaferCount)
            {
                if(WaferCount != PreDefineWaferCount)
                {
                    EV.PostAlarmLog("LoadPort", $"{LPModuleName} mapping error,predefine count is {PreDefineWaferCount}, " +
                        $"Mapping result is {WaferCount}.");
                    OnError("Mapping Error");
                }
            }
            OnSlotMapRead(strslotmap);
        }
        private bool _isDisableEvenSlot
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.DisableEvenSlot{InfoPadCarrierIndex}")) 
                    return SC.GetValue<bool>($"CarrierInfo.DisableEvenSlot{InfoPadCarrierIndex}");
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.DisableEvenSlot"))
                    return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.DisableEvenSlot");
                return false;
            }
        }

        protected override bool fStartRead(object[] param)
        {
            return true;
        }

        protected override bool fStartReset(object[] param)
        {
            if(!_doResetCommand.Value)
            {
                _doResetCommand.SetValue(true, out _);
            }
            else
            {
                _doResetCommand.SetValue(false, out _);
                Thread.Sleep(1000);
                _doResetCommand.SetValue(true, out _);                
            }
            _doLoadCommand.SetValue(false, out _);
            _doUnloadCommand.SetValue(false, out _);
            _actionStartTime = DateTime.Now;
            return true;
        }


        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if(DateTime.Now - _actionStartTime>TimeSpan.FromSeconds(10))
            {
                OnError("Reset Timeout");
                _doResetCommand.SetValue(false, out _);
                return false;
            }

            if(_diAck_Reset.Value)
            {
                _doResetCommand.SetValue(false, out _);
                _errorCode = 0;
                _lastErrorCode = 0;
                return true;
            }
            return false;

        }
        
        protected override bool fStartUnload(object[] param)
        {
            _actionTimeLimit = SC.ContainsItem($"LoadPort.{Name}.TimeLimitLoadportUnload") ?
                SC.GetValue<int>($"LoadPort.{Name}.TimeLimitLoadportUnload") : 45;

            ResetRoutine();

            _isNeedLoadRecipe = _aiRECIPE_TYPE_FEEDBACK.Value !=(short)_intRecipeNum;
            return true;
        }

        public override int ValidSlotsNumber
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.CarrierSlotsNumber{InfoPadCarrierIndex}"))
                    return SC.GetValue<int>($"CarrierInfo.CarrierSlotsNumber{InfoPadCarrierIndex}");
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierSlotsNumber"))
                    return SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierSlotsNumber");
                return 25;
            }
        }
        private bool _isKeepClampAfterUnload
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.KeepClampedAfterUnloadCarrier{InfoPadCarrierIndex}"))
                    return SC.GetValue<bool>($"CarrierInfo.KeepClampedAfterUnloadCarrier{InfoPadCarrierIndex}");
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.KeepClampedAfterUnloadCarrier"))
                    return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.KeepClampedAfterUnloadCarrier");
                return false;
            }
            
        }

        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
            try
            {                
                if (_isNeedLoadRecipe)
                {
                    SetAoValue((int)LoadPortStepEnum.LoadRecipe1, _aoSAVECurrentRecipeNumber, (short)_intRecipeNum, Notify);
                    SetDoState((int)LoadPortStepEnum.LoadRecipe2, _doRecipeLoadCommand, true, Notify);
                    WaitDiState((int)LoadPortStepEnum.LoadRecipe3, _actionTimeLimit, _diAck_RecipeLoad, true, Notify, Stop);
                    WaitAiValue((int)LoadPortStepEnum.LoadRecipe4, _actionTimeLimit, _aiRECIPE_TYPE_FEEDBACK, (short)_intRecipeNum, Notify, Stop);
                    SetDoState((int)LoadPortStepEnum.LoadRecipe5, _doRecipeLoadCommand, false, Notify);
                    WaitDiState((int)LoadPortStepEnum.LoadRecipe6, _actionTimeLimit, _diAck_RecipeLoad, false, Notify, Stop);
                }
                SetDoState((int)LoadPortStepEnum.ActionStep1, _doUnloadCommand, true, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep2, _actionTimeLimit, _diAck_Unload, true, Notify, Stop);
                SetDoState((int)LoadPortStepEnum.ActionStep3, _doUnloadCommand, false, Notify);
                WaitDiState((int)LoadPortStepEnum.ActionStep4, _actionTimeLimit, _diAck_Unload, false, Notify, Stop);      
                
                if(_isKeepClampAfterUnload)
                {
                    SetDoState((int)LoadPortStepEnum.ActionStep5, _doClampOnCommand, true, Notify);
                    WaitDiState((int)LoadPortStepEnum.ActionStep6, _actionTimeLimit, _diAck_ClampOn, true, Notify, Stop);
                    SetDoState((int)LoadPortStepEnum.ActionStep7, _doClampOnCommand, false, Notify);
                    WaitDiState((int)LoadPortStepEnum.ActionStep8, _actionTimeLimit, _diAck_ClampOn, false, Notify, Stop);
                }
                else
                {
                    base.OnUnloaded();
                }
               

            }

            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                EV.Notify(AlarmLoadPortUnloadTimeOut);
                OnError("UnloadFailed");
                return true;
            }
         
            return true;



        }

        protected override bool fStartWrite(object[] param)
        {
            return true;
        }

        protected override bool fStartExecute(object[] param)
        {
            if (!CheckAndLoadRecipe())
                return false;
            _actionStartTime = DateTime.Now;
            try
            {
                switch (param[0].ToString())
                {
                    case "SetIndicator":
                        _currentExecuteCommand = "SetIndicator";
                        Indicator light = (Indicator)param[1];
                        IndicatorState state = (IndicatorState)param[2];
                        switch(light)
                        {
                            case Indicator.LOAD:
                                if (state == IndicatorState.BLINK || state == IndicatorState.ON)
                                    _doLoadLight.SetValue(true, out _);
                                if (state == IndicatorState.OFF)
                                    _doLoadLight.SetValue(false, out _);
                                break;
                            case Indicator.UNLOAD:
                                if (state == IndicatorState.BLINK || state == IndicatorState.ON)
                                    _doUnloadLight.SetValue(true, out _);
                                if (state == IndicatorState.OFF)
                                    _doUnloadLight.SetValue(false, out _);
                                break;

                            case Indicator.ACCESSAUTO:
                                if (state == IndicatorState.BLINK || state == IndicatorState.ON)
                                    _doStatu1Light.SetValue(true, out _);
                                if (state == IndicatorState.OFF)
                                    _doStatu1Light.SetValue(false, out _);
                                break;
                            case Indicator.ACCESSMANUL:
                                if (state == IndicatorState.BLINK || state == IndicatorState.ON)
                                    _doStatu2Light.SetValue(true, out _);
                                if (state == IndicatorState.OFF)
                                    _doStatu2Light.SetValue(false, out _);
                                break;
                        }
                        break;

                    case "QueryIndicator":
                        break;
                    case "QueryState":
                        _currentExecuteCommand = "QueryState";
                        break;
                    case "Undock":
                        _currentExecuteCommand = "Undock";
                        _doDOCKHomeCommand.SetValue(true, out _);
                        _doDOCKForwardCommand.SetValue(false, out _);                        
                        break;
                    case "Dock":
                        _currentExecuteCommand = "Dock";
                        _doDOCKHomeCommand.SetValue(false, out _);
                        _doDOCKForwardCommand.SetValue(true, out _);
                        break;
                    case "CloseDoor":
                        _currentExecuteCommand = "CloseDoor";
                        _doDoorOpenCommand.SetValue(false, out _);
                        _doDoorCloseCommand.SetValue(true, out _);
                        break;
                    case "OpenDoor":
                        _currentExecuteCommand = "CloseDoor";
                        _doDoorOpenCommand.SetValue(true, out _);
                        _doDoorCloseCommand.SetValue(false, out _);
                        break;
                    case "Unclamp":
                        _currentExecuteCommand = "Unclamp";
                        _doClampOffCommand.SetValue(true, out _);
                        _doClampOnCommand.SetValue(false, out _);
                        break;
                    case "Clamp":
                        _currentExecuteCommand = "Clamp";
                        _doClampOffCommand.SetValue(false, out _);
                        _doClampOnCommand.SetValue(true, out _);
                        break;
                    case "DoorUp":
                        _currentExecuteCommand = "DoorUp";
                        _doVertiaclDownCommand.SetValue(false, out _);
                        _doVertiaclUpCommand.SetValue(true, out _);
                        break;
                    case "DoorDown":
                        _currentExecuteCommand = "DoorDown";
                        _doVertiaclDownCommand.SetValue(true, out _);
                        _doVertiaclUpCommand.SetValue(false, out _);
                        break;
                        
                    case "MapWafer":
                        //_currentExecuteCommand = "MapWafer";
                        //_doZtoMAPPINGSTARTPOSI.SetValue(true, out _);
                        break;
                    case "DoorUpAndClose":
                        _currentExecuteCommand = "DoorUpAndClose";
                        _doVertiaclUpCommand.SetValue(true, out _);
                        break;

                    case "OpenDoorAndDown":
                        _currentExecuteCommand = "DoorUpAndClose";
                        _doDoorOpenCommand.SetValue(true, out _);
                        break;
                        
                    case "Move":
                        HandlerMoveCommand(param[1].ToString());
                        break;
                    case "Set":
                        
                        break;
                    case "Get":
                       
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostAlarmLog(Name, $"Parameter invalid");
                return false;

            }
        }

        private void HandlerMoveCommand(string movecommand)
        {
            
            switch(movecommand)
            {
                case "PODCL":
                    _currentExecuteCommand = "Clamp";
                    _doClampOffCommand.SetValue(false, out _);
                    _doClampOnCommand.SetValue(true, out _);
                    break;
                case "YDOOR":
                    _currentExecuteCommand = "Dock";
                    _doDOCKHomeCommand.SetValue(false, out _);
                    _doDOCKForwardCommand.SetValue(true, out _);
                    break;
                case "VACON":
                    _currentExecuteCommand = "VacuumOn";
                    _doVACOnCommand.SetValue(true, out _);
                    _doVACOffCommand.SetValue(false, out _);
                    break;
                case "DOROP":
                    _currentExecuteCommand = "UnlatchDoor";
                    _doLatchOffCommand.SetValue(true, out _);
                    _doLatchOnCommand.SetValue(false, out _);
                    break;
                case "ZMPST":
                    _currentExecuteCommand = "MoveToMapStart";
                    _doZtoMAPPINGSTARTPOSI.SetValue(true, out _);
                    _doZtoMAPPINGENDPOSI.SetValue(false, out _);
                    break;
                case "MAPOP":
                    _currentExecuteCommand = "MoveToMapMeasurement";
                    
                    break;
                case "ZDRMP":
                    _currentExecuteCommand = "MoveToMapEnd";
                    _doZtoMAPPINGSTARTPOSI.SetValue(false, out _);
                    _doZtoMAPPINGENDPOSI.SetValue(true, out _);
                    break;
                case "MAPCL":
                    _currentExecuteCommand = "MoveToMapWait";
                    break;
                case "ZDRDW":
                    _currentExecuteCommand = "MoveToLoadPosition";

                    break;
                case "ZDRUP":
                    _currentExecuteCommand = "MoveToDoorOpenClosePostion";
                    _doVertiaclUpCommand.SetValue(true, out _);
                    _doVertiaclDownCommand.SetValue(false, out _);
                    break;
                case "DORFW":
                    _currentExecuteCommand = "CloseDoor";
                    _doDoorCloseCommand.SetValue(true, out _);
                    _doDoorOpenCommand.SetValue(false, out _);
                    break;
                case "DORCL":
                    _currentExecuteCommand = "LatchDoor";
                    _doLatchOnCommand.SetValue(true, out _);
                    _doLatchOffCommand.SetValue(false, out _);
                    break;
                case "VACOF":
                    _currentExecuteCommand = "VacuumOff";
                    _doVACOffCommand.SetValue(true, out _);
                    _doVACOnCommand.SetValue(false, out _);
                    break;
                case "YWAIT":
                    _currentExecuteCommand = "Undock";
                    _doDOCKHomeCommand.SetValue(true, out _);
                    _doDOCKForwardCommand.SetValue(false, out _);
                    break;
                case "PODOP":
                    _currentExecuteCommand = "Unclamp";
                    _doClampOffCommand.SetValue(true, out _);
                    _doClampOnCommand.SetValue(false, out _);
                    break;
                case "ORGSH":
                    _currentExecuteCommand = "ORGSH";
                    _doIntialCommand.SetValue(true, out _);
                    break;
                default:
                    break;

            }
        }

        private string _currentExecuteCommand;

        protected override bool fMonitorExecuting(object[] param)
        {
            IsBusy = false;
            int timelimit = SC.ContainsItem($"LoadPort.{Name}.TimeLimitAction") ?
                SC.GetValue<int>($"LoadPort.{Name}.TimeLimitAction") : 45;
            if (DateTime.Now - _actionStartTime > TimeSpan.FromSeconds(timelimit))
            {
                OnError("Execute command timeout");
                return true;
            }
            switch(_currentExecuteCommand)
            {
                case "SetIndicator":
                    return true;
                case "Clamp":
                    if (_diAck_ClampOn.Value)
                    {
                        _doClampOnCommand.SetValue(false, out _);


                        return true;
                    }
                    break;
                case "Unclamp":
                    if (_diAck_ClampOff.Value)
                    {
                        _doClampOffCommand.SetValue(false, out _);
                        if (_diDockHomeSensor.Value)
                            base.OnUnloaded();
                        return true;
                    }
                    break;
                case "Dock":
                    if (_diAck_DockFwd.Value)
                    {
                        _doDOCKForwardCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "Undock":
                    
                        if (_diAck_DockFwd.Value)
                        {
                        _doDOCKHomeCommand.SetValue(false, out _);
                            return true;
                        }
                    break;
                case "VacuumOn":
                    if(_diAck_VacOn.Value)
                    {
                        _doVACOnCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "VacuumOff":
                    if(_diAck_VacOff.Value)
                    {
                        _doVACOffCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "LatchDoor":
                    if(_diAck_LatchOn.Value)
                    {
                        _doLatchOnCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "UnlatchDoor":
                    if(_diAck_LatchOff.Value)
                    {
                        _doLatchOffCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "OpenDoor":
                    if(_diAck_DoorOpen.Value)
                    {
                        _doDoorOpenCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "CloseDoor":
                    if(_diAck_DoorClose.Value)
                    {
                        _doDoorCloseCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "MoveToDoorOpenClosePostion":
                case "DoorUp":
                    if (_diAck_VerticalUp.Value)
                    {
                        _doVertiaclUpCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "MoveToMapStart":
                    if(_diAck_ZtoMapStart.Value)
                    {
                        _doZtoMAPPINGSTARTPOSI.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "MoveToMapEnd":
                    if(_diAck_ZtoMapEnd.Value)
                    {
                        _doZtoMAPPINGENDPOSI.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "DoorDown":
                    if(_diAck_VerticalDown.Value)
                    {
                        _doVertiaclDownCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                case "ORGSH":
                    if(_diAck_Init.Value)
                    {
                        _doIntialCommand.SetValue(false, out _);
                        return true;
                    }
                    break;
                
                case "MapWafer":
                //if(_diAck_Mapin)
                default:
                    return true;
            }
            return base.fMonitorExecuting(param);
        }

        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            return fStartExecute(new object[] { "SetIndicator", light, state });
        }

        public DOAccessor ParseDoNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.DO[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];

            return null;
        }

        public DIAccessor ParseDiNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.DI[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];
            return null;
        }

        public AOAccessor ParseAoNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.AO[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];
            return null;
        }

        public AIAccessor ParseAiNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.AI[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];
            return null;
        }
        public SCConfigItem ParseScNode(string name, XmlElement node, string ioModule = "", string defaultScPath = "")
        {
            SCConfigItem result = null;

            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                result = SC.GetConfigItem(node.GetAttribute(name));

            if (result == null && !string.IsNullOrEmpty(defaultScPath) && SC.ContainsItem(defaultScPath))
                result = SC.GetConfigItem(defaultScPath);

            return result;
        }

        public static T ParseDeviceNode<T>(string name, XmlElement node) where T : class, IDevice
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return DEVICE.GetDevice<T>(node.GetAttribute(name));
            LOG.Write(string.Format("{0}，未定义{1}", node.InnerXml, name));
            return null;
        }

        public static T ParseDeviceNode<T>(string module, string name, XmlElement node) where T : class, IDevice
        {
            string device_id = node.GetAttribute(name);
            if (!string.IsNullOrEmpty(device_id) && !string.IsNullOrEmpty(device_id.Trim()))
            {
                return DEVICE.GetDevice<T>($"{module}.{device_id}");
            }
            LOG.Write(string.Format("{0},undefined {1}", node.InnerXml, name));
            return null;
        }
        public enum CarrierMode
        {
            Foup,
            Fosb,
            OpenCassette,
        }

        private void SetAoValue(int id, AOAccessor ao, short value,Action<string> notify)
        {
            var ret = Execute(id, () =>
            {
                notify($"{LPModuleName} set {ao.Name} to {value}.");
                ao.Value = value;
                return true;
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
            }
        }
        private void WaitAiValue(int id, int time, AIAccessor ai, short value, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Wait {LPModuleName} {ai.Name} to be {value}");
                return true;
            }, () =>
            {
                if (ai.Value == value)
                    return true;

                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    EV.Notify(AlarmLoadPortError);
                    error($"Wait {LPModuleName} {ai.Name} to be {value} timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }
        private void WaitDiState(int id, int time, DIAccessor di,bool state, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Wait {LPModuleName} {di.Name} to be {state}");                
                return true;
            }, () =>
            {
                if (di.Value == state)
                    return true;

                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    EV.Notify(AlarmLoadPortError);
                    error($"Wait {LPModuleName} {di.Name} to be {state} timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }       
        private void SetDoState(int id, DOAccessor _do, bool state, Action<string> notify)
        {
            var ret = Execute(id, () =>
            {
                notify($"{LPModuleName} start set {_do.Name} to {state}.");
                if (_do.Value == state)
                {
                    _do.Value = !state;
                    Thread.Sleep(500);
                }
                return _do.SetValue(state, out _);                
            });

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }               
            }
        }





        protected void Notify(string message)
        {
            EV.PostMessage(Name, EventEnum.GeneralInfo, string.Format("{0}:{1}", Name, message));
        }
        protected void Stop(string failReason)
        {
            OnError(string.Format("Failed {0}, {1} ", Name, failReason));
        }





        private enum LoadPortStepEnum
        {
            LoadRecipe1,
            LoadRecipe2,
            LoadRecipe3,
            LoadRecipe4,
            LoadRecipe5,
            LoadRecipe6,

            ActionStep1,
            ActionStep2,
            ActionStep3,
            ActionStep4,
            ActionStep5,
            ActionStep6,
            ActionStep7,
            ActionStep8,

            ActionStep9,
            ActionStep10,
            ActionStep11,
            ActionStep12,

            ActionStep13,
            ActionStep14,
            ActionStep15,
            ActionStep16,
        }

        //timer, 计算routine时间
        protected DeviceTimer counter = new DeviceTimer();
        protected DeviceTimer delayTimer = new DeviceTimer();

        private enum STATE
        {
            IDLE,
            WAIT,
        }

        public int TokenId
        {
            get { return _id; }
        }
        private int _id;         //step index

        /// <summary>
        /// already done steps
        /// </summary>
        private Stack<int> _steps = new Stack<int>();

        private STATE state;    //step state //idel,wait,

        //loop control
        private int loop = 0;
        private int loopCount = 0;
        private int loopID = 0;

        private DeviceTimer timer = new DeviceTimer();

        public int LoopCounter { get { return loop; } }
        public int LoopTotalTime { get { return loopCount; } }

        // public int Timeout { get { return (int)(timer.GetTotalTime() / 1000); } }

        //状态持续时间，单位为秒
        public int Elapsed { get { return (int)(timer.GetElapseTime() / 1000); } }

        protected RoutineResult RoutineToken = new RoutineResult() { Result = RoutineState.Running };

        public void ResetRoutine()
        {
            _id = 0;
            _steps.Clear();

            loop = 0;
            loopCount = 0;

            state = STATE.IDLE;
            counter.Start(60 * 60 * 100);   //默认1小时

            RoutineToken.Result = RoutineState.Running;
        }
        protected void PerformRoutineStep(int id, Func<RoutineState> execution, RoutineResult result)
        {
            if (!Acitve(id))
                return;

            result.Result = execution();
        }



        #region interface

        public void StopLoop()
        {
            loop = loopCount;
        }

        public Tuple<bool, Result> Loop<T>(T id, Func<bool> func, int count)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (!func())
                {
                    return Tuple.Create(bActive, Result.FAIL);   //执行错误
                }

                loopID = idx;
                loopCount = count;

                next();
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> EndLoop<T>(T id, Func<bool> func)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (++loop >= loopCount)   //Loop 结束
                {
                    if (!func())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }

                    loop = 0;
                    loopCount = 0;  // Loop 结束时，当前loop和loop总数都清零

                    next();
                    return Tuple.Create(true, Result.RUN);
                }

                //继续下一LOOP

                next(loopID);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, IRoutine routine)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    Result startRet = routine.Start();
                    if (startRet == Result.FAIL)
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    else if (startRet == Result.DONE)
                    {
                        next();
                        return Tuple.Create(true, Result.DONE);
                    }
                    state = STATE.WAIT;
                }

                Result ret = routine.Monitor();

                if (ret == Result.DONE)
                {
                    next();
                    return Tuple.Create(true, Result.DONE);
                }
                else if (ret == Result.FAIL || ret == Result.TIMEOUT)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    return Tuple.Create(true, Result.RUN);
                }

            }

            return Tuple.Create(false, Result.RUN);
        }


        public Tuple<bool, Result> ExecuteAndWait<T>(T id, List<IRoutine> routines)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    foreach (var item in routines)
                    {
                        if (item.Start() == Result.FAIL)
                            return Tuple.Create(true, Result.FAIL);
                    }

                    state = STATE.WAIT;
                }
                //wait all sub failed or completedboo

                bool bFail = false;
                bool bDone = true;

                foreach (var item in routines)
                {
                    Result ret = item.Monitor();

                    bDone &= (ret == Result.FAIL || ret == Result.DONE);
                    bFail |= ret == Result.FAIL;
                }

                if (bDone)
                {
                    next();

                    if (bFail)
                        return Tuple.Create(true, Result.FAIL);

                    return Tuple.Create(true, Result.DONE);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }



        public Tuple<bool, Result> Check<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(Check(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Execute<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(execute(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, double timeout = int.MaxValue)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool? bExecute = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if (!execute())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(bActive, Result.FAIL);    //Termianate
                }
                else
                {
                    if (bExecute.Value)       //检查Success, next
                    {
                        next();
                        return Tuple.Create(true, Result.RUN);
                    }
                }

                if (timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, Func<double> time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool? bExecute = false;
            double timeout = 0;
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timeout = time();
                    if (!execute())
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }
                if (bExecute.Value)       //检查Success, next
                {
                    next();
                    return Tuple.Create(true, Result.RUN);
                }

                if (timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> Wait<T>(T id, IRoutine rt)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    rt.Start();
                    state = STATE.WAIT;
                }

                Result ret = rt.Monitor();

                return Tuple.Create(true, ret);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Monitor
        public Tuple<bool, Result> Monitor<T>(T id, Func<bool> func, Func<bool> check, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool bCheck = false;
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    timer.Start(time);
                    state = STATE.WAIT;
                }

                bCheck = check();

                if (!bCheck)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Delay
        public Tuple<bool, Result> Delay<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //先delay 再运行
        public Tuple<bool, Result> DelayCheck<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    if (func != null && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }
        #endregion


        private Tuple<bool, bool> execute(int id, Func<bool> func)   //顺序执行
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                if (bExecute)
                {
                    next();
                }
            }

            return Tuple.Create(bActive, bExecute);
        }


        private Tuple<bool, bool> Check(int id, Func<bool> func)   //check
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                next();
            }

            return Tuple.Create(bActive, bExecute);
        }


        /// <summary>

        /// </summary>
        /// <param name="id"></param>
        /// <param name="func"></param>
        /// <param name="timeout"></param>
        /// <returns>
        ///  item1 Active
        ///  item2 execute
        ///  item3 Timeout
        ///</returns>

        private Tuple<bool, bool, bool> wait(int id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        private Tuple<bool, bool?, bool> wait(int id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition && Check error
        {
            bool bActive = Acitve(id);
            bool? bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute.HasValue && bExecute.Value)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        /// <summary>      
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        /// item1 true, return item2
        /// </returns>
        private Tuple<bool, Result> Check(Tuple<bool, bool> value)
        {
            if (value.Item1)
            {
                if (!value.Item2)
                {
                    return Tuple.Create(true, Result.FAIL);
                }

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (CheckTimeout(value))  //timeout
                {
                    return Tuple.Create(true, Result.TIMEOUT);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool?, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (value.Item2 == null)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    if (value.Item2 == false && value.Item3 == true)  //timeout
                    {
                        return Tuple.Create(true, Result.TIMEOUT);
                    }
                    return Tuple.Create(true, Result.RUN);
                }
            }

            return Tuple.Create(false, Result.RUN);
        }

        private bool CheckTimeout(Tuple<bool, bool, bool> value)
        {
            return value.Item1 == true && value.Item2 == false && value.Item3 == true;
        }

        private bool Acitve(int id) //
        {
            if (_steps.Contains(id))
                return false;

            this._id = id;
            return true;
        }

        private void next()
        {
            _steps.Push(this._id);
            state = STATE.IDLE;
        }

        private void next(int step)   //loop
        {
            while (_steps.Pop() != step) ;

            state = STATE.IDLE;
        }


        public void Delay(int id, double delaySeconds)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                return true;
            }, delaySeconds * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.RUN)
                {
                    throw (new RoutineBreakException());
                }
            }
        }



        public bool IsActived(int id)
        {
            return _steps.Contains(id);
        }








    }
}
