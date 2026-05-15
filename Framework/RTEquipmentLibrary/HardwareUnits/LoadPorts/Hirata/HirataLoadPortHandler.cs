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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.Hirata
{
    public class HirataLoadPortHandler: HandlerBase
    {
        public HirataLoadPort Device { get; set; }
        public string Command;
        protected HirataLoadPortHandler(HirataLoadPort device,string command,string para): base(BuildMesage(command, para))
        {
            Device = device;
            Command = command;
            Name = command;
        }
        public static string BuildMesage(string data, string para)
        {
            List<byte> ret = new List<byte>();

            ret.Add(0x1);

            List<byte> cmd = new List<byte>();
            foreach (char c in data)
            {
                cmd.Add((byte)c);
            }
            //cmd.Add((byte)(':'));    //3A
            if(!string.IsNullOrEmpty(para))
                foreach(char c in para)
                {
                    cmd.Add((byte)c);
                }
            cmd.Add((byte)(';'));  //3B

            int length = cmd.Count + 4;

            int checksum = length + 0x30 +0x30;
            foreach(byte bvalue in cmd)
            {
                checksum += bvalue;
            }
            byte[] byteschecksum = Encoding.ASCII.GetBytes(Convert.ToString((int)((byte)(checksum & 0xFF)), 16));
          
            byte[] blength = BitConverter.GetBytes((short)length);
            ret.Add(blength[1]);
            ret.Add(blength[0]);
            //ret.AddRange(new byte[] { 0x30, 0x30 });
            ret.AddRange(new byte[] { 0x30, 0x30 });
            ret.AddRange(cmd);
            ret.AddRange(byteschecksum);
            ret.Add(0xD);
            return Encoding.ASCII.GetString(ret.ToArray());
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataLoadPortMessage response = msg as HirataLoadPortMessage;
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

    public class SetHandler:HirataLoadPortHandler
    {
        private string setcommand;
        public SetHandler(HirataLoadPort device, string command, string para) : base(device, BuildData(command), para)
        {
            setcommand = command;
        }
        private static string BuildData(string command)
        {
            return "SET:"+command;
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataLoadPortMessage response = msg as HirataLoadPortMessage;
            ResponseMessage = msg;
            //Device.TaExecuteSuccss = true;
            transactionComplete = false;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = false;
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
            if(setcommand == "RESET")
            {
                transactionComplete = true;
            }
            return true;
        }
    }

    public class GetHandler : HirataLoadPortHandler
    {
        public GetHandler(HirataLoadPort device, string command, string para) : base(device, BuildData(command), para)
        {

        }
        private static string BuildData(string command)
        {
            return "GET:"+command;
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataLoadPortMessage response = msg as HirataLoadPortMessage;
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

            if (cmddata.Contains("STATE"))
            {
                byte[] data = Encoding.ASCII.GetBytes(cmddata.Replace("STATE/",""));
                if (data.Length < 20) return;
                Device.SystemStatus = (HirataSystemStatus)data[0];                
                Device.Mode = (HirataMode)data[1];
                Device.InitPosMovement = (HirataInitPosMovement)data[2];
                Device.OperationStatus = (HirataOperationStatus)data[3];
                Device.HostErrorCode = data[4];
                Device.LoadPortErrorCode = data[5];
                Device.ContainerStatus = (HirataContainerStatus)data[6];
                Device.ClampPosition = (HirataPosition)data[7];
                Device.DoorLatchPosition = (HirataPosition)data[8];
                Device.VacuumStatus = (HirataVacummStatus)data[9];
                Device.DoorPosition = (HirataPosition)data[10];
                Device.WaferProtrusion = (HirataWaferProtrusion)data[11];
                Device.ElevatorAxisPosition = (HirataElevatorAxisPosition)data[12];
                Device.DockPosition = (HirataDockPosition)data[13];
                
                
                Device.MapperPostion = (HirataMapPosition)data[14];
                Device.MappingStatus = (HirataMappingStatus)data[17];
                Device.Model = (HirataModel)data[18];

                Device.Error = Device.SystemStatus != HirataSystemStatus.Normal;
                Device.ErrorCode = Encoding.ASCII.GetString(new byte[] { data[4], data[5] });
                LoadportCassetteState st = LoadportCassetteState.None;
                if (Device.ContainerStatus == HirataContainerStatus.Absence) st = LoadportCassetteState.Absent;
                if (Device.ContainerStatus == HirataContainerStatus.NormalMount) st = LoadportCassetteState.Normal;
                if (Device.ContainerStatus == HirataContainerStatus.MountError) st = LoadportCassetteState.Unknown;
                Device.SetCassetteState(st);

                if (Device.ClampPosition == HirataPosition.Close)
                    Device.ClampState = FoupClampState.Close;
                else if (Device.ClampPosition == HirataPosition.Open)
                    Device.ClampState = FoupClampState.Open;
                else if(Device.ClampPosition == HirataPosition.TBD)
                    Device.ClampState = FoupClampState.Unknown;

                if (Device.DoorPosition == HirataPosition.Close)
                    Device.DoorState = FoupDoorState.Close;
                if (Device.DoorPosition == HirataPosition.Open)
                    Device.DoorState = FoupDoorState.Open;
                if (Device.DoorPosition == HirataPosition.TBD)
                    Device.DoorState = FoupDoorState.Unknown;               

                Device.DockState = ConvertHirataDockPositin(Device.DockPosition);  // Load port dock state

                if (Device.ElevatorAxisPosition == HirataElevatorAxisPosition.UP)
                    Device.DoorPOS = TDKZ_AxisPos.Up;
                if (Device.ElevatorAxisPosition == HirataElevatorAxisPosition.Down)
                    Device.DoorPOS = TDKZ_AxisPos.Down;
                if (Device.ElevatorAxisPosition == HirataElevatorAxisPosition.MappingEndPos)
                    Device.DoorPOS = TDKZ_AxisPos.End;
                if (Device.ElevatorAxisPosition == HirataElevatorAxisPosition.MappingStartPos)
                    Device.DoorPOS = TDKZ_AxisPos.Start;
                if (Device.ElevatorAxisPosition == HirataElevatorAxisPosition.TBD)
                    Device.DoorPOS = TDKZ_AxisPos.Unknown;




            }
            if (cmddata.Contains("VERSN/"))
            {

            }
            if (cmddata.Contains("LEDST/"))
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
                char[] ledstate = cmddata.Replace("LEDST/", "").ToArray();

                for(int i=0; i< (lpledstate.Length> ledstate.Length? ledstate.Length:lpledstate.Length);i++)
                {
                    if (ledstate[i] == '0') lpledstate[i] = IndicatorState.OFF;
                    if (ledstate[i] == '1') lpledstate[i] = IndicatorState.ON;
                    if (ledstate[i] == '2') lpledstate[i] = IndicatorState.BLINK;
                }
            }
            if (cmddata.Contains("MAPDT/"))
            {                
                string str1 = cmddata.Replace("MAPDT/", "");
                string str2 = string.Empty;
                foreach (char c in str1)
                    str2 = c.ToString() + str1;


                Device.OnSlotMapRead(str2);
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
        }

        private FoupDockState ConvertHirataDockPositin(HirataDockPosition dockPosition)
        {
            if(dockPosition == HirataDockPosition.Dock) return FoupDockState.Docked;
            if (dockPosition == HirataDockPosition.Undock) return FoupDockState.Undocked;
            return FoupDockState.Unknown;
        }
    }

    public class ModHandler : HirataLoadPortHandler
    {
        public ModHandler(HirataLoadPort device, string command, string para) : base(device, BuildData(command), para)
        {

        }
        private static string BuildData(string command)
        {
            return "MOD:"+command;
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataLoadPortMessage response = msg as HirataLoadPortMessage;
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

    public class MoveHandler : HirataLoadPortHandler
    {
        private string moveCommand;
        public MoveHandler(HirataLoadPort device, string command, string para) : base(device, BuildData(command), para)
        {
            moveCommand = command;
        }
        private static string BuildData(string command)
        {
            return "MOV:"+command;
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            HirataLoadPortMessage response = msg as HirataLoadPortMessage;
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
                Device.ExecuteError = true;
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                //ParseError(response.Data);
            }
            if(response.IsError)
            {
                Device.ExecuteError = true;
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
            }
            if(response.IsEvent)
            {
                if(response.Command.Contains("ORGSH")|| response.Command.Contains("ABORG"))
                {
                    Device.Initalized = true;
                    Device.OnHomed();
                }
                if(response.Command.Contains(moveCommand))
                {
                    SetState(EnumHandlerState.Completed);
                    transactionComplete = true;
                }
            }

            return true;
        }


        public void ParseData(byte[] cmd)
        {


        }
    }


}
