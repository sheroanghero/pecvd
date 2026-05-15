using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceModel;
using System.Threading;
using System.Configuration;
using Aitex.Core.RT.OperationCenter;

namespace Aitex.Core.Backend
{
    public partial class MainView : Form
    {
		private NotifyIcon _notifyIcon = new NotifyIcon();  
		Dictionary<string,UserControl> _views;
        object _msgLock = new object();
        List<string> _events = new List<string>();

		public MainView()
        {
            InitializeComponent();

            _views = new Dictionary<string, UserControl>();
            _views.Add("About",new AboutView());
 
            foreach(var item in _views)
                splitContainer2.Panel2.Controls.Add(item.Value);

            ////defaut view
            ShowView("About");
 

            this.SizeChanged += new EventHandler(MainView_SizeChanged);

       }

        void MainView_SizeChanged(object sender, EventArgs e)
        {
            foreach (var item in _views)
            {
                item.Value.Width = this.splitContainer1.Panel2.Width;
                item.Value.Height = this.splitContainer1.Panel2.Height;
            }
        }
 

        /// <summary>
        /// 不允许用户直接关闭后台界面窗口程序，退出PM程序必须通过点击任务栏图标实现程序退出
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

        }   

 

        public void AddCustomView(string name, UserControl uc)
        {
            if (uc == null)
                return;

            this.treeView1.Nodes.Add(new TreeNode() { Tag = name, Text = name });

            uc.Show();

            uc.Hide();

            _views.Add(name, uc);
            splitContainer2.Panel2.Controls.Add(uc);

            uc.Width = splitContainer2.Panel2.Width;
            uc.Height = splitContainer2.Panel2.Height;
        }
 
		protected override CreateParams CreateParams
		{
			get
			{
				int CS_NOCLOSE = 0x200;
				CreateParams parameters = base.CreateParams;
				parameters.ClassStyle |= CS_NOCLOSE;
				return parameters;
			}
		}

 
		private void ShowView(string viewName)
		{
			foreach (var item in _views)
			{
				if (item.Key != viewName)
					item.Value.Hide();
				else
					item.Value.Show();
			}		
		}
 
		private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			var viewName = (string)e.Node.Tag;
			ShowView(viewName);
		}
 
		private void btnLogout_Click(object sender, EventArgs e)
		{
			Hide();
		}
 

        private void btnReset_Click(object sender, EventArgs e)
        {
            OP.DoOperation("Reset");
        }
    }
}
