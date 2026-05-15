using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MECF.Framework.Common.Account.Extends
{
    [Serializable]
    public class AppMenu : INotifyPropertyChanged
    {
        private string m_strMenuID;
        private string m_strViewModel;
        private string m_strResKey;
        private string _System;
        private int permission;
        private AppMenu parent;
        private List<AppMenu> menuItems;
        private bool selected;
     
        public string MenuID
        {
            get { return m_strMenuID; }
            set { m_strMenuID = value; }
        }
     
        public string ResKey
        {
            get { return this.m_strResKey; }
            set { m_strResKey = value; }
        }
   
        public List<AppMenu> MenuItems
        {
            get { return menuItems; }
            set { menuItems = value; }
        }
    
        public string ViewModel
        {
            get { return this.m_strViewModel; }
            set { this.m_strViewModel = value; }
        }
    
        public int Permission
        {
            get { return this.permission; }
            set { this.permission = value; }
        }
     
        public string System
        {
            get { return this._System; }
            set { this._System = value; }
        }

        public AppMenu Parent
        {
            get { return this.parent; }
            set { this.parent = value; }
        }

        public bool Selected
        {
            get { return this.selected; }
            set
            {
                this.selected = value;
                this.OnPropertyChanged("Selected");
            }
        }

        private bool _isAlarm;
        public bool IsAlarm
        {
            get { return this._isAlarm; }
            set
            {
                this._isAlarm = value;
                this.OnPropertyChanged("IsAlarm");
            }
        }

        private string _alarmModule;
        public string AlarmModule
        {
            get { return this._alarmModule; }
            set { this._alarmModule = value; }
        }

        private string _type;
        public string Type
        {
            get { return this._type; }
            set { this._type = value; }
        }

        public object Model { get; set; }

        public AppMenu LastSelectedSubMenu { get; set; }

        public AppMenu() { }

        public AppMenu(string p_strMenuID, string p_strMenuViewModel, string p_strResKey, List<AppMenu> p_menuItems)
        {
            this.m_strMenuID = p_strMenuID;
            this.m_strViewModel = p_strMenuViewModel;
            this.m_strResKey = p_strResKey;
            this.menuItems = p_menuItems;
        }

        public object Clone(AppMenu parent)
        {
            AppMenu item = new AppMenu();

            item.MenuID = this.MenuID;
            item.ResKey = this.ResKey;
            item.ViewModel = this.ViewModel;
            item.Permission = this.Permission;
            item.Selected = this.Selected;
            item.System = this.System;
            item.Parent = parent;
            item.IsAlarm = this.IsAlarm;
            item.AlarmModule = this.AlarmModule;
            item.Type = this.Type;

            if (this.MenuItems != null)
            {
                item.MenuItems = new List<AppMenu>();
                foreach (var item1 in this.MenuItems)
                {
                    item.MenuItems.Add((AppMenu)item1.Clone(item));
                }
            }

            return item;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
