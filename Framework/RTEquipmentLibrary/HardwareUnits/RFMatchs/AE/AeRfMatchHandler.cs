using System;
using System.Collections.Generic;
using System.Linq;
using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.AE
{
    public abstract class AeRfMatchHandler : HandlerBase
    {
        public AeRfMatch Device { get; }

        private byte _address;
        private byte _command;

        protected AeRfMatchHandler(AeRfMatch device, byte address, byte command, byte[] data)
            : base(BuildMessage(address, command, data))
        {
            Device = device;
            _address = address;
            _command = command;
        }

        private static byte[] BuildMessage(byte address, byte command, byte[] data)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add((byte)((address << 3) + (data == null ? 0 : data.Length)));

            buffer.Add(command);

            if (data != null && data.Length > 0)
            {
                buffer.AddRange(data);
            }

            buffer.Add(CalcSum(buffer, buffer.Count));

            return buffer.ToArray();
        }

        //host->unit, unit->host(ack), unit->host(csr), host->unit(ack)
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            AeRfMatchMessage response = msg as AeRfMatchMessage;
            ResponseMessage = msg;

            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }

            if (response.Address != _address || response.CommandNumber != _command)
            {
                transactionComplete = false;
                return false;
            }

            if (response.IsResponse)
            {
                if (response.DataLength >= 1)
                {
                    ParseData(response);
                }

                SendAck();

                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                return true;
            }

            transactionComplete = false;
            return false;
        }

        protected virtual void ParseData(AeRfMatchMessage msg)
        {
            if (msg.Data[0] != 0)
            {
                var reason = TranslateCsrCode(msg.Data[0]);
                Device.NoteError(reason);
            }
        }

        public void SendAck()
        {
            Device.Connection.SendMessage(new byte[] { 0x06 });
        }

        private static byte CalcSum(List<byte> data, int length)
        {
            byte ret = 0x00;
            for (var i = 0; i < length; i++)
            {
                ret ^= data[i];
            }
            return ret;
        }

        protected string TranslateCsrCode(int csrCode)
        {
            string ret = csrCode.ToString();
            switch (csrCode)
            {
                case 0:
                    ret = null;//"Command accepted";
                    break;
                case 1:
                    ret = "Control Code Is Incorrect";
                    break;
                case 2:
                    ret = "Output Is On(Change Not Allowed)";
                    break;
                case 4:
                    ret = "Data Is Out Of Range";
                    break;
                case 7:
                    ret = "Active Fault(s) Exist";
                    break;
                case 9:
                    ret = "Data Byte Count Is Incorrect";
                    break;
                case 19:
                    ret = "Recipe Is Active(Change Not Allowed)";
                    break;
                case 50:
                    ret = "The Frequency Is Out Of Range";
                    break;
                case 51:
                    ret = "The Duty Cycle Is Out Of Range";
                    break;
                case 53:
                    ret = "The Device Controlled By The Command Is Not Detected";
                    break;
                case 99:
                    ret = "Command Not Accepted(There Is No Such Command)";
                    break;
                default:
                    break;
            }
            return ret;
        }
    }

    //91 set active preset number
    public class AeRfMatchSetActivePresetHandler : AeRfMatchHandler
    {
        public AeRfMatchSetActivePresetHandler(AeRfMatch device, byte address, byte network, int number)
            : base(device, address, 91, BuildData(network, number))
        {
            Name = "set active preset number";
        }
        private static byte[] BuildData(byte network, int number)
        {
            return new byte[] { network, 0, (byte)number, 0 };
        }
    }

    //92 set   preset 
    public class AeRfMatchSetPresetHandler : AeRfMatchHandler
    {
        public AeRfMatchSetPresetHandler(AeRfMatch device, byte address, byte network, Presets data)
            : base(device, address, 92, BuildData(network, data))
        {
            Name = "set active preset number";
        }
        private static byte[] BuildData(byte network, Presets data)
        {
            List<byte> lstData = new List<byte>();
            lstData.AddRange(new byte[]{ network, 0, data.PreNo, data.TraSum });

            int tempdata = (int)(data.LoadData * 100);
            lstData.Add((byte)tempdata);
            lstData.Add((byte)(tempdata >> 8));

            tempdata = (int)(data.TuneData * 100);
            lstData.Add((byte)tempdata);
            lstData.Add((byte)(tempdata >> 8));
            for (int i = 0; i < data.TraSum; i++)
            {
                tempdata = (int)(data.TraData[i, 0] * 100);
                lstData.Add((byte)tempdata);
                lstData.Add((byte)(tempdata >> 8));
                tempdata = (int)(data.TraData[i, 1] * 100);
                lstData.Add((byte)tempdata);
                lstData.Add((byte)(tempdata >> 8));
            }

            return lstData.ToArray();
        }
    }
 
    //93 control mode
    public class AeRfMatchSetControlModeHandler : AeRfMatchHandler
    {
        public AeRfMatchSetControlModeHandler(AeRfMatch device, byte address, byte network, EnumRfMatchTuneMode mode)
            : base(device, address, 93, BuildData(network, mode))
        {
            Name = "Set control mode";
        }
        
        private static byte[] BuildData(byte network, EnumRfMatchTuneMode mode)
        {
            byte setpoint = 0;
            switch (mode)
            {
                case EnumRfMatchTuneMode.Auto:
                    setpoint = 1;
                    break;
                case EnumRfMatchTuneMode.Manual:
                    setpoint = 2;
                    break;
            }
            return new byte[]{network, 0, setpoint, 0};
        }
    }

    //94 enable presets
    public class AeRfMatchEnablePresetHandler : AeRfMatchHandler
    {
        public AeRfMatchEnablePresetHandler(AeRfMatch device, byte address, byte network, bool enable)
            : base(device, address, 94, BuildData(network, enable))
        {
            Name = "enable preset";
        }

        private static byte[] BuildData(byte network, bool enable)
        {
            return new byte[] { network, 0, enable?(byte)0x1: (byte)0x0, 0 };
        }
    }

    //98 enable motor move
    public class AeRfMatchEnableCapMoveHandler : AeRfMatchHandler
    {
        public AeRfMatchEnableCapMoveHandler(AeRfMatch device, byte address, byte network, bool enable)
            : base(device, address, 98, BuildData(network, enable))
        {
            Name = "enable motor move";
        }
        private static byte[] BuildData(byte network, bool enable)
        {
            return new byte[] { network, 0, enable ? (byte)0x1 : (byte)0x0, 0 };
        }
    }

    //112 set load position
    public class AeRfMatchSetLoadPositionHandler : AeRfMatchHandler
    {
        public AeRfMatchSetLoadPositionHandler(AeRfMatch device, byte address, byte network, float position)
            : base(device, address, 112, BuildData(network, position))
        {
            Name = "set load position";
        }
        private static byte[] BuildData(byte network, float position)
        {
            int iposi = (int)(position * 100);
            return new byte[] { network, 0, (byte)iposi, (byte)(iposi >> 8) };
        }
    }

    //122 set tune position
    public class AeRfMatchSetTunePositionHandler : AeRfMatchHandler
    {
        public AeRfMatchSetTunePositionHandler(AeRfMatch device, byte address, byte network, float position)
            : base(device, address, 122, BuildData(network, position))
        {
            Name = "set tune position";
        }
        private static byte[] BuildData(byte network, float position)
        {
            int iposi = (int)(position * 100);
            return new byte[] { network, 0, (byte)iposi, (byte)(iposi >> 8) };
        }
    }

    //161 query preset no
    public class AeRfMatchQueryPresetNumberHandler : AeRfMatchHandler
    {
        public AeRfMatchQueryPresetNumberHandler(AeRfMatch device, byte address)
            : base(device, address, 161, null)
        {
            Name = "query preset mode";
        }
        protected override void ParseData(AeRfMatchMessage response)
        {
            if (response.DataLength != 4)
            {
                Device.NoteError($"{Name}, return data length {response.DataLength}");
            }
            else
            {
                Device.NotePresetNumber(response.Data[2] + (response.Data[3] << 8));
            }
        }
    }
 
    //219 query status
    public class AeRfMatchQueryStatusHandler : AeRfMatchHandler
    {
        private AEStatusData _statusData;

        public AeRfMatchQueryStatusHandler(AeRfMatch device, byte address)
            : base(device, address, 219, null)
        {
            Name = "query status";
        }
        protected override void ParseData(AeRfMatchMessage response)
        {
            if (response.DataLength != 88)
            {
                Device.NoteError($"{Name}, return data length {response.DataLength}");
            }
            else
            {
                var retData = response.Data;
                 _statusData = new AEStatusData();
                 
                _statusData.LoadPosi1 = (retData[4] + (retData[5] << 8)) / 100f;
                _statusData.TunePosi1 = (retData[6] + (retData[7] << 8)) / 100f;
                _statusData.LoadPosi2 = (retData[12] + (retData[13] << 8)) / 100f;
                _statusData.TunePosi2 = (retData[14] + (retData[15] << 8)) / 100f;
                _statusData.BiasPeak = (retData[20] + (retData[21] << 8)) / 100f;
                _statusData.DCBias = (retData[22] + (retData[23] << 8)) / 100f;
                _statusData.ZScanII.R1 = BitConverter.ToSingle(retData.ToArray(), 40); 
                _statusData.ZScanII.X1 = BitConverter.ToSingle(retData.ToArray(), 44); 
                _statusData.ZScanII.Voltage1 = BitConverter.ToSingle(retData.ToArray(), 48) * 1.41421f; 
                _statusData.ZScanII.Current1 = BitConverter.ToSingle(retData.ToArray(), 52); 
                _statusData.ZScanII.Phase1 = BitConverter.ToSingle(retData.ToArray(), 56); 
                _statusData.ZScanII.Power1 = BitConverter.ToSingle(retData.ToArray(), 60); 
                _statusData.ZScanII.R2 = BitConverter.ToSingle(retData.ToArray(), 64); 
                _statusData.ZScanII.X2 = BitConverter.ToSingle(retData.ToArray(), 68); 
                _statusData.ZScanII.Voltage2 = BitConverter.ToSingle(retData.ToArray(), 72); 
                _statusData.ZScanII.Current2 = BitConverter.ToSingle(retData.ToArray(), 76); 
                _statusData.ZScanII.Phase2 = BitConverter.ToSingle(retData.ToArray(), 80); 
                _statusData.ZScanII.Power2 = BitConverter.ToSingle(retData.ToArray(), 84); 
                AnalysisAEMatchStatus(retData);

                Device.NoteStatus(_statusData);
            }
        }

        private void AnalysisAEMatchStatus(byte[] sts)
        {
            _statusData.Status2.Net1OutPutOn = ((sts[0] >> 0) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net1OutPutTuned = ((sts[0] >> 1) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net2OutPutOn = ((sts[0] >> 2) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net2OutPutTuned = ((sts[0] >> 3) & 0x01) == 0x01 ? true : false;
            //RecvData.Status2.Net2OutPutOn = ((sts[0] >> 4) & 0x01) == 0x01 ? true : false;
            //RecvData.Status2.Net2OutPutTuned = ((sts[0] >> 5) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net1PresetsActive = ((sts[0] >> 6) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net1ExtPresetsSelected = ((sts[0] >> 7) & 0x01) == 0x01 ? true : false;

            _statusData.Status2.Low24VDetected = ((sts[1] >> 0) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.OverTempDetected = ((sts[1] >> 1) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.InterlockOpen = ((sts[1] >> 2) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.FanFault = ((sts[1] >> 3) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net1AutoMode = ((sts[1] >> 4) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net1HostCtrlMode = ((sts[1] >> 5) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net2AutoMode = ((sts[1] >> 6) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net2HostCtrlMode = ((sts[1] >> 7) & 0x01) == 0x01 ? true : false;

            _statusData.Status2.AuxCapOutputTuned = ((sts[2] >> 0) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.AuxCapAutoModed = ((sts[2] >> 1) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.AuxCapPresetsActive = ((sts[2] >> 2) & 0x01) == 0x01 ? true : false;
            //RecvData.Status2.FanFault = ((sts[1] >> 3) & 0x01) == 0x01 ? true : false;
            //RecvData.Status2.Net1AutoMode = ((sts[1] >> 4) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net1UserCtrlMode = ((sts[2] >> 5) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net2UserCtrlMode = ((sts[2] >> 6) & 0x01) == 0x01 ? true : false;
            //RecvData.Status2.Net2HostCtrlMode = ((sts[2] >> 7) & 0x01) == 0x01 ? true : false;

            _statusData.Status2.Faults = ((sts[3] >> 0) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Warning = ((sts[3] >> 1) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.InitMotorFailed = ((sts[3] >> 2) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net2PresetsActive = ((sts[3] >> 3) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.Net2ExtPresetsSelected = ((sts[3] >> 4) & 0x01) == 0x01 ? true : false;
            _statusData.Status2.VoltageOverLimitFault = ((sts[3] >> 5) & 0x01) == 0x01 ? true : false;
            //RecvData.Status2.Net2AutoMode = ((sts[3] >> 6) & 0x01) == 0x01 ? true : false;
            //RecvData.Status2.Net2HostCtrlMode = ((sts[3] >> 7) & 0x01) == 0x01 ? true : false;
        }

    }
}
