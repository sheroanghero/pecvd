using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.FFUs
{
    class Ffu : SerialPortDeviceSimulator
    {
        public bool Failed { get; set; }

        public bool IsPumpOn { get; set; }

        public bool IsOverTemp { get; set; }

        public bool IsAtSpeed { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        public event Action<string> receiveMsg;

        private object _locker = new object();

        public string ResultValue { get; set; }

        public Ffu(string port)
            : base(port, -1, "\r", ' ', false)
        {
            ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

            IsAtSpeed = true;
        }

        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();

                    IsAtSpeed = true;
                }
            }


        }

        private byte GetSpeedValue()
        {
            Random random = new Random();
            return (byte)random.Next(0, 255);
        }

        protected override void ProcessUnsplitMessage(byte[] message)
        {
            if (CheckModRTU_CRC(message) != 0xff)
            { return; }
            base.ProcessUnsplitMessage(message);
            byte prefix = message[0];
            byte command = (byte)(message[1] / 16 * 16);
            byte deviceAddress = (byte)(message[1] % 16);
            byte groupAddress = message[2];
            switch (message.Length)
            {
                case 4:
                    switch (prefix)
                    {
                        case 0x15:
                            if (command == 0x20)
                            {
                                byte speed = GetSpeedValue();
                                byte[] speedmsg = new byte[] { 0x31, (byte)(deviceAddress + 0x20), groupAddress, speed, 0x00 };
                                speedmsg[4] = ModRTU_CRC(speedmsg);
                                ProcessWriteMessage(speedmsg);
                                receiveMsg($"Query Speed!{speed * 1440 / 250}");
                            }
                            if (command == 0x60)
                            {
                                if (receiveMsg != null)
                                {
                                    receiveMsg("Receive restart!");
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case 5:
                    switch (prefix)
                    {
                        case 0x35:
                            QueryMsg(message, prefix, command, deviceAddress, groupAddress);
                            break;
                        case 0x55:
                            SetMsg(message, prefix, command, deviceAddress, groupAddress);
                            break;
                        default:
                            break;
                    }
                    break;
                case 6:
                    switch (prefix)
                    {
                        case 0x55:
                            SetMsg(message, prefix, command, deviceAddress, groupAddress);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
           

        }
        /// <summary>
        /// 查询参数，含风速设置
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="command"></param>
        /// <param name="deviceAddress"></param>
        /// <param name="groupAddress"></param>
        private void QueryMsg(byte[] message, byte prefix, byte command, byte deviceAddress, byte groupAddress)
        {
            switch (command)
            {
                case 0x00:
                    byte[] errormsg = new byte[] { 0x51, (byte)(deviceAddress + 0x00), groupAddress, 0x00, 0x00, 0x00 };
                    errormsg[5] = ModRTU_CRC(errormsg);
                    ProcessWriteMessage(errormsg);
                    receiveMsg("Query Error!");//查询故障状态
                    break;
                case 0xE0:
                    if (message[3] == 0)
                    {
                        byte[] queryGroupymsg = new byte[] { 0x51, (byte)(deviceAddress + 0xE0), groupAddress, 0x05, 0x00, 0x00 };
                        queryGroupymsg[5] = ModRTU_CRC(queryGroupymsg);
                        ProcessWriteMessage(queryGroupymsg);
                        receiveMsg("Query Group ddress!");//查询组地址
                    }
                    else if (message[3] == 1)
                    {
                        byte[] queryGroupymsg = new byte[] { 0x51, (byte)(deviceAddress + 0xE0), groupAddress, 0x05, 0x01, 0x00 };
                        queryGroupymsg[5] = ModRTU_CRC(queryGroupymsg);
                        ProcessWriteMessage(queryGroupymsg);
                        receiveMsg("Query Device Address!");//查询机地址
                    }
                    else if (message[3] == 0x08)
                    {
                        byte[] queryMaxSpeed1msg = new byte[] { 0x51, (byte)(deviceAddress + 0xE0), groupAddress, 0x05, 0x08, 0x00 };
                        queryMaxSpeed1msg[5] = ModRTU_CRC(queryMaxSpeed1msg);
                        ProcessWriteMessage(queryMaxSpeed1msg);
                        receiveMsg("Query MaxSpeed1!");//查询最高转速 1
                    }
                    else if (message[3] == 0x09)
                    {
                        byte[] queryMaxSpeed2msg = new byte[] { 0x51, (byte)(deviceAddress + 0xE0), groupAddress, 0x05, 0x09, 0x00 };
                        queryMaxSpeed2msg[5] = ModRTU_CRC(queryMaxSpeed2msg);
                        ProcessWriteMessage(queryMaxSpeed2msg);
                        receiveMsg("Query MaxSpeed2!");//查询最高转速 2
                    }
                    else if (message[3] == 0x0A)
                    {
                        byte[] queryMaxSpeed3msg = new byte[] { 0x51, (byte)(deviceAddress + 0xE0), groupAddress, 0x05, 0x0a, 0x00 };
                        queryMaxSpeed3msg[5] = ModRTU_CRC(queryMaxSpeed3msg);
                        ProcessWriteMessage(queryMaxSpeed3msg);
                        receiveMsg("Query MaxSpeed3!");//查询最高转速 3
                    }
                    else if (message[3] == 0x11)
                    {
                        byte[] querySpeedFactormsg = new byte[] { 0x51, (byte)(deviceAddress + 0xE0), groupAddress, 0x05, 0x11, 0x00 };
                        querySpeedFactormsg[5] = ModRTU_CRC(querySpeedFactormsg);
                        ProcessWriteMessage(querySpeedFactormsg);
                        receiveMsg("Query  Speed Factor!");//查询转速因子
                    }
                    break;
                case 0x40:
                    byte[] setSpeedmsg = new byte[] { 0x11, (byte)(deviceAddress + 0x40), groupAddress,  0x00 };
                    setSpeedmsg[3] = ModRTU_CRC(setSpeedmsg);
                    ProcessWriteMessage(setSpeedmsg);
                    receiveMsg($"Set Speed {message[3]}!");//设置风速
                    break;
                default:
                    break;
            }
        }

        private void SetMsg(byte[] message, byte prefix, byte command, byte deviceAddress, byte groupAddress)
        {
            switch (message[3])
            {
                case 0x00://设置组地址
                    byte[] returnGroupAddressMsg = new byte[] { 0x11, (byte)(deviceAddress + 0xC0), groupAddress, 00};
                    returnGroupAddressMsg[3] = ModRTU_CRC(returnGroupAddressMsg);
                    ProcessWriteMessage(returnGroupAddressMsg);
                    receiveMsg($"Set Group Address{message[4]}!");
                    break;
                case 0x01://设置机地址
                    byte[] returnDeviceAddressMsg = new byte[] { 0x11, (byte)(deviceAddress + 0xC0), groupAddress, 00 };
                    returnDeviceAddressMsg[3] = ModRTU_CRC(returnDeviceAddressMsg);
                    ProcessWriteMessage(returnDeviceAddressMsg);
                    receiveMsg($"Set Device Address{message[4]}!");
                    break;
                case 0x11://设置转速因子
                    byte[] returnSpeedFactorMsg = new byte[] { 0x11, (byte)(deviceAddress + 0xC0), groupAddress, 00 };
                    returnSpeedFactorMsg[3] = ModRTU_CRC(returnSpeedFactorMsg);
                    ProcessWriteMessage(returnSpeedFactorMsg);
                    receiveMsg($"Set Device Address{message[4]}!");
                    break;
                default:
                    break;
            }
        }

            private static byte ModRTU_CRC(byte[] buffer)
        {
            //ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;
            byte temp = buffer[0];

            for (int i = 1; i < buffer.Length; i++)
            {
                temp = (byte)(temp ^ buffer[i]);
            }
            return (byte)~temp;
        }


        private static byte CheckModRTU_CRC(byte[] buffer)
        {
            //ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;
            byte temp = buffer[0];

            for (int i = 1; i < buffer.Length; i++)
            {
                temp = (byte)(temp ^ buffer[i]);
            }
            return (byte)temp;
        }

        protected override void ProcessUnsplitMessage(string msg)
        {
            //if (!Failed)
            {
                if (msg == "0010030302=?101\r")
                {
                    //OnWriteMessage("0011030306000000014" + "\r" );

                    string text = "0011030306" + (Failed ? _rd.Next(0, 999).ToString("D6") : "000000");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }

                if (msg == "0010039802=?115\r")
                {
                    string text = "0011039806" + _rd.Next(0, 999999).ToString("D6");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }

                if (msg == "0010034602=?108\r")
                {
                    string text = "0011034606" + _rd.Next(0, 999999).ToString("D6");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }
                if (msg == "0011001006111111015\r")
                {
                    lock (_locker)
                    {
                        if (!IsPumpOn)
                        {
                            IsPumpOn = true;
                            IsAtSpeed = false;
                            _timer.Restart();
                        }
                    }


                    OnWriteMessage("0011001006111111015" + "\r");
                    return;
                }

                if (msg == "0011001006000000009\r")
                {
                    lock (_locker)
                    {
                        if (IsPumpOn)
                        {
                            IsPumpOn = false;
                            IsAtSpeed = false;

                            _timer.Restart();
                        }
                    }

                    OnWriteMessage("0011001006000000009" + "\r");
                    return;
                }

                if (msg == "0010001002=?096\r")
                {
                    string text = "0011001006" + (IsPumpOn ? "111111" : "000000");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }

                if (msg == "0010030502=?103\r")
                {
                    string text = "0011030506" + (IsOverTemp ? "111111" : "000000");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }


                if (msg == "0010030602=?104\r")
                {
                    string text = "0011030606" + (IsAtSpeed ? "111111" : "000000");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }


                if (msg == "0010030702=?105\r")
                {
                    string text = "0011030706" + (IsAtSpeed ? "000000" : "111111");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }
            }

        }




    }
}
