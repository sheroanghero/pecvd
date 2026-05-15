using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.RFMatchs.STeam
{
    public class STeamRFMatch : SerialPortDeviceSimulator
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

        public STeamRFMatch(string port)
            : base(port, -1, "\r\n", ' ', false)
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

            string command = "";

            if (_cached[0] == 0x80)
            {
                switch (_cached[1])
                {
                    case 0x1C:
                        if (_cached[_cached.Count - 1] == 0x39)
                        {
                            command = "80,1C,39,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x38)
                        {
                            command = "80, 1C, 38,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x07)
                        {
                            command = "80,1C,07,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x4B)
                        {
                            command = "80,1C,4B,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x06)
                        {
                            command = "80,1C,06,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x35)
                        {
                            command = "80,1C,35,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x46)
                        {
                            command = "80,1C,46,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x47)
                        {
                            command = "80,1C,47,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x48)
                        {
                            command = "80,1C,48,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x49)
                        {
                            command = "80,1C,49,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x4A)
                        {
                            command = "80,1C,4A,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x56)
                        {
                            command = "80,1C,56,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x5C)
                        {
                            command = "80,1C,5C,00,80,04";
                        }
                        if (_cached[_cached.Count - 1] == 0x60)
                        {
                            command = "80,1C,60,00,80,04";
                        }

                        break;
                    case 0x11:
                        command = "80,1C,11,00,80,04,80,1C,20,00,09,38,05,FE,00,D6,00,F8,04,B8,AC,A0,0E,7B,03,89,FF,17,0A,23,06,00,00,77,00,F0,80,10";
                        break;
                    case 0x39:
                        command = "80,1C,38,00,80,04";
                        break;
                    case 0x45:
                        command = "80,1C,45,00,80,04";
                        break;
                    case 0x4A:
                        command = "80,1C,2D,00,00,01,02,A0,0E,79,00,59,80,10";
                        break;
                    case 0x55:
                        command = "80,1C,34,00,09,26,05,FE,00,FF,D6,00,F8,04,B8,AC,A0,0C,7B,03,89,FF,00,00,01,02,A9,0F,77,00,81,80,10";
                        break;
                    case 0x57:
                        command = "80,1C,34,00,09,2B,05,FE,00,FF,DF,00,F1,04,62,D8,9F,0E,7D,03,96,FF,AC,0D, 84,08,00,00,77,00,F7,80,10,80,1C,30,17,0A,23,08,00,00,77,80,80,73,80,10";
                        break;
                }

            }
            _cached.Clear();

            OnWriteMessage(ToByteArray(command));

        }

        public static byte SetBits(byte _word, byte value, int offset)
        {
            byte mask_1 = (byte)(value << offset);
            byte mask_2 = (byte)(~mask_1 & _word);
            return (byte)(mask_1 | mask_2);
        }

        public void ResultData()
        {
            string msg = "Clk=50736732 HST=20 HER=16 MHz=915 Mag=0.928 Deg=34.127 Mgd=NONE Dgd=NONE Pi=7.376E3 Pr = 6.349E3 T = 25.1 M1 = 5000 M2 = 5000 M3 = 0 MS1 = 119 MS2 = 0 END\r\n";
            OnWriteMessage(msg);
        }

        protected static byte[] ToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }
    }
}
