using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.ThrottleValves.KITZ
{
    class SimThrottleValve : SerialPortDeviceSimulator
    {
        private Dictionary<int, List<byte>> _flowDic = new Dictionary<int, List<byte>>();

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        public string ResultValue { get; set; }

        private string _positionValue = 0.ToString();
        private string _pressureValue = 0.ToString();
        private string _controlMode = "PRESS";
        private string _accessMode = "LOCAL";
        private string _warningInformation = "0000000000000000";
        private string _cycleCounter= "1";

        public SimThrottleValve(string port) 
            : base(port, -1, "\r", ' ')
        {
            ResultValue = "";

            _tick =  new System.Timers.Timer();
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
 
        protected override void ProcessUnsplitMessage(string msg)
        {
            msg = msg.Replace("\r", "").Replace("\n", "");
            string[] datas = Regex.Split(msg, ":");
            if(datas.Length > 1)
            {                
                switch(datas[0])
                {
                    case "REMOTE":
                        {
                            _accessMode = "REMOTE";
                            OnWriteMessage(msg + "\r");
                        }
                        break;
                    case "C":
                        {
                            _controlMode = "CLOSE";
                            OnWriteMessage(msg + "\r");
                        }
                        break;
                    case "O":
                        {
                            _controlMode = "OPEN";
                            OnWriteMessage(msg + "\r");
                        }
                        break;
                    case "POS":
                        {
                            _controlMode = "POS";
                            if (!string.IsNullOrEmpty(datas[1]))
                            {
                                _positionValue = datas[1];
                            }
                            OnWriteMessage(msg + "\r");
                        }
                        break;
                    case "PRS":
                        {
                            _controlMode = "PRESS";
                            if (!string.IsNullOrEmpty(datas[1]))
                            {
                                var array = datas[1].Split(',');
                                _pressureValue = array[array.Length - 1];
                            }
                            OnWriteMessage(msg + "\r");
                        }
                        break;
                    case "CAL":
                        {
                            _controlMode = "CALIB";
                            OnWriteMessage(msg + "\r");
                        }
                        break;
                    case "prs":
                        {
                            OnWriteMessage($"prs:{_pressureValue}" + "\r");
                        }
                        break;
                    case "pos":
                        {
                            OnWriteMessage($"pos:{_positionValue}" + "\r");
                        }
                        break;
                    case "sts":
                        {
                            OnWriteMessage($"sts:{_accessMode},{_controlMode},{_warningInformation},{_cycleCounter}" + "\r");

                            if (_controlMode == "CALIB")
                                _controlMode = "CLOSE";
                        }
                        break;
                }
            }
        }
    }
}
