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
    class SimDxkdpPower : SerialPortDeviceSimulator
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

        public SimDxkdpPower(string port)
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

        private byte _vmss = 2;
        private byte _cmss = 3;

        private UInt16 _voltSetPoint;  //未除以 Math.Power(10,_vmss)，不做运算，直接用
        private UInt16 _currentSetPoint; //未除以 Math.Power(10,_cmss)，不做运算，直接用

        private UInt16 _setPolarity; //

        private UInt16 _volt;  //未除以 Math.Power(10,_vmss)，不做运算，直接用
        private UInt16 _current; //未除以 Math.Power(10,_cmss)，不做运算，直接用

        private UInt16 _maxVolt = 10000;  //未除以 Math.Power(10,_vmss)，不做运算，直接用
        private UInt16 _maxCurrent = 10000; //未除以 Math.Power(10,_cmss)，不做运算，直接用

        protected override void ProcessUnsplitMessage(byte[] message1)
        {
            _cached.AddRange(message1);

            if (_cached[0] == 0x06)
                _cached.RemoveAt(0);
            while(_cached.Count>1 && _cached[0] != 0xAA)
            {
                _cached.RemoveAt(0);
            }

            if (_cached.Count < 5 || _cached.Count < (5 + _cached[3]))
                return;

            byte[] msgIn = _cached.ToArray();

            _cached.Clear();

            List<byte> lstAck = new List<byte>();
            lstAck.Add(0x06);

            byte[] response = null;
            List<byte> pin = new List<byte>();

            switch (msgIn[2])
            {
                case 0x20:
                    if (msgIn[4] == 0)
                        IsOn = false;
                    else
                        IsOn = true;
                    break;
                case 0x21:
                    _voltSetPoint = (UInt16)((msgIn[5] << 8) + msgIn[4]);
                    _volt = _voltSetPoint;
                    break;
                case 0x22: //
                    _currentSetPoint = (UInt16)((msgIn[5] << 8) + msgIn[4]);
                    _current = _currentSetPoint;
                    break;
                case 0x23:
                    _voltSetPoint = (UInt16)((msgIn[5] << 8) + msgIn[4]);
                    _currentSetPoint = (UInt16)((msgIn[7] << 8) + msgIn[6]);

                    _volt = _voltSetPoint;
                    _current = _currentSetPoint;
                    break;
                case 0x24://
                    _setPolarity = (UInt16)((msgIn[5] << 8) + msgIn[4]);
                    break;
                case 0x25://

                    //response = BuildMessage(msgIn[1], msgIn[2], pin.ToArray());
                    break;
                case 0x26: //
                    byte[] actualVolt = BitConverter.GetBytes(_volt);
                    byte[] actualCurrent = BitConverter.GetBytes(_current);

                    pin.AddRange(actualVolt);
                    pin.AddRange(actualCurrent);
                    pin.Add(0x00);
                    response = BuildMessage(msgIn[1], msgIn[2], pin.ToArray());
                    break;
                case 0x27: //
                    //response = BuildMessage(msgIn[1], msgIn[2], pin.ToArray());
                    break;
                case 0x28: //

                    byte state = (byte)(IsOn ? 0x01 : 0x00);
                    byte[] setVolt = BitConverter.GetBytes(_voltSetPoint);
                    byte[] setCurrent = BitConverter.GetBytes(_currentSetPoint);
                    pin.Add(state);
                    pin.AddRange(setVolt);
                    pin.AddRange(setCurrent);

                    response = BuildMessage(msgIn[1], msgIn[2], pin.ToArray());
                    break;
                case 0x29://
                    break;
                case 0x2A://
                    //response = BuildMessage(msgIn[1], msgIn[2], pin.ToArray());
                    break;
                case 0x2B://
                    
                    byte[] maxVolt = BitConverter.GetBytes(_maxVolt);
                    byte[] maxCurrent = BitConverter.GetBytes(_maxCurrent);
                    pin.Add(_vmss);
                    pin.Add(_cmss);
                    pin.Add(0x00);
                    pin.Add(0x00);
                    pin.Add(0x00);
                    pin.Add(0x00);

                    pin.AddRange(maxVolt);
                    pin.AddRange(maxCurrent);
                    pin.Add(0x00);
                    pin.Add(0x00);
                    pin.Add(0x00);
                    pin.Add(0x00);

                    response = BuildMessage(msgIn[1], msgIn[2], pin.ToArray());
                    break;
                case 0x30://

                    break;
            }
            
            if (response != null)
            {
                OnWriteMessage(response);
            }
            else
            {
                OnWriteMessage(lstAck.ToArray());
            }
        }
        private static byte[] BuildMessage(byte address, byte command, byte[] data)
        {
            List<byte> buffer = new List<byte>();

            if (data!=null && data.Length>0)
            {
                buffer.Add(0xAA);
                buffer.Add(address);
                buffer.Add(command);
                buffer.Add((byte)data.Length);
            }

            if (data != null && data.Length > 0)
            {
                buffer.AddRange(data);
            }

            buffer.Add(CalcSum(buffer.GetRange(1, buffer.Count - 1)));

            return buffer.ToArray();
        }
        private static byte CalcSum(List<byte> data)
        {
            byte ret = 0x00;
            for (var i = 0; i < data.Count; i++)
            {
                ret += data[i];
            }
            return ret;
        }

    }
}
