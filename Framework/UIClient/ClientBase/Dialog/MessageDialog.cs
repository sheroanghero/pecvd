using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.UI.Client.ClientBase;

namespace OpenSEMI.ClientBase
{
    public class MessageDialog : DialogViewModel<DialogButton>
    {
        private string m_text;
        public string Text
        {
            get { return m_text; }
            set { m_text = value; NotifyOfPropertyChange("Text"); }
        }

        public DialogButton DialogButton
        {
            set
            {
                this.m_buttons.Clear();
                foreach (int v in Enum.GetValues(typeof(DialogButton)))
                {
                    DialogButton m_btn = (DialogButton)v;
                    if (value.HasFlag(m_btn))
                    {
                        ButtonControl bc = new ButtonControl();
                        bc.Name = m_btn.ToString();

                        if (m_btn == DialogButton.OK || m_btn == DialogButton.Yes)
                            bc.IsDefault = true;
                        else
                            bc.IsDefault = false;

                        if (m_btn == DialogButton.Cancel || m_btn == DialogButton.No)
                            bc.IsCancel = true;
                        else
                            bc.IsCancel = false;
                        m_buttons.Add(bc);
                    }
                }
            }
        }

        private List<ButtonControl> m_buttons = new List<ButtonControl>();
        public List<ButtonControl> Buttons
        {
            get { return m_buttons; }
        }

        private DialogType m_DialogType = DialogType.INFO;
        public DialogType DialogType
        {
            get { return m_DialogType; }
            set { m_DialogType = value; NotifyOfPropertyChange("DialogType"); }
        }
    }

    public class ButtonControl
    {
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public bool IsCancel { get; set; }
    }
}
