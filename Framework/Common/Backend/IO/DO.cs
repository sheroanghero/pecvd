using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;

namespace Aitex.Core.Backend
{
    public partial class DO : UserControl, IIOView
	{
        PeriodicJob _thread;

		public DO()
		{
			InitializeComponent();

            this.Load += new EventHandler(DO_Load);
            
		}

        void DO_Load(object sender, EventArgs e)
        {
            if (this.panel1.Controls.Count > 0)
                return;

            List<Tuple<int, int, string>> ioList = IO.GetIONameList("", IOType.DO);
            //List<Tuple<int, int, string>> ioListRemote = IO.GetIONameList("remote", IOType.DO);
            //foreach (var tuple in ioListRemote)
            //{
            //    ioList.Add(tuple);
            //}
            int index = 0;
            foreach (var item in ioList)
            {
                Button button1 = new Button();
                //button1.Margin = new Padding(0,0,5,5);
                button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
                button1.Location = new System.Drawing.Point(index % 3 * 305, index / 3 * 30);
                //button1.Name = string.Format("DO_{0}", i);
                button1.Size = new System.Drawing.Size(300, 25);
                button1.Text = string.Format("DO-{0}. {1}", item.Item1, item.Item3);
                button1.Tag = item.Item3;
                button1.BackColor = Color.LightGray;
                button1.TextAlign = ContentAlignment.MiddleCenter;
                button1.Font = new System.Drawing.Font("Arial,SimSun", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
                button1.UseVisualStyleBackColor = false;
                button1.Click += (sender2, e2) =>
                {
                    var btn = (Button)sender2;
                    string name = (string)btn.Tag;
                    string reason;
                    if (!IO.DO[name].SetValue(!IO.DO[name].Value, out reason))
                    {
                        MessageBox.Show(reason);
                    }
                };
                this.panel1.Controls.Add(button1);
                index++;
            }

            _thread = new PeriodicJob(500, OnTimer, "DOTimer", false, true);

            VisibleChanged += (sender1, e1) =>
            {
                if (Visible) _thread.Start(); else _thread.Pause();
            };
        }

        bool OnTimer()
		{
            Invoke(new Action(() =>
            {
                foreach (Button ctrl in this.panel1.Controls)
                {
                    if (ctrl != null)
                    {
                        string name = (string)ctrl.Tag;

                        ctrl.BackColor = IO.DO[(string)ctrl.Tag].Value ? Color.Lime : Color.LightGray;
                    }
                }
            }));


            return true;
		}

        public void EnableTimer(bool enable)
        {
            if (enable)
                _thread.Start();
            else
                _thread.Pause();
        }

        public void Close()
        {
            if (_thread != null)
            _thread.Stop();
        }
	}
}
