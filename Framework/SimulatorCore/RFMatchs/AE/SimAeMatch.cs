using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.RFMatchs.AE
{
    class SimAeMatch : SerialPortDeviceSimulator
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

        public SimAeMatch(string port)
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

        private int _mode1;
        private int _mode2;
        private bool _enablePreset;
        private byte _presetNumber;
        private int _load1;
        private int _load2;
        private int _tune1;
        private int _tune2;

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
                case 91://mode comm
                    _presetNumber = msgIn[4];
                    break;

                case 92: //presets
                    break;
                case 93: // auto mode
                    if (msgIn[2] == 1)
                    {
                        _mode1 = msgIn[4];
                    }
                    else
                    {
                        _mode2 = msgIn[4];
                    }

                    break;
                case 94:
                    _enablePreset = msgIn[4] == 1;
                    break;
                case 98: //presets
                    break;
                case 112: //load
                    if (msgIn[2] == 1)
                    {
                        _load1 = msgIn[4] + (msgIn[5] << 8);
                    }
                    else
                    {
                        _load2 = msgIn[4] + (msgIn[5] << 8);
                    }
                    break;
                case 122: //tune
                    if (msgIn[2] == 1)
                    {
                        _tune1 = msgIn[4] + (msgIn[5] << 8);
                    }
                    else
                    {
                        _tune2 = msgIn[4] + (msgIn[5] << 8);
                    }
                    break;
                case 161://preset  
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)_presetNumber, (byte)(_presetNumber >> 8) });
                    break;

                case 219://all data
                    List<byte> allData = new List<byte>();
                    for (int i = 0; i < 88; i++)
                    {
                        allData.Add(0);
                    }

                    allData[4] = (byte)_load1;
                    allData[5] = (byte)(_load1 >> 8);

                    allData[6] = (byte)_tune1;
                    allData[7] = (byte)(_tune1 >> 8);

                    allData[12] = (byte)_load2;
                    allData[13] = (byte)(_load2 >> 8);

                    allData[14] = (byte)_tune2;
                    allData[15] = (byte)(_tune2 >> 8);

                    int _peak = _rd.Next(6000, 7000);
                    allData[20] = (byte)_peak;
                    allData[21] = (byte)(_peak >> 8);

                    int _dc = _rd.Next(3000, 4000);
                    allData[22] = (byte)_dc;
                    allData[23] = (byte)(_dc >> 8);

                    allData[1] = SetBits(allData[1], _mode1 == 1 ? (byte)1 : (byte)0, 4);
                    allData[1] = SetBits(allData[1], _mode2 == 1 ? (byte)1 : (byte)0, 6);

                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], allData.ToArray());
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

        public static byte SetBits(byte _word, byte value, int offset)
        {
            byte mask_1 = (byte)(value << offset);
            byte mask_2 = (byte)(~mask_1 & _word);
            return (byte)(mask_1 | mask_2);
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
