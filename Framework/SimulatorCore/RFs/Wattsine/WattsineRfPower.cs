using MECF.Framework.Common.Utilities;
using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.RFs
{
    public class WattsineRfPower: SerialPortDeviceSimulator
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

        public WattsineRfPower(string port)
            : base(port, -1, " ", ' ', false)
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

        //private int _mode;
        //private int _powerSetPoint;

        protected override void ProcessUnsplitMessage(byte[] message1)
        {
            _cached.AddRange(message1);

            if (_cached.Count < 3)
                return;

            byte[] msgIn = _cached.ToArray();

            _cached.Clear();

            //List<byte> lstAck = new List<byte>();
            //lstAck.Add(0x06);

            byte[] response = BuildMessage(msgIn[0], msgIn[1],new byte[] { msgIn[4],msgIn[5] },msgIn);

            OnWriteMessage(response);

            //if (!IsContinueAck)
            //{
            //    OnWriteMessage(lstAck.ToArray());
            //    OnWriteMessage(response);
            //}
            //else
            //{
            //    lstAck.AddRange(response);
            //    OnWriteMessage(lstAck.ToArray());
            //}


        }
        private static byte[] BuildMessage(byte address, byte command,byte[] readLength,byte[] msgIn)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(address);
            buffer.Add(command);

            if (command == 0x06) 
            {
                buffer.AddRange(msgIn.Skip(2).Take(4));
            }
            if (command == 0x03)
            {
                ushort len = (ushort)((readLength[0] * 256) + readLength[1]);


                buffer.Add((byte)len);

                for (int i = 0; i < len; i++)
                {
                    buffer.AddRange(new byte[] { 0x00, 0x00 });
                }

                //buffer.Add(CalcSum(buffer, buffer.Count));
            }
            var crc = Crc16.Crc16Ccitt(buffer.ToArray());
            buffer.Add((byte)(crc >> 8));
            buffer.Add((byte)(crc & 0xFF));
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
