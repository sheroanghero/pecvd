using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Pumps.PfeifferHipace
{
    class SimHipaceTurboPump : SerialPortDeviceSimulator
    {
        public bool Failed { get; set; }

        public bool IsPumpOn { get; set; }

        public bool IsOverTemp { get; set; }

        public bool IsAtSpeed { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        public string ResultValue { get; set; }

        public SimHipaceTurboPump(string port)
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




        protected override void ProcessUnsplitMessage(string msg)
        {
            //if (!Failed)
            {
                if (msg == "0010030302=?101\r")
                {
                    //OnWriteMessage("0011030306000000014" + "\r" );

                    string text = "0011030306" + (Failed ? _rd.Next(0, 999).ToString("D6") : "000000");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }

                if (msg == "0010039802=?115\r")
                {
                    string text = "0011039806" + _rd.Next(0, 999999).ToString("D6");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }

                if (msg == "0010034602=?108\r")
                {
                    string text = "0011034606" + _rd.Next(0, 999999).ToString("D6");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }
                if (msg == "0011001006111111015\r")
                {
                    lock (_locker)
                    {
                        if (!IsPumpOn)
                        {
                            IsPumpOn = true;
                            IsAtSpeed = false;
                            _timer.Restart();
                        }
                    }


                    OnWriteMessage("0011001006111111015" + "\r");
                    return;
                }

                if (msg == "0011001006000000009\r")
                {
                    lock (_locker)
                    {
                        if (IsPumpOn)
                        {
                            IsPumpOn = false;
                            IsAtSpeed = false;

                            _timer.Restart();
                        }
                    }

                    OnWriteMessage("0011001006000000009" + "\r");
                    return;
                }

                if (msg == "0010001002=?096\r")
                {
                    string text = "0011001006" + (IsPumpOn ? "111111" : "000000");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }

                if (msg == "0010030502=?103\r")
                {
                    string text = "0011030506" + (IsOverTemp ? "111111" : "000000");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }


                if (msg == "0010030602=?104\r")
                {
                    string text = "0011030606" + (IsAtSpeed ? "111111" : "000000");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }


                if (msg == "0010030702=?105\r")
                {
                    string text = "0011030706" + (IsAtSpeed ? "000000" : "111111");

                    int sum = 0;
                    foreach (var item in text)
                    {
                        sum += (int)item;
                    }

                    sum = sum % 256;

                    OnWriteMessage(text + sum.ToString("D3") + "\r");
                    return;
                }
            }

        }




    }

}
