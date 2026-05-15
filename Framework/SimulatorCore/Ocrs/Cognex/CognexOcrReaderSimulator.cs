using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Aligners
{
    public class CognexOcrReaderSimulator : SocketDeviceSimulator
    {
        Random _rd = new Random();

        public CognexOcrReaderSimulator()
            : base(23, -1, "\r\n", ' ')
        {

        }

        public CognexOcrReaderSimulator(int port)
            : base(port, -1, "\r\n", ' ')
        {

        }

        int GetSleepTime()
        {
            return 50;
        }
        private int _slotID = 0;
        private int GeneratorSlotID()
        {
            int ret;
            if (_slotID < 1)
            {
                _slotID = 1;

            }
            if (_slotID > 25)
            {
                _slotID = 1;

            }
            ret = _slotID;
            _slotID++;
            return ret;
        }
        private int iCount = 0;
        private bool isSucess = true;
        protected override void ProcessUnsplitMessage(string msg)
        {
            List<string> result = new List<string>();
            var isMultipleWrite = false;
            switch (msg)
            {
                case "admin":
                    result.Add("Password:");
                    break;
                case "":
                    result.Add("User Logged In");
                    break;
                case "SM\"READ\"0 ":
                    // [PNGHE001MXC2,400.000,1.000]. 
                    isMultipleWrite = true;
                    //Thread.Sleep(2000);

                    if (iCount  <= 4)
                    {
                        result.Add("-2");
                        iCount++;
                    }
                    else
                    {
                        result.Add("1");

                        //result.Add($"[****{GeneratorSlotID():D2}EF,{398:F3}]");
                        if (iCount % 2 == 0)
                            result.Add($"[ABCD{GeneratorSlotID():D2}EF123456789,{398:F3}]");
                        else
                            result.Add($"[ABCD{GeneratorSlotID():D2}EF123456789,{398:F3}]");

                        iCount = 0;
                    }




                    //    if (isSucess)
                    //{
                    //    result.Add("1");

                    //    //result.Add($"[****{GeneratorSlotID():D2}EF,{398:F3}]");
                    //    if (++iCount % 2 == 0)
                    //        result.Add($"[ABCD{GeneratorSlotID():D2}EF123456789,{398:F3}]");
                    //    else
                    //        result.Add($"[ABCD{GeneratorSlotID():D2}EF123456789,{398:F3}]");
                    //}
                    //else
                    //{
                        
                    //}
                    //isSucess = !isSucess;
                    foreach (var res in result)
                    {                        
                        OnWriteMessage(res);
                        //Thread.Sleep(10000);
                    }
                    return; 
                case "Get Filelist":
                    isMultipleWrite = true;
                    result.AddRange(GenerateFilelist());
                    break;
                case "RI":
                    
                    if (!File.Exists("C:\\Image.bmp"))
                        File.Create("C:\\Image.bmp");
                    Thread.Sleep(2000);
                    Bitmap bp = new Bitmap("C:\\Image.bmp");
                    MemoryStream ms = new MemoryStream();
                    bp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    byte[] bytes = ms.GetBuffer();  //byte[]   bytes=   ms.ToArray(); 这两句都可以，至于区别么，下面有解释
                    ms.Close();

                    string strBytes = BitConverter.ToString(bytes,0).Replace("-","").ToLower();
                    


                    isMultipleWrite = true;

                    result.Add($"1\r\n{bytes.Length *2}");

                    int icount = strBytes.Length / 1280 ;
                    for(int i=0;i<=icount;i++)
                    {
                        if (i == icount)
                        {
                            result.Add(strBytes.Substring(i * 128));
                        }
                        else
                            result.Add(strBytes.Substring(i * 128, 128));
                    }

                    foreach (var res in result)
                    {                        
                        OnWriteMessage(res);
                    }
                    return;

                default:
                    //if(_failTime++ <2)
                    //{
                    //    result.Add("-2");
                    //}
                    //else
                    //{
                    //    if(_failTime>6)
                    //        _failTime = 0;
                    //    result.Add("1");
                    //}
                    result.Add("1");
                    //Thread.Sleep(1000);
                    break;
            }

            if (isMultipleWrite)
            {
                //Thread.Sleep(GetSleepTime());
                foreach (var res in result)
                {
                    Thread.Sleep(1);
                    OnWriteMessage(res);
                }
            }
            else
            {
                Task.Run(() =>
                {
                    //Thread.Sleep(GetSleepTime());

                    OnWriteMessage(result.FirstOrDefault());
                });
            }
        }

        //private int _failTime = 0;

        private static char[] constant =
        {
            '0','1','2','3','4','5','6','7','8','9',
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z'
        };
        public static string GenerateRandomNumber(int Length)
        {
            System.Text.StringBuilder newRandom = new System.Text.StringBuilder(62);
            Random rd = new Random();
            for (int i = 0; i < Length; i++)
            {
                newRandom.Append(constant[rd.Next(62)]);
            }
            return newRandom.ToString();
        }

        public static List<string> GenerateFilelist()
        {
            var newRandom = new System.Text.StringBuilder(62);
            var rd = new Random();
            var quantity = 50;
            var listStr = new List<string>();
            listStr.Add("1");
            listStr.Add(quantity.ToString());
            for (var i = 0; i < quantity; i++)
            {
                for (int j = 0; j < rd.Next(2, 8); j++)
                {
                    newRandom.Append(constant[rd.Next(62)]);
                }
                listStr.Add(newRandom.Append(".job").ToString());
                newRandom.Clear();
            }

            return listStr;
        }
    }
}