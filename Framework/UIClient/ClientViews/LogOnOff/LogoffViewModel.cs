using System;
using System.Windows;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.LogOnOff
{
    public class LogoffViewModel : DialogViewModel<UserMode>
    {
        public LogoffViewModel()
        {
            this.DisplayName = (string)Application.Current.Resources["AppName"];
            this.DialogResult = UserMode.Normal;
            this.LoginName = BaseApp.Instance.UserContext.LoginName;
            this.RoleName = BaseApp.Instance.UserContext.RoleName;
            this.LoginTime = BaseApp.Instance.UserContext.LoginTime;
        }

        protected override void OnInitialize()
        {
            //check some condition to set property AllowShowDown, system manual and PM busy 
            this.Token = BaseApp.Instance.UserContext.Token;
        }

        public void Exit()
        {
            //this message can keep in resource file
            if (DialogBox.Confirm(string.Format("Are you sure that you want to {0}?", "exit")))
            {
                this.DialogResult = UserMode.Exit;
                this.TryClose(true);
            }
        }

        public void ShutDown()
        {
            if (DialogBox.Confirm(string.Format("Are you sure that you want to {0}?", "shutdown")))
            {
                this.DialogResult = UserMode.Shutdown;
                this.TryClose(true);
            }
        }

        public void Logoff()
        {
            this.DialogResult = UserMode.Logoff;
            this.TryClose();
        }

        public void Cancel()
        {
            this.DialogResult = UserMode.Normal;
            this.TryClose();
        }

        private bool _AllowShowDown = false;
        public bool AllowShowDown
        {
            get { return _AllowShowDown; }
            set
            {
                _AllowShowDown = value;
                NotifyOfPropertyChange("AllowShowDown");
            }
        }

        public string LoginName { get; private set; }
        public string RoleName { get; private set; }
        public DateTime LoginTime { get; private set; }
    }
}
