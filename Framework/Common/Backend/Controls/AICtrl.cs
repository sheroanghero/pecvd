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
    public partial class AICtrl : UserControl
    {
        private string _ioName;
        public AICtrl()
        {
            InitializeComponent();
        }

        public void SetName(string name)
        {
            labelName.Text = name;
        }

        public void SetIoName(string group, string ioName)
        {
            _ioName = ioName;
        }
 
        public void SetValue(float value)
        {
            textBox1.Text = String.Format("{0:f2}", value);
 
        }

 

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBox2.Text = textBox1.Text;

                float value;
                float.TryParse(textBox2.Text.Trim(), out value);

                AiForce.Instance.Set(_ioName, value);

                button1.Enabled = true;
                textBox2.Enabled = true;

                this.BackColor = Color.DodgerBlue;
            }
            else
            {
                AiForce.Instance.Unset(_ioName);

                button1.Enabled = false;
                textBox2.Enabled = false;

                this.BackColor = Color.White;
             }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            float value;
            if (float.TryParse(textBox2.Text.Trim(), out value))
                AiForce.Instance.Set(_ioName, value);
        }
    }
}
