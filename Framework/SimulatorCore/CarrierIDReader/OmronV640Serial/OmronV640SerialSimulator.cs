using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.LoadPorts
{
    class OmronV640SerialSimulator : SerialPortDeviceSimulator
    {
        public bool Failed { get; set; }
        public string ResultValue { get; set; }
        private string _rfid = "";
        public OmronV640SerialSimulator(string port) 
            : base(port, -1, "\r", ' ')
        {
            ResultValue = "";
            _rfid =
                "004346303533302020000000000000000000000000000000002020202020202020000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

        }

        protected override void ProcessUnsplitMessage(string msg)
        {
            string type = msg.Substring(0, 4);
            if (type == "0100") //read
            {
                if (_rfid.TrimStart('0').Length == 0)
                    _rfid = GenerateRandomNumber(16);
                _rfid = ASCII2HEX(GenerateRandomNumber(4));

                OnWriteMessage("00" + _rfid+"\r");
                //OnWriteMessage("{0}\r" + _rfid);

            }
            else if (type == "0200") //write
            {
                _rfid = msg.Substring(12);

                OnWriteMessage("00\r");
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
            return newRandom.ToString().ToUpper();
        }

        public static string ASCII2HEX(string src)
        {
            while (src.Length < 128)
            {
                src = '\0' + src;
            }

            if (src.Length > 128)
            {
                src = src.Substring(0, 128);
            }
            string res = String.Empty;
            try
            {

                char[] charValues = src.ToCharArray();
                string hexOutput = "";
                foreach (char _eachChar in charValues)
                {
                    // Get the integral value of the character.
                    int value = Convert.ToInt32(_eachChar);
                    // Convert the decimal value to a hexadecimal value in string form.
                    hexOutput += String.Format("{0:X2}", value);
                    // to make output as your eg 
                    //  hexOutput +=" "+ String.Format("{0:X}", value);

                }

                return hexOutput;
            }
            catch (Exception)
            {
            }

            return res;

        }
    }
}
