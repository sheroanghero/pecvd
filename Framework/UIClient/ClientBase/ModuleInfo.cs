using Caliburn.Micro.Core;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class ModuleInfo : PropertyChangedBase
    {
        private string _moduleID;
        public string ModuleID
        {
            get { return _moduleID; }
            set { _moduleID = value; NotifyOfPropertyChange("ModuleID"); }
        }

        private string _waferModuleID;
        public string WaferModuleID
        {
            get { return _waferModuleID; }
            set { _waferModuleID = value; NotifyOfPropertyChange("WaferModuleID"); }
        }

        private bool _IsInstalled;
        public bool IsInstalled
        {
            get { return _IsInstalled; }
            set { _IsInstalled = value; NotifyOfPropertyChange("IsInstalled"); }
        }

        private bool _IsOnline;
        public bool IsOnline
        {
            get { return _IsOnline; }
            set { _IsOnline = value; NotifyOfPropertyChange("IsOnline"); }
        }

        private ModuleWaferManager _WaferManager;
        public ModuleWaferManager WaferManager
        {
            get { return _WaferManager; }
            set { _WaferManager = value; NotifyOfPropertyChange("WaferManager"); }
        }
 
        public string WaferDataName { get; set; }

        public bool IsWaferReverseDisplay { get; set; }

        public ModuleInfo(string name, string waferModuleName, string waferDataName, bool isWaferReverseDisplay, bool isInstalled )
        {
            ModuleID = name;
            WaferDataName = waferDataName;
            WaferModuleID = string.IsNullOrEmpty(waferModuleName) ? name : waferModuleName;
            IsWaferReverseDisplay = isWaferReverseDisplay;
            IsInstalled = isInstalled;

            WaferManager = new ModuleWaferManager(name);
        }
 
    }
}
