using MECF.Framework.Simulator.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.FFUs
{
    class FfuViewModel : SerialPortDeviceViewModel
    {
        private Ffu _reader;

        public string ResultValue { get; set; }

        public FfuViewModel(string port) : base("FfuViewModel")
        {
            _reader = new Ffu(port);
            Init(_reader, true);
            _reader.receiveMsg += _reader_receiveMsg;


        }

        private void _reader_receiveMsg(string obj)
        {
            ResultValue = obj;
        }
    }
}
