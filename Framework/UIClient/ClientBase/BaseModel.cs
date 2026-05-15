using Caliburn.Micro.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class BaseModel : Screen
    {
        public BaseModel()
        {
         }

        public PageID Page { get; set; }
 
        private int permission = 0;
        private int token = 0;

        public int Permission
        {
            get { return this.permission; }
            set
            {
                if (this.permission != value)
                {
                    this.permission = value;
                    this.NotifyOfPropertyChange("Permission");
					this.NotifyOfPropertyChange("IsPermission");
                }
            }
        }

        public bool HasToken { get { return this.token > 0 ? true : false; } }

        public int Token
        {
            get { return this.token; }
            set
            {
                if (this.token != value)
                {
                    this.token = value;
                    OnTokenChanged(this.token);
                    this.NotifyOfPropertyChange("Token");
                }
            }
        }

        protected virtual void OnTokenChanged(int nNewToken) { }

        protected override void OnActivate()
        {
            base.OnActivate();
         }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
         }
    }
}
