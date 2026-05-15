using System.IO;
using System.Linq;
using System.Reflection;
using Aitex.Common.Util;
using Caliburn.Micro;
using MECF.Framework.Common.Account.Extends;
 
namespace MECF.Framework.UI.Client.ClientBase
{
    public class BaseApp
    {
        public UserContext UserContext { get; set; }
        public MenuManager MenuManager { get; set; }
        public MenuLoader MenuLoader { get; set; }

        public UserMode UserMode { get; set; }
        public bool Initialized { get; set; }

        public ModuleDataMonitor _dataMonitor;

        private static BaseApp _instance = null;

        public static BaseApp Instance
        {
            get
            {
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public BaseApp()
        {
            this.UserMode = UserMode.None;
            this.Initialized = false;
 
            this.Configure();
        }

        public virtual void Initialize(bool force = false)
        {
            if (this.Initialized && !force)
                return;

            this.MenuLoader = new MenuLoader(Path.Combine(PathManager.GetCfgDir(), "Menu.xml"));
             
            this.MenuLoader.Load();

            this.MenuManager = new MenuManager();

            this.UserContext = new UserContext();

            var file = string.Format("{0}MECF.Framework.UI.Client.dll", System.AppDomain.CurrentDomain.BaseDirectory);
            if (File.Exists(file))
            {
                Assembly assembly = Assembly.LoadFile(file);
                AssemblySource.Instance.Add(assembly);
            }

            this.OnInitialize();

            //must be called after specific project initialized
            _dataMonitor = new ModuleDataMonitor();
 

            this.Initialized = true;
        }

        public virtual void Dispose()
        {
             
        }

        protected void Configure()
        {
            //config skin/language...
            this.OnConfiguration();
 
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnConfiguration() { }
        public virtual void SwitchPage(string mainMenu, string subMenu, object parameter) { }

        public int GetPermission(string menuid)
        {
            int per = 1;
            if (this.UserContext != null)
            {
                string[] list = this.UserContext.Role.MenuPermission.Split(';');
                var result = from r in list
                             where r.Split(',')[0] == menuid
                             select r;
                if (result.Count() > 0)
                    per = int.Parse(result.ToArray()[0].Split(',')[1]);
            }
            return per;
        }


    }
}
