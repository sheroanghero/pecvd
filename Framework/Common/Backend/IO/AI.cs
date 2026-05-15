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
    public partial class AI : UserControl, IIOView
	{
        PeriodicJob _thread;

		public AI()
		{
			InitializeComponent();

            this.Load += new EventHandler(AI_Load);
        }

        void AI_Load(object sender, EventArgs e)
        {
            if (this.Controls.Count > 0)
                return;

            List<Tuple<int, int, string>> ioList = IO.GetIONameList("", IOType.AI);
            //List<Tuple<int, int, string>> ioListRemote = IO.GetIONameList("remote", IOType.AI);
            //foreach (var tuple in ioListRemote)
            //{
            //    ioList.Add(tuple);
            //}
            int index = 0;
            foreach (var item in ioList)
            {
                AICtrl aiCtrl1 = new AICtrl();
                aiCtrl1.Location = new System.Drawing.Point(index % 3 * 305, index / 3 * 55);
                aiCtrl1.SetName(string.Format("AI-{0}.{1}", item.Item1, item.Item3));
                aiCtrl1.SetIoName("local", item.Item3);
                //aiCtrl1.Margin = new Padding(0, 0, 5, 5);

                aiCtrl1.Name = string.Format("AI_{0}", item.Item1);
                aiCtrl1.Size = new System.Drawing.Size(300, 50);
                aiCtrl1.Visible = true;
                aiCtrl1.Tag = item.Item3;
                this.Controls.Add(aiCtrl1);
                index++;
            }

            _thread = new PeriodicJob(500, OnTimer, "AITimer", false, true);

            VisibleChanged += (sender1, e1) =>
            {
                if (Visible) _thread.Start(); else _thread.Pause();
            };
        }

        bool OnTimer()
        {
            Invoke(new Action(() =>
            {
                foreach (AICtrl ctrl in Controls)
                {
                    if (ctrl != null)
                    {
                        ctrl.SetValue((short)IO.AI[(string)ctrl.Tag].Value);
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
