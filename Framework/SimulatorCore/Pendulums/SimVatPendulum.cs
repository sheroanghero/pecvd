using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Pendulums
{
    class SimVatPendulum : SerialPortDeviceSimulator
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

        public SimVatPendulum(string port)
            : base(port, -1, "\r\n", ' ', true)
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

        private bool _isAuto;

        protected override void ProcessUnsplitMessage(string message)
        {
            if (message == "i:76\r\n")
            {
                _isAuto = true;
                OnWriteMessage("i:7600010000000000040\r\n");
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

            OnWriteMessage(message);
            return;


        }

 

    }
}
