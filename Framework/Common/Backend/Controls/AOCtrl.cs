using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aitex.Core.RT.IOCore;

namespace Aitex.Core.Backend
{
    public partial class AOCtrl : UserControl
    {
        private string _ioName;

        public AOCtrl()
        {
            InitializeComponent();
        }

        public void SetIoName(string group, string ioName)
        {
            _ioName = ioName;
        }

        public void SetName(string name)
        {
            labelName.Text = name;
        }

        /// <summary>
        /// set value
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(float value)
        {
 
            textBox1.Text = String.Format("{0:f2}", value);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            double value;
            if (double.TryParse(textBox2.Text, out value ))
             IO.AO[_ioName].Value = (short) value;
        }
    }
}
