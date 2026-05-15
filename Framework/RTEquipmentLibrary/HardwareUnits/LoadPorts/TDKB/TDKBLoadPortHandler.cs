using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDKB
{
    public class TDKBLoadPortHandler : HandlerBase
    {
        public TDKBLoadPort Device { get; set; }
        public string Command;
        protected TDKBLoadPortHandler(TDKBLoadPort device, string command, string para) : base(BuildMesage(command, para))
        {
            Device = device;
            Command = command;
            Name = command;
        }
        public static string BuildMesage(string command, string para)
        {
            if(string.IsNullOrEmpty(para))
                return $"s00{command};\r";
            return $"s00{command}{para};\r";
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TDKBLoadPortMessage response = msg as TDKBLoadPortMessage;
            ResponseMessage = msg;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = true;
            }

            if (response.IsError)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsEvent)
            {
                SendAck();
                if (response.Command.Contains(Command))
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                }

            }

            return true;
        }

        public void HandleEvent(string content)
        {

        }
        public void SendAck()
        {
            //Device.Connection.SendMessage(new byte[] { 0x06 });
        }

        public void Retry()
        {

        }
        public virtual bool PaseData(byte[] data)
        {
            return true;
        }
    }

    public class TDKBSetHandler : TDKBLoadPortHandler
    {
        private string setcommand;
        public TDKBSetHandler(TDKBLoadPort device, string command, string para) : base(device, BuildData(command), para)
        {
            setcommand = command;
        }
        private static string BuildData(string command)
        {
            return "SET:" + command;
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TDKBLoadPortMessage response = msg as TDKBLoadPortMessage;
            ResponseMessage = msg;
            //Device.TaExecuteSuccss = true;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = false;
                if (setcommand.Contains("FSB"))
                    transactionComplete = true;
            }
            if (response.IsBusy)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Completed);

                transactionComplete = true;
                //ParseError(response.Data);
            }
            if (response.IsError)
            {
                SetState(EnumHandlerState.Completed);

                transactionComplete = true;

            }
            if (response.IsEvent)
            {
                SendAck();
                if (response.Command.Contains(setcommand))
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                }

                else
                    HandleEvent(response.Command);
                transactionComplete = true;
            }
            //if (setcommand == "RESET")
            //{
            //    transactionComplete = true;
            //}
            return true;
        }
    }

    public class TDKBGetHandler : TDKBLoadPortHandler
    {
        public TDKBGetHandler(TDKBLoadPort device, string command, string para) : base(device, BuildData(command), para)
        {

        }
        private static string BuildData(string command)
        {
            return "GET:" + command;
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TDKBLoadPortMessage response = msg as TDKBLoadPortMessage;
            ResponseMessage = msg;
            //Device.TaExecuteSuccss = true;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = true;
                ParseData(response.Command);
            }

            if (response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                //ParseError(response.Data);
            }



            return true;
        }


        public void ParseData(string cmddata)
        {
            try
            {

                if (cmddata.Contains("STATE"))
                {
                    byte[] data = Encoding.ASCII.GetBytes(cmddata.Replace("STATE/", ""));
                    if (data.Length < 20) return;
                    Device.SystemStatus = (TDKSystemStatus)data[0];
                    Device.Mode = (TDKMode)data[1];
                    Device.InitPosMovement = (TDKInitPosMovement)data[2];
                    Device.OperationStatus = (TDKOperationStatus)data[3];
                    Device.ErrorCode = Encoding.ASCII.GetString(new byte[] { data[4], data[5] });

                    Device.ContainerStatus = (TDKContainerStatus)data[6];
                    Device.ClampPosition = (TDKPosition)data[7];
                    Device.LPDoorLatchPosition = (TDKPosition)data[8];
                    Device.VacuumStatus = (TDKVacummStatus)data[9];
                    Device.LPDoorState = (TDKPosition)data[10];
                    Device.WaferProtrusion = (TDKWaferProtrusion)data[11];
                    Device.ElevatorAxisPosition = (TDKElevatorAxisPosition)data[12];
                    Device.DockPosition = (TDKDockPosition)data[13];


                    Device.MapperPostion = (TDKMapPosition)data[14];
                    Device.MappingStatus = (TDKMappingStatus)data[17];
                    Device.Model = (TDKModel)data[18];

                    int infopadstatus = Convert.ToInt16($"0x{Encoding.ASCII.GetString(new byte[] { data[19] })}", 16);
                  

                    int indexValue = (((infopadstatus & 1) != 0) ? 8 : 0) + (((infopadstatus & 2) != 0) ? 4 : 0) +
                        (((infopadstatus & 4) != 0) ? 2 : 0) + (((infopadstatus & 8) !=0) ? 1 : 0);


                    Device.LPSetInfoPadSensorIndex(indexValue);
                    
                    Device.IsError = Device.SystemStatus != TDKSystemStatus.Normal;
                    Device.ErrorCode = Encoding.ASCII.GetString(new byte[] { data[4], data[5] });
                    LoadportCassetteState st = LoadportCassetteState.None;
                    if (Device.ContainerStatus == TDKContainerStatus.Absence) st = LoadportCassetteState.Absent;
                    if (Device.ContainerStatus == TDKContainerStatus.NormalMount) st = LoadportCassetteState.Normal;
                    if (Device.ContainerStatus == TDKContainerStatus.MountError)
                    {
                        st = LoadportCassetteState.Unknown;
                        //Device.OnEvent(out _);
                    }
                    Device.SetCassetteState(st);

                    if (Device.ClampPosition == TDKPosition.Close)
                        Device.ClampState = FoupClampState.Close;
                    else if (Device.ClampPosition == TDKPosition.Open)
                        Device.ClampState = FoupClampState.Open;
                    else if (Device.ClampPosition == TDKPosition.TBD)
                        Device.ClampState = FoupClampState.Unknown;

                    if (Device.LPDoorState == TDKPosition.Close)
                        Device.DoorState = FoupDoorState.Close;
                    if (Device.LPDoorState == TDKPosition.Open)
                        Device.DoorState = FoupDoorState.Open;
                    if (Device.LPDoorState == TDKPosition.TBD)
                        Device.DoorState = FoupDoorState.Unknown;

                    Device.DockState = ConvertTDKDockPositin(Device.DockPosition);  // Load port dock state

                    if (Device.ElevatorAxisPosition == TDKElevatorAxisPosition.UP)
                        Device.DoorPosition = FoupDoorPostionEnum.Up;
                    if (Device.ElevatorAxisPosition == TDKElevatorAxisPosition.Down)
                        Device.DoorPosition = FoupDoorPostionEnum.Down; // = TDKZ_AxisPos.Down;
                    if (Device.ElevatorAxisPosition == TDKElevatorAxisPosition.MappingEndPos)
                        Device.DoorPosition = FoupDoorPostionEnum.MapEnd;// = TDKZ_AxisPos.End;
                    if (Device.ElevatorAxisPosition == TDKElevatorAxisPosition.MappingStartPos)
                        Device.DoorPosition = FoupDoorPostionEnum.MapStart;// = TDKZ_AxisPos.Start;
                    if (Device.ElevatorAxisPosition == TDKElevatorAxisPosition.TBD)
                        Device.DoorPosition = FoupDoorPostionEnum.Unknown;// = TDKZ_AxisPos.Unknown;




                }
                if (cmddata.Contains("VERSN/"))
                {

                }
                if (cmddata.Contains("LEDST/"))
                {
                    char[] ledstate = cmddata.Replace("LEDST/", "").ToArray();
                    if (ledstate.Length == 9)
                    {
                        IndicatorState[] lpledstate = new IndicatorState[]
                        {
                            Device.IndicatiorLoad,
                            Device.IndicatiorUnload,
                            Device.IndicatiorAccessManual,
                            Device.IndicatiorPresence,
                            Device.IndicatiorPlacement,
                            Device.IndicatiorAccessAuto,
                            Device.IndicatiorStatus1,
                            Device.IndicatorAlarm,
                         };
                        for (int i = 0; i < (lpledstate.Length > ledstate.Length ? ledstate.Length : lpledstate.Length); i++)
                        {
                            if (ledstate[i] == '0') lpledstate[i] = IndicatorState.OFF;
                            if (ledstate[i] == '1') lpledstate[i] = IndicatorState.ON;
                            if (ledstate[i] == '2') lpledstate[i] = IndicatorState.BLINK;
                        }
                    }
                    else
                    {
                        IndicatorState[] lpledstate = new IndicatorState[]
                        {
                            Device.IndicatiorPresence,
                            Device.IndicatiorPlacement,
                            Device.IndicatiorLoad,
                            Device.IndicatiorUnload,
                            Device.IndicatiorOpAccess,
                            Device.IndicatiorStatus1,
                            Device.IndicatiorStatus2,
                         };
                        for (int i = 0; i < (lpledstate.Length > ledstate.Length ? ledstate.Length : lpledstate.Length); i++)
                        {
                            if (ledstate[i] == '0') lpledstate[i] = IndicatorState.OFF;
                            if (ledstate[i] == '1') lpledstate[i] = IndicatorState.ON;
                            if (ledstate[i] == '2') lpledstate[i] = IndicatorState.BLINK;
                        }
                    }
                }
                if (cmddata.Contains("MAPRD/"))
                {
                    string str1 = cmddata.Replace("MAPRD/", "");



                    Device.OnSlotMapRead(str1);
                }
                if (cmddata.Contains("WFCNT/"))
                {

                    Device.WaferCount = Convert.ToInt16(cmddata.Replace("WFCNT/", ""));
                }
                if (cmddata.Contains("MDAH/"))
                {

                }
                if (cmddata.Contains("LPIOI/"))
                {

                }
                if (cmddata.Contains("FSBxx/"))
                {
                    if (cmddata.Contains("ON"))
                        Device.IsFosbModeActual = true;
                    if (cmddata.Contains("OF"))
                        Device.IsFosbModeActual = false;
                }
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
            }
        }

        private void SetIEDValue(int index, IndicatorState state)
        {
            switch (index)
            {
                case 0:
                    Device.IndicatiorPresence = state;
                    break;
                case 1:
                    Device.IndicatiorPlacement = state;
                    break;
                case 2:
                    Device.IndicatiorLoad = state;
                    break;
                case 3:
                    Device.IndicatiorUnload = state;
                    break;
                case 4:
                    Device.IndicatiorOpAccess = state;
                    break;
                case 5:
                    Device.IndicatiorStatus1 = state;
                    break;
                case 6:
                    Device.IndicatiorStatus2 = state;
                    break;
                default:
                    break;
            }
        }

        private FoupDockState ConvertTDKDockPositin(TDKDockPosition dockPosition)
        {
            if (dockPosition == TDKDockPosition.Dock) return FoupDockState.Docked;
            if (dockPosition == TDKDockPosition.Undock) return FoupDockState.Undocked;
            return FoupDockState.Unknown;
        }
    }

    public class TDKBModHandler : TDKBLoadPortHandler
    {
        public TDKBModHandler(TDKBLoadPort device, string command, string para) : base(device, BuildData(command), para)
        {

        }
        private static string BuildData(string command)
        {
            return "MOD:" + command;
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TDKBLoadPortMessage response = msg as TDKBLoadPortMessage;
            ResponseMessage = msg;
            //Device.TaExecuteSuccss = true;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = true;
            }

            if (response.IsNak)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                //ParseError(response.Data);
            }



            return true;
        }


        public void ParseData(byte[] cmd)
        {


        }
    }

    public class TDKBMoveHandler : TDKBLoadPortHandler
    {
        private string moveCommand;
        public TDKBMoveHandler(TDKBLoadPort device, string command, string para) : base(device, BuildData(command), para)
        {
            moveCommand = command;
        }
        private static string BuildData(string command)
        {
            return "MOV:" + command;
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            TDKBLoadPortMessage response = msg as TDKBLoadPortMessage;
            ResponseMessage = msg;
            //Device.TaExecuteSuccss = true;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = false;
            }

            if (response.IsNak)
            {
                Device.OnCommandFailed(moveCommand);
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                //ParseError(response.Data);
            }
            if (response.IsError)
            {
                Device.OnCommandFailed(moveCommand);
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if (response.IsEvent)
            {
                if (response.Command.Contains("ORGSH") || response.Command.Contains("ABORG"))
                {
                    Device.OnHomed();
                }
                if (response.Command.Contains(moveCommand))
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                    Device.OnCommandSuccess(moveCommand);
                }
            }

            return true;
        }


        public void ParseData(byte[] cmd)
        {


        }
    }

}
