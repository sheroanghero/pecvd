using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Aitex.Core.Util;

namespace MECF.Framework.Simulator.Core.Temperatures
{
    class SimOmronEJ1 : SerialPortDeviceSimulator
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

        Dictionary<int, Dictionary<int, int>> _setpoint = new Dictionary<int, Dictionary<int, int>>();

        List<byte> _cached = new List<byte>();


        List<byte> _buffer = new List<byte>();




        public SimOmronEJ1(string port)
            : base(port, -1, "", ' ', false)
        {
            ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();


            _setpoint[1] = new Dictionary<int, int>();
            _setpoint[2] = new Dictionary<int, int>();
            _setpoint[3] = new Dictionary<int, int>();
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

        protected override void ProcessUnsplitMessage(byte[] message)
        {
            _buffer.AddRange(message);

            if (_buffer.Count < 7)
            {
                return;
            }


            message = _buffer.ToArray();

            _buffer.Clear();
            _cached.Clear();

            int slave = message[0];
            int cmd = message[1];


            if (cmd == 0x06)
            {
                if (message[3] == 0xC0)
                {
                    //_setpoint[slave][1] = 
                    //{
                    //    _setpoint1[1] = 
                    //} 
                }
            }

            if (cmd == 0x03)
            {
                _cached.Add((byte)slave);
                _cached.Add(message[1]);

                _cached.Add(32);

                

                for (int i = 0; i < 16; i++)
                {
                    var data = 0;

                    if (i %3 == 0)
                    {
                        data = (50 + slave * 10 + i) * 10;

                    }
                    else if ((i-1)%3 ==0)
                    {

                    }else if ((i - 2) % 3 == 0)
                    {
                        byte state = 0;
                        state = Converter.SetBit(state, 0, true);

                    }
                    else
                    {
                        data = 0;

                    }

                    byte dataH = Convert.ToByte((data >> 8) & 0xff);
                    byte dataL = Convert.ToByte(data & 0xff);
                    _cached.Add(dataH);
                    _cached.Add(dataL);
                }

                _cached.Add(0);
                _cached.Add(0);

                OnWriteMessage(_cached.ToArray());

                return;
            }


            OnWriteMessage(message);


            return;
        }
    }
}
