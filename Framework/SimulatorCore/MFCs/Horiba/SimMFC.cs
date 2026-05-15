using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.MFCs.Horiba
{
    class SimMFC : SerialPortDeviceSimulator
    {
        private Dictionary<int, List<byte>> _flowDic = new Dictionary<int, List<byte>>();

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        public string ResultValue { get; set; }

        public SimMFC(string port) 
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
            //if (!Failed)
            {
                var tmp = System.Text.Encoding.ASCII.GetBytes(msg).ToList();
                var index = tmp.IndexOf(0x03);
                if(tmp.Count > index + 2)
                {
                    byte[] datas = new byte[index + 2];
                    tmp.CopyTo(0, datas,0, index + 2);
                    tmp.RemoveRange(0, index + 2);
                    
                    if (datas.Length > 7)
                    {
                        //RFV
                        //40 30 31 02 52 46 56 03 71
                        if (datas[4] == 0x52 && datas[5] == 0x46 && datas[6] == 0x56)
                        {
                            var address = datas[1] + (datas[2] << 16);
                            if(_flowDic.ContainsKey(address))
                            {
                                List<byte> sendback = new List<byte>()
                                {
                                    0x02,
                                };
                                var values = _flowDic[address];
                                values.ForEach(x=> sendback.Add(x));
                                int sum = 0;
                                values.ForEach(x => sum += x);
                                sendback.Add(0x03);
                                sendback.Add((byte)((sum + 0x03) % 128));
                                OnWriteMessage(sendback.ToArray());
                            }
                            else
                            {
                                //02 30 2E 30 32 34 37 31 32 31 03 42
                                List<byte> sendback = new List<byte>()
                                {
                                    0x02,
                                    0x30,
                                    0x2e,
                                    0x30,
                                    0x32,
                                    0x34,
                                    0x37,
                                    0x31,
                                    0x32,
                                    0x31,
                                    0x03,
                                };
                                int sum = 0;
                                sendback.ForEach(x => sum += (int)x);
                                sendback.Add((byte)((sum - (int)sendback[0]) % 128));
                                OnWriteMessage(sendback.ToArray());
                            }
                        }

                        //AFC   9sccm
                        //40 30 31 02 41 46 43 30 30 30 39 2c 42 03 04
                        if (datas.Length >= 12 && datas[4] == 0x41 && datas[5] == 0x46 && datas[6] == 0x43)
                        {
                            var address = datas[1] + (datas[2] << 16);
                            int i1 = datas.ToList().IndexOf(0x43);
                            int i2 = datas.ToList().IndexOf(0x2c);
                            int valueLength = i2 - i1 - 1;
                            byte[] values = new byte[valueLength];
                            Array.Copy(datas, 7, values, 0, values.Length);
                            _flowDic[address] = values.ToList();
                            List<byte> sendback = new List<byte>()
                        {
                            0x02,
                            0x4f,
                            0x4b,
                            0x03,
                        };
                            int sum = 0;
                            sendback.ForEach(x => sum += (int)x);
                            sendback.Add((byte)((sum - (int)sendback[0]) % 128));
                            OnWriteMessage(sendback.ToArray());
                        }

                        //rfs
                        //40 30 31 02 52 46 53 03 6E
                        //02 31 30 30 2C 42 03 02
                        if (datas[4] == 0x52 && datas[5] == 0x46 && datas[6] == 0x53)
                        {
                            List<byte> sendback = new List<byte>()
                        {
                            0x02,
                            0x31,
                            0x30,
                            0x30,
                            0x2c,
                            0x42,
                            0x03,
                        };
                            int sum = 0;
                            sendback.ForEach(x => sum += (int)x);
                            sendback.Add((byte)((sum - (int)sendback[0]) % 128));
                            OnWriteMessage(sendback.ToArray());
                        }

                        //设为数字模式
                        //’！+REVERSE
                        //40 30 31 02 27 21 2B 52 45 56 45 52 53 45 03 12
                        if (datas[4] == 0x27 && datas[5] == 0x21 && datas[6] == 0x2b)
                        {
                            List<byte> sendback = new List<byte>()
                        {
                            0x02,
                            0x4f,
                            0x4b,
                            0x03,
                        };
                            int sum = 0;
                            sendback.ForEach(x => sum += (int)x);
                            sendback.Add((byte)((sum - (int)sendback[0]) % 128));
                            OnWriteMessage(sendback.ToArray());
                        }
                    }

                    
                }
            }
 
        }
    }
}
