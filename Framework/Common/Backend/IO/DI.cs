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
	public partial class DI : UserControl, IIOView
	{
        PeriodicJob _thread;

		public DI()
		{
			InitializeComponent();

            this.Load += new EventHandler(DI_Load);
            
		}

        void DI_Load(object sender, EventArgs e)
        {
            if (this.panel1.Controls.Count > 0)
                return;

            List<Tuple<int, int, string>> ioList = IO.GetIONameList("", IOType.DI);
            //List<Tuple<int, int, string>> ioListRemote = IO.GetIONameList("remote", IOType.DI);
            //foreach (var tuple in ioListRemote)
            //{
            //    ioList.Add(tuple);
            //}

            int totalColumn = 3;

            int countPerColumn = (ioList.Count / totalColumn) + ((ioList.Count % totalColumn) > 0 ? 1 : 0);

            
            int index = 0;
            foreach (var item in ioList)
            {
                int row = index % countPerColumn;
                int col = index / countPerColumn;

                DICtrl diCtrl1 = new DICtrl();
                diCtrl1.Location = new System.Drawing.Point(col * 305, row * 30);
                diCtrl1.SetName(string.Format("DI-{0}. {1}", item.Item1, item.Item3));
                diCtrl1.SetIoName("local", item.Item3);
                diCtrl1.Name = string.Format("DI_{0}", item.Item1);
                diCtrl1.Size = new System.Drawing.Size(300, 25);
                diCtrl1.Tag = item.Item3;
                //diCtrl1.Margin = new Padding(0, 0, 5, 5);


                this.panel1.Controls.Add(diCtrl1);
                index++;
            }

            _thread = new PeriodicJob(500, OnTimer, "DITimer", false, true);

            VisibleChanged += (sender1, e1) =>
            {
                if (Visible) _thread.Start(); else _thread.Pause();
            };
        }

        bool OnTimer()
        {
            Invoke(new Action(() =>
            {
                foreach (DICtrl ctrl in panel1.Controls)
                {
                    string name = (string)ctrl.Tag;
                    if (ctrl != null)
                    {
                        ctrl.SetValue(IO.DI[name].Value, IO.DI[name].Value);
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
