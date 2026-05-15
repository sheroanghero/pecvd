using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aitex.Core.RT.Simulator;

namespace Aitex.Core.Backend
{
    public partial class DICtrl : UserControl
    {
        private string _ioName;
        private bool _rawValue;

        public DICtrl()
        {
            InitializeComponent();

            this.Load += DICtrl_Load;
        }

        private void DICtrl_Load(object sender, EventArgs e)
        {
             
        }

        public void SetIoName(string group, string ioName)
        {
            _ioName = ioName;
        }

        public void SetName(string name)
        {
            this.labelRaw.Text = name;
        }

        public void SetValue(bool latchedValue, bool rawValue)
        {
            _rawValue = rawValue;

             this.labelRaw.BackColor = rawValue ? Color.Lime : Color.LightGray;
 
        }

        private void labelName_Click(object sender, EventArgs e)
        {

        }
 

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                checkBox2.Checked = _rawValue;

                DiForce.Instance.Set(_ioName, checkBox2.Checked);
                checkBox2.Enabled = true;

                this.BackColor = Color.DodgerBlue;
            }
            else
            {
                DiForce.Instance.Unset(_ioName);
                checkBox2.Enabled = false;

                this.BackColor = Color.White;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            DiForce.Instance.Set(_ioName, checkBox2.Checked);
        }
    }
}
