using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.RFs
{
    class SimMksRfPlasmaGenerator : SerialPortDeviceSimulator
    {
        public bool Failed { get; set; }

        public bool IsOn { get; set; }

        public bool IsEnable { get; set; }

        public bool IsHalo { get; set; }

        public bool IsContinueAck { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        private int _onOffData = 0;
        private int _setPointData = 0;
        private byte[] _faultData = new byte[2];
        private byte[] _statusData = new byte[2];
        private int _forwardPower = 0;  //正向功率
        private int _reversePower = 0;  //逆向功率
        private int _deliveredPower = 0;    //输出功率
        private int _controller_output = 0; //控制器输出
        private int _interface = 0;
        private int _pulseHighTime = 0;
        private int _pulseLowTime = 0;

        private string _lastCmd;


        public SimMksRfPlasmaGenerator(string port)
            : base(port,115200, -1, "", ' ', true)
        {
            _pulseLowTime = 20000;
            _pulseHighTime = 20000;
            _statusData[1] = 0x08;

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

        }

        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();
                }
            }

        }

        
        protected override void ProcessUnsplitMessage(string message)
        {
            string sendData = message + "\r\n";
            if (message == "E")
            {
                IsEnable = true;
                _lastCmd = message;
            }
            else if (message == "I")
            {
                IsEnable = false;
                _lastCmd = message;
            }
            else if (message == "N")
            {
                IsOn = true;
                _lastCmd = message;
            }
            else if (message == "F")
            {
                IsOn = false;
                _lastCmd = message;
            }
            else if (message == "P")
            {
                sendData += "Enter New Setpoint (AAAAAA ‐ BBBBBB)";
                _lastCmd = message;
            }
            else if (message == "T")
            {
                sendData += (IsOn? "000001" : "000000") + " " + _setPointData.ToString().PadLeft(6, '0')
                        + " " + System.Convert.ToString(_faultData[0], 2).PadLeft(8, '0') + System.Convert.ToString(_faultData[1], 2).PadLeft(8, '0')
                        + " " + System.Convert.ToString(_statusData[0], 2).PadLeft(8,'0') + System.Convert.ToString(_statusData[1], 2).PadLeft(8, '0')
                        + " " + _forwardPower.ToString().PadLeft(6, '0') + " " + _reversePower.ToString().PadLeft(6, '0')
                        + " " + _deliveredPower.ToString().PadLeft(6, '0') + " " + _controller_output.ToString().PadLeft(6, '0')
                        + " " + _interface.ToString().PadLeft(6, '0') + " " + _pulseHighTime.ToString("X6").PadLeft(8, '0')
                        + " " + _pulseLowTime.ToString("X6").PadLeft(8, '0') + "\r\n";
            }
            else
            {
                //if (_lastCmd == "P")
                {
                    
                    _setPointData = Convert.ToInt32(message);
                    _forwardPower = _setPointData;
                    sendData = _setPointData + "\r\n";
                }

            }
            
            
            sendData += "*";

            OnWriteMessage(sendData);
        }

        //protected override void ProcessUnsplitMessage(byte[] message1)
        //{
        //    _cached.AddRange(message1);

        //    if (_cached[0] == 0x06)
        //        _cached.RemoveAt(0);

        //    if (_cached.Count < 3)
        //        return;

        //    byte[] msgIn = _cached.ToArray();

        //    _cached.Clear();

        //    List<byte> lstAck = new List<byte>();
        //    lstAck.Add(0x06);

             

        //    switch (msgIn[1])
        //    {
        //        case 1:
        //            IsOn = false;
        //            break;
        //        case 2:
        //            IsOn = true;
        //            break;
        //        case 3: //reg mode
        //            _mode = msgIn[2];
        //            break;
        //        case 8:
        //            _powerSetPoint = msgIn[2] + (msgIn[3] << 8);
        //            break;
        //        case 155://mode comm
        //            //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { 0x8 });
        //            break;
        //        case 162://status 
        //            //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { IsOn ? (byte)0x40 : (byte)0, 0, 0, 0 });
        //            break;
        //        case 164: //setpoint
        //            //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)_powerSetPoint, (byte)(_powerSetPoint >> 8), (byte)_mode });
        //            break;
        //        case 165: //forward
        //            int forward = (int)(_powerSetPoint * 0.8);
        //            //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)forward, (byte)(forward >> 8) });
        //            break;
        //        case 166: //reflect
        //            int reflect = (int)(_powerSetPoint * 0.2);
        //            //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)reflect, (byte)(reflect >> 8) });
        //            break;
        //        case 221://pin
        //            List<byte> pin = new List<byte>();
        //            for (int i = 0; i < 32; i++)
        //            {
        //                if (i == 20 && IsHalo)
        //                {
        //                    pin.Add(0x31);
        //                    continue;
        //                }
        //                pin.Add(0);
        //            }
        //            //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], pin.ToArray());
        //            break;
        //    }

        //    if (!IsContinueAck)
        //    {
        //        OnWriteMessage(lstAck.ToArray());
        //        //OnWriteMessage(response);
        //    }
        //    else
        //    {
        //        //lstAck.AddRange(response);
        //        OnWriteMessage(lstAck.ToArray());
        //    }


        //}
 
 

    }
}
