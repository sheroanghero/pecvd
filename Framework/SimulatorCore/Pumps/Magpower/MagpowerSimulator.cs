using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Pumps.Magpower
{
    class MagpowerSimulator : SerialPortDeviceSimulator
    {
        public bool Failed { get; set; }

        public bool IsPumpOn { get; set; }

        public bool IsNormalSpeed { get; set; }

        public bool IsOverTemp { get; set; }

        public bool IsAtSpeed { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        public string ResultValue { get; set; }

        public MagpowerSimulator(string port)
            : base(port, -1, "\r", ' ')
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

        private Byte[] GetBytesFromBinaryString(String binary)
        {
            var list = new List<Byte>();

            for (int i = 0; i < binary.Length; i += 8)
            {
                String t = binary.Substring(i, 8);

                list.Add(Convert.ToByte(t, 2));
            }

            return list.ToArray();
        }
        private string GetCharsFormFromBinaryString(String binary)
        {
            return Encoding.ASCII.GetString(GetBytesFromBinaryString(binary));
        }

        private int _speed = 1200;
        int iStep = 0;
        protected override void ProcessUnsplitMessage(string msg)
        {
            //if (!Failed)
            {
                iStep++;

                if (msg.Contains("TMPON"))
                {
                    lock (_locker)
                    {
                        if (!IsPumpOn)
                        {
                            IsPumpOn = true;
                            IsAtSpeed = false;
                            _timer.Restart();

                            OnWriteMessage("#001,OK\r");
                        }
                        else
                        {
                            OnWriteMessage("#001,Err3\r");
                        }
                    }
                    return;
                }

                if (msg.Contains("TMPOFF"))
                {
                    lock (_locker)
                    {
                        if (IsPumpOn)
                        {
                            IsPumpOn = false;
                            IsAtSpeed = false;

                            _timer.Restart();

                            OnWriteMessage("#001,OK\r");

                        }
                        else
                        {
                            OnWriteMessage("#001,Err3\r");
                        }
                    }

                    return;
                }

                if (msg.Contains("NSP"))
                {
                    lock (_locker)
                    {
                        if (!IsNormalSpeed)
                        {
                            IsNormalSpeed = true;
                            _timer.Restart();

                            OnWriteMessage("#001,OK\r");
                        }
                        else
                        {
                            OnWriteMessage("#001,Err3\r");
                        }
                    }
                    return;
                }

                if (msg.Contains("SBY"))
                {
                    lock (_locker)
                    {
                        if (IsNormalSpeed)
                        {
                            IsNormalSpeed = false;
                            _timer.Restart();

                            OnWriteMessage("#001,OK\r");
                        }
                        else
                        {
                            OnWriteMessage("#001,Err3\r");
                        }
                    }
                    return;
                }
                if (msg.Contains("DEF"))
                {
                    lock (_locker)
                    {
                        _timer.Restart();
                        string error = iStep % 10 < 5 ? "Err2,Err3" : "OK";
                        OnWriteMessage($"#001,{error}\r");
                    }
                    return;
                }

                if (msg.Contains("SPD"))
                {
                    lock (_locker)
                    {
                        _timer.Restart();

                        string sSpeed = _speed.ToString("D6");
                        OnWriteMessage($"#001,{sSpeed} rpm\r");

                        _speed++;
                        if(_speed > 1210)
                        {
                            _speed = 1200;
                        }
                    }
                    return;
                }

                if (msg.Contains("STA"))
                {
                    lock (_locker)
                    {
                        _timer.Restart();

                        string pumpStatus1 = GetCharsFormFromBinaryString("00101011");
                        string pumpStatus2 = GetCharsFormFromBinaryString("00000111");
                        string pumpStatus = iStep % 10 <5  ? pumpStatus2 : pumpStatus1;
                        string pumpTemp = iStep % 10 < 5 ? "025" : "030";
                        OnWriteMessage($"#001,s{pumpStatus}s,rrrrr,vvv,www,xxx,yyy,zzz,aa,bbbbb,{pumpTemp},ddd,ggggggggggggggggggggggggg\r");

                    }
                    return;
                }


                if (msg.Contains("SEL10"))
                {
                    lock (_locker)
                    {
                        _timer.Restart();

                        string r = iStep % 10 < 5 ? "1" : "0";
                        OnWriteMessage($"#001,a,u,1,b,{r}\r");

                    }
                    return;
                }


            }

        }




    }

}
