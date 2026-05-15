using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.RFs.AE
{
    class SimAeRfPower : SerialPortDeviceSimulator
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

        public SimAeRfPower(string port)
            : base(port, -1, "\r", ' ', false)
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

            byte[] response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { 0 });

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
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { 0x8 });
                    break;
                case 162://status 
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { IsOn ? (byte)0x40 : (byte)0, 0, 0, 0 });
                    break;
                case 164: //setpoint
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)_powerSetPoint, (byte)(_powerSetPoint >> 8), (byte)_mode });
                    break;
                case 165: //forward
                    int forward = (int)(_powerSetPoint * 0.8);
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)forward, (byte)(forward >> 8) });
                    break;
                case 166: //reflect
                    int reflect = (int)(_powerSetPoint * 0.2);
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)reflect, (byte)(reflect >> 8) });
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
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], pin.ToArray());
                    break;
            }

            if (!IsContinueAck)
            {
                OnWriteMessage(lstAck.ToArray());
                OnWriteMessage(response);
            }
            else
            {
                lstAck.AddRange(response);
                OnWriteMessage(lstAck.ToArray());
            }


        }
        private static byte[] BuildMessage(byte address, byte command, byte[] data)
        {
            List<byte> buffer = new List<byte>();

            if (data.Length < 7)
            {
                buffer.Add((byte)((address << 3) + (data == null ? 0 : data.Length)));
                buffer.Add(command);
            }
            else
            {
                buffer.Add((byte)((address << 3) + 7));
                buffer.Add(command);
                buffer.Add((byte)data.Length);
            }



            if (data != null && data.Length > 0)
            {
                buffer.AddRange(data);
            }

            buffer.Add(CalcSum(buffer, buffer.Count));

            return buffer.ToArray();
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

    }
}
