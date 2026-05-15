using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Aitex.Core.Backend
{
	public partial class IoDataView : UserControl
	{
	    //List<UserControl> _views = new List<UserControl>();

	    DI _diView = new DI();
	    DO _doView = new DO();
	    AI _aiView = new AI();
	    AO _aoView = new AO();

        public IoDataView()
		{
			InitializeComponent();

            this.Load += IoDataView_Load;

			//_views.Add(_diView);
			//_views.Add(_doView);
			//_views.Add(_aiView);
			//_views.Add(_aoView);
			

		}

        private void IoDataView_Load(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
            {
                AddView("DI", _diView);
                AddView("DO", _doView);
                AddView("AI", _aiView);
                AddView("AO", _aoView);
            }
        }

	    private void AddView(string name, UserControl uc)
	    {
	            uc.Dock = DockStyle.Fill;
	        uc.AutoScroll = true;

	            TabPage tp = new TabPage();
	            tp.Text = name;
	            tp.Controls.Add(uc);
	            this.tabControl1.Controls.Add(tp);
 
	    }

        public void Close()
        {
            ((IIOView)_diView).Close();
            ((IIOView)_doView).Close();
            ((IIOView)_aiView).Close();
            ((IIOView)_aoView).Close();
        }
	}
}
