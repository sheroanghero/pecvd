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
    public partial class AO : UserControl, IIOView
	{
        PeriodicJob _thread;

        public AO()
		{
			InitializeComponent();

            this.Load += new EventHandler(AO_Load);
		}

        void AO_Load(object sender, EventArgs e)
        {
            if (this.Controls.Count > 0)
                return;

            List<Tuple<int, int, string>> ioList = IO.GetIONameList("", IOType.AO);

            //List<Tuple<int, int, string>> ioListRemote = IO.GetIONameList("remote", IOType.AO);
            //foreach (var tuple in ioListRemote)
            //{
            //    ioList.Add(tuple);
            //}

            int index = 0;
            foreach (var item in ioList)
            {
                AOCtrl aoCtrl1 = new AOCtrl();
                aoCtrl1.Location = new System.Drawing.Point(index % 3 * 305, index / 3 * 55);
                aoCtrl1.SetName(string.Format("AO-{0}.{1}", item.Item1, item.Item3));
 
                aoCtrl1.SetIoName("local", item.Item3);
                aoCtrl1.Name = string.Format("AO_{0}", item.Item1);
                aoCtrl1.Size = new System.Drawing.Size(300, 50);
                aoCtrl1.Visible = true;
                aoCtrl1.Tag = item.Item3;
                //aoCtrl1.Margin = new Padding(0, 0, 5, 5);

                Controls.Add(aoCtrl1);
                index++;
            }

            _thread = new PeriodicJob(500, OnTimer, "AOTimer", false, true);

            VisibleChanged += (sender1, e1) =>
            {
                if (Visible) _thread.Start(); else _thread.Pause();
            };
        }

        bool OnTimer()
        {
            Invoke(new Action(() =>
            {
                foreach (AOCtrl ctrl in Controls)
                {
                    if (ctrl != null)
                    {
                        ctrl.SetValue((short)IO.AO[(string)ctrl.Tag].Value);
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
