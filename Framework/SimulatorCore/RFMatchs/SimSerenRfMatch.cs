using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace MECF.Framework.Simulator.Core.RFMatchs
{
    class SimSerenRfMatch : SerialPortDeviceSimulator
    {
        public bool Failed { get; set; }

        public bool IsOn { get; set; }

        public bool IsHalo { get; set; }

        public bool IsContinueAck { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        public string ResultValue { get; set; }

        public SimSerenRfMatch(string port)
            : base(port, -1, "\r", ' ', true)
        {
            ResultValue = "";

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

        List<byte> _cached = new List<byte>();

        private int _mode;
        private int _powerSetPoint;

        private bool _isAuto;

        protected override void ProcessUnsplitMessage(string message)
        {
            if (message == "ALD\r")
            {
                _isAuto = true;
                OnWriteMessage("\r");
                return;
            }

            if (message == "MLD\r")
            {
                _isAuto = false;
                OnWriteMessage("\r");
                return;
            }

            if (message == "CPLP\r")
            {
                OnWriteMessage("666\r");
                return;
            }
            if (message == "QDP\r")
            {
                OnWriteMessage("111\r");
                return;
            }

            if (message == "QRP\r")
            {
                OnWriteMessage("222\r");
                return;
            }

            if (message == "PHS\r")
            {
                OnWriteMessage("333\r");
                return;
            }

            if (message == ("QAML\r"))
            {
                if (_isAuto)
                    OnWriteMessage("A\r");
                else
                {
                    OnWriteMessage("M\r");

                }
                return;
            }

            if (message == ("QAMT\r"))
            {
                IsOn = false;
                OnWriteMessage("M\r");
            }

            OnWriteMessage("\r");
            return;


        }

        protected override void ProcessUnsplitMessage(byte[] message1)
        {
            _cached.AddRange(message1);

            if (_cached[0] == 0x06)
                _cached.RemoveAt(0);

            if (_cached.Count < 3)
                return;

            byte[] msgIn = _cached.ToArray();

            _cached.Clear();

            List<byte> lstAck = new List<byte>();
            lstAck.Add(0x06);



            switch (msgIn[1])
            {
                case 1:
                    IsOn = false;
                    break;
                case 2:
                    IsOn = true;
                    break;
                case 3: //reg mode
                    _mode = msgIn[2];
                    break;
                case 8:
                    _powerSetPoint = msgIn[2] + (msgIn[3] << 8);
                    break;
                case 155://mode comm
                    //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { 0x8 });
                    break;
                case 162://status 
                    //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { IsOn ? (byte)0x40 : (byte)0, 0, 0, 0 });
                    break;
                case 164: //setpoint
                    //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)_powerSetPoint, (byte)(_powerSetPoint >> 8), (byte)_mode });
                    break;
                case 165: //forward
                    int forward = (int)(_powerSetPoint * 0.8);
                    //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)forward, (byte)(forward >> 8) });
                    break;
                case 166: //reflect
                    int reflect = (int)(_powerSetPoint * 0.2);
                    //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)reflect, (byte)(reflect >> 8) });
                    break;
                case 221://pin
                    List<byte> pin = new List<byte>();
                    for (int i = 0; i < 32; i++)
                    {
                        if (i == 20 && IsHalo)
                        {
                            pin.Add(0x31);
                            continue;
                        }
                        pin.Add(0);
                    }
                    //response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], pin.ToArray());
                    break;
            }

            if (!IsContinueAck)
            {
                OnWriteMessage(lstAck.ToArray());
                //OnWriteMessage(response);
            }
            else
            {
                //lstAck.AddRange(response);
                OnWriteMessage(lstAck.ToArray());
            }


        }



    }
}
