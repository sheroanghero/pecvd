using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Simulator;
using CheckBox = System.Windows.Forms.CheckBox;
using TextBox = System.Windows.Forms.TextBox;
using UserControl = System.Windows.Forms.UserControl;

namespace Aitex.Core.Backend
{
    public partial class SystemConfigView : UserControl
    {
        //<scname, type>
        private Dictionary<string, Type> _scItems = new Dictionary<string, Type>();

        public SystemConfigView()
        {
            InitializeComponent();
        }

        private void SystemConfigView_Load(object sender, EventArgs e)
        {
            this.Dock = DockStyle.Fill;

            //Dictionary<string, object> items = new Dictionary<string, object>();

            //List<string> groups = new List<string>();
            //foreach (FieldInfo field in typeof(SCName).GetFields())
            //{

            //    var group = field.Name.Split('_')[0];
            //    var name = field.Name.Split('_')[1];

            //    if (!groups.Contains(group))
            //    {
            //        groups.Add(group);
            //        TextBox tb = new TextBox();
            //        tb.ReadOnly = true;
            //        tb.Enabled = false;
            //        tb.Text = group;
            //        tb.TextAlign = HorizontalAlignment.Center;
            //        tb.BackColor = System.Drawing.SystemColors.MenuHighlight;
            //        tb.Location = new System.Drawing.Point(3, 3);

            //        tb.Size = new System.Drawing.Size(262, 35);
            //        tb.TabIndex = 0;
            //        this.flowLayoutPanel1.Controls.Add(tb);
            //    }

            //    _scItems[name] = typeof(string);


            //    TextBox tbName = new TextBox();
            //    tbName.Location = new System.Drawing.Point(3, 3);
            //    tbName.ReadOnly = true;
            //    tbName.Size = new System.Drawing.Size(268, 21);
            //    tbName.Text = name;
            //    tbName.Tag = field.Name;

            //    TextBox tbValue = new TextBox();
            //    tbValue.Location = new System.Drawing.Point(3, 3);
            //    tbValue.Size = new System.Drawing.Size(168, 21);
            //    tbValue.Text = SC.GetItemValue(field.Name).ToString();
            //    tbValue.Name = "tb" + field.Name;

            //    System.Windows.Forms.Button bt = new System.Windows.Forms.Button();
            //    bt.Location = new System.Drawing.Point(710, 3);
            //    bt.Size = new System.Drawing.Size(75, 23);
            //    bt.Text = "Set";
            //    bt.UseVisualStyleBackColor = true;
            //    bt.Click += bt_Click;
            //    bt.Name = "bt" + field.Name;
            //    bt.Tag = field.Name;

            //    FlowLayoutPanel fl = new FlowLayoutPanel();


            //    fl.Controls.Add(tbName);
            //    fl.Controls.Add(tbValue);
            //    fl.Controls.Add(bt);
            //    fl.Location = new System.Drawing.Point(3, 3);
            //    fl.Size = new System.Drawing.Size(853, 33);


            //    this.flowLayoutPanel1.Controls.Add(fl);

            //}
        }

        void bt_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Button bt = sender as System.Windows.Forms.Button;

            

            FlowLayoutPanel fl = bt.Parent as FlowLayoutPanel;
                            TextBox tbName = new TextBox();
                TextBox tbValue = new TextBox();

                 
                foreach (var v in fl.Controls)
                {
                    TextBox tb = v as TextBox;

                    if (tb == null)
                        continue;

                    if (tb.ReadOnly)
                        tbName = tb;
                    else
                    {
                        tbValue = tb;
                    }
                }
            SC.SetItemValue(bt.Tag.ToString(), tbValue.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //foreach (var panel in flowLayoutPanel1.Controls)
            //{
            //    if (panel.GetType() != typeof(FlowLayoutPanel))
            //        continue;

            //    TextBox tbName = new TextBox();
            //    TextBox tbValue = new TextBox();

            //    FlowLayoutPanel fl = panel as FlowLayoutPanel;
            //    foreach (var v in fl.Controls)
            //    {
            //        TextBox tb = v as TextBox;
            //        if (tb == null)
            //            continue;

            //        if (tb.ReadOnly)
            //            tbName = tb;
            //        else
            //        {
            //            tbValue = tb;
            //        }
            //    }

            //    tbValue.Text = SC.GetItemValue(tbName.Tag as string).ToString();
            //}
        }
    }
}
