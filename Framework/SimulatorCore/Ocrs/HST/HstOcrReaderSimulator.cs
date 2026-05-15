using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Aligners
{
 
    public class HstOcrReaderSimulator : SocketDeviceSimulator
    {
        Random _rd = new Random();

        public HstOcrReaderSimulator( )
            : base(23, -1, "\r\n", ' ')
        {

        }

        public HstOcrReaderSimulator(int port)
            : base(port, -1, "\r\n", ' ')
        {
 
        }

        int GetSleepTime()
        {
            return 1000;
        }
        public bool ResultOK { get; set; } = true;
        protected override void ProcessUnsplitMessage(string msg)
        {
            string result = ResultOK?"1":"-2";

            OnWriteMessage(result);

 
            if (msg.StartsWith("SM"))
            {
                result = string.Format("{0},{1:F2},{2:F2}", "WAFERID123", _rd.Next(80, 100), 1);

                Task.Run(() =>
                {
                    Thread.Sleep(GetSleepTime());

                    OnWriteMessage(result);
                });


            }
        }

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
    }
}

