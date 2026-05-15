using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Shapes;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Simulator;

namespace Aitex.Core.Backend
{
    public partial class SimulatorView : UserControl
    {
        //<GroupName, <Name, Description>>
        private Dictionary<string, Dictionary<string, string>> _caseGroup = new Dictionary<string, Dictionary<string, string>>();
        public SimulatorView()
        {
            InitializeComponent();
        }

        private void SimulatorView_Load(object sender, EventArgs e)
        {
            this.Dock = DockStyle.Fill;

            //btnSetSimulation.Enabled = false;

            //ckInSimulationMode.Checked = SC.GetValue<bool>(SCName.System_IsSimulatorMode);

            foreach (var propertyInfo in typeof(ExceptionCase).GetProperties())
            {
                foreach (var attr in propertyInfo.GetCustomAttributes(false))
                {
                    var display = attr as DisplayAttribute;
                    if (display == null)
                        continue;

                    if (!_caseGroup.ContainsKey(display.GroupName))
                    {
                        _caseGroup[display.GroupName] = new Dictionary<string, string>();

                        TextBox tb = new TextBox();
                        tb.ReadOnly = true;
                        tb.Enabled = false;
                        tb.Text = display.GroupName;
                        tb.TextAlign = HorizontalAlignment.Center;
                                     tb.BackColor = System.Drawing.SystemColors.MenuHighlight;
                            tb.Location = new System.Drawing.Point(3, 3);
                             
                            tb.Size = new System.Drawing.Size(262, 35);
                            tb.TabIndex = 0;
                         this.flowLayoutPanel1.Controls.Add(tb);
                    }

                    _caseGroup[display.GroupName][propertyInfo.Name] = display.Description;



                    CheckBox cb = new CheckBox();

                    cb.AutoSize = true;
                    cb.Location = new System.Drawing.Point(16, 34);
                    cb.Name = "ck" + propertyInfo.Name;
                    cb.Size = new System.Drawing.Size(126, 16);
                    cb.TabIndex = 1;
                    cb.Text = display.Description;
                    cb.UseVisualStyleBackColor = true;
                    cb.CheckedChanged += new System.EventHandler(this.cbCheckedChanged);
                    cb.Tag = propertyInfo;


                    this.flowLayoutPanel1.Controls.Add(cb);

                }
            }



        }

        private void cbCheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            PropertyInfo property = cb.Tag as PropertyInfo;

            property.SetValue(null, cb.Checked, null);
        }


    }
}
