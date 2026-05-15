using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.LoadPorts
{
    class OmronBarcodeReaderSimulator : SerialPortDeviceSimulator
    {
        public bool Failed { get; set; }
        public string ResultValue { get; set; }

        public OmronBarcodeReaderSimulator(string port) 
            : base(port, -1, "\r", ' ')
        {
            ResultValue = "";
        }

        protected override void ProcessUnsplitMessage(string msg)
        {
            if (!Failed)
            {
                //OnWriteMessage("\n" +  "BR" + "\r");

                OnWriteMessage(string.IsNullOrEmpty(ResultValue) ?  GenerateRandomNumber(6)+"\r" : ResultValue + "\r");
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
