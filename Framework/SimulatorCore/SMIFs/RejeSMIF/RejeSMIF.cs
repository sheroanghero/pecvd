using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.SMIFs.RejeSMIF
{
    public class RejeSMIF : SerialPortDeviceSimulator
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

        public RejeSMIF(string port)
            : base(port, -1, "\r", ' ', true)
        {
            ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

            //Failed = true;

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


        protected override void ProcessUnsplitMessage(string message)
        {
            string startchar = "s";
            string adrchar = "00";
            string endchar = "\r";
            string responseAck = "ACK:";
            string responseINF = "INF:";
            string responseNAK = "NAK";
            string responseABS = "ABS";
            string command = "";
            if (message.Contains("MOD") || message.Contains("MOV") || message.Contains("SET"))
            {
                if (message.Contains(":"))
                {
                    command = message.Split(':')[1].Replace(";", "").Replace("\r", "");
                }
                if (!Failed)
                {
                    OnWriteMessage(string.Format("{0}{1}{2}{3}{4}", startchar, adrchar, responseAck, command, endchar));


                    OnWriteMessage(string.Format("{0}{1}{2}{3}{4}", startchar, adrchar, responseINF, command, endchar));
                }
                else
                {
                    //OnWriteMessage(string.Format("{0}{1}{2}{3}/{4}{5}", startchar, adrchar, responseNAK, command, "CBUSY", endchar));
                    OnWriteMessage(string.Format("{0}{1}{2}{3}/{4}{5}", startchar, adrchar, responseABS, command, "ZDALM", endchar));
                }
            }
            else
            {
                command = message.Split(':')[1].Replace(";", "").Replace("\r", "");
                if (message.Contains("GET"))
                {
                    if (!Failed)
                    {
                        if (message.Contains("STATE"))
                        {
                            //OnWriteMessage(string.Format("{0}{1}{2}{3}/00001000000000000000;{4}", startchar, adrchar, responseAck, command, endchar));
                            OnWriteMessage(string.Format("{0}{1}{2}{3}/00000000000000000000;{4}", startchar, adrchar, responseAck, command, endchar));
                        }
                        if (message.Contains("VERSN"))
                        {
                            OnWriteMessage(string.Format("{0}{1}{2}{3}/Ver.19/12/1.0.0.01;{4}", startchar, adrchar, responseAck, command, endchar));
                        }
                        if (message.Contains("MAPRD"))
                        {
                            OnWriteMessage(string.Format("{0}{1}{2}{3}0000000000000000000000000;{4}", startchar, adrchar, responseAck, command, endchar));
                        }
                        if (message.Contains("WTY"))
                        {
                            OnWriteMessage(string.Format("{0}{1}{2}{3}/S25/P0000/M0000/N0000/G0000;{4}", startchar, adrchar, responseAck, command, endchar));
                        }
                        if (message.Contains("THICK"))
                        {
                            OnWriteMessage(string.Format("{0}{1}{2}{3}0000000000000000000000000;{4}", startchar, adrchar, responseAck, command, endchar));
                        }
                        if (message.Contains("JZPOS"))
                        {
                            OnWriteMessage(string.Format("{0}{1}{2}{3}/0;{4}", startchar, adrchar, responseAck, command, endchar));
                        }
                        if (message.Contains("HMPOS"))
                        {
                            OnWriteMessage(string.Format("{0}{1}{2}{3}/0;{4}", startchar, adrchar, responseAck, command, endchar));
                        }
                    }
                    else
                    {
                        OnWriteMessage(string.Format("{0}{1}{2}{3}/{4}{5}", startchar, adrchar, responseABS, command, "ZDALM", endchar));
                    }
                }
            }
        }
    }

}
