using Aitex.Core.RT.Device;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Coolers;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TMs;
using MECF.Framework.RT.ModuleLibrary.LLModules;
using MECF.Framework.RT.ModuleLibrary.PMModules;
using MECF.Framework.RT.ModuleLibrary.SystemModules;
using MECF.Framework.RT.ModuleLibrary.TMModules;
using MECF.Framework.RT.ModuleLibrary.VceModules;

namespace MECF.Framework.RT.ModuleLibrary.Commons
{
    public class SharedPump : Singleton<SharedPump>
    {
        private ModuleName _owner = ModuleName.System;
        private object _locker = new object();
        private object _monitorlocker = new object();
        private DeviceTimer _timer = new DeviceTimer();

        private int _occupyMinTime = 10000;

        private bool _isVCEAInstalled = SC.ContainsItem("System.SetUp.VCEA.IsInstalled") && SC.GetValue<bool>("System.SetUp.VCEA.IsInstalled");
        private bool _isVCEBInstalled = SC.ContainsItem("System.SetUp.VCEB.IsInstalled") && SC.GetValue<bool>("System.SetUp.VCEB.IsInstalled");
        private bool _isPM1Installed = SC.ContainsItem("System.SetUp.PM1.IsInstalled") && SC.GetValue<bool>("System.SetUp.PM1.IsInstalled");
        private bool _isPM2Installed = SC.ContainsItem("System.SetUp.PM2.IsInstalled") && SC.GetValue<bool>("System.SetUp.PM2.IsInstalled");
        private bool _isPM3Installed = SC.ContainsItem("System.SetUp.PM3.IsInstalled") && SC.GetValue<bool>("System.SetUp.PM3.IsInstalled");
        private bool _isPM4Installed = SC.ContainsItem("System.SetUp.PM4.IsInstalled") && SC.GetValue<bool>("System.SetUp.PM4.IsInstalled");
        private bool _isPM5Installed = SC.ContainsItem("System.SetUp.PM5.IsInstalled") && SC.GetValue<bool>("System.SetUp.PM5.IsInstalled");
        private bool _isPM6Installed = SC.ContainsItem("System.SetUp.PM6.IsInstalled") && SC.GetValue<bool>("System.SetUp.PM6.IsInstalled");
        private bool _isPM7Installed = SC.ContainsItem("System.SetUp.PM7.IsInstalled") && SC.GetValue<bool>("System.SetUp.PM7.IsInstalled");
        private bool _isCoolerInstalled = SC.ContainsItem("System.SetUp.Cooler.IsInstalled") && SC.GetValue<bool>("System.SetUp.Cooler.IsInstalled");

        private string _PM1Type = SC.ContainsItem("System.SetUp.PM1.ChamberType") ? SC.GetStringValue("System.SetUp.PM1.ChamberType") : null;
        private string _PM2Type = SC.ContainsItem("System.SetUp.PM2.ChamberType") ? SC.GetStringValue("System.SetUp.PM2.ChamberType") : null;
        private string _PM3Type = SC.ContainsItem("System.SetUp.PM3.ChamberType") ? SC.GetStringValue("System.SetUp.PM3.ChamberType") : null;
        private string _PM4Type = SC.ContainsItem("System.SetUp.PM4.ChamberType") ? SC.GetStringValue("System.SetUp.PM4.ChamberType") : null;
        private string _PM5Type = SC.ContainsItem("System.SetUp.PM5.ChamberType") ? SC.GetStringValue("System.SetUp.PM5.ChamberType") : null;
        private string _PM6Type = SC.ContainsItem("System.SetUp.PM6.ChamberType") ? SC.GetStringValue("System.SetUp.PM6.ChamberType") : null;
        private string _PM7Type = SC.ContainsItem("System.SetUp.PM7.ChamberType") ? SC.GetStringValue("System.SetUp.PM7.ChamberType") : null;

        private bool _isLLAInstalled = SC.ContainsItem("System.SetUp.LLA.IsInstalled") && SC.GetValue<bool>("System.SetUp.LLA.IsInstalled");
        private bool _isLLBInstalled = SC.ContainsItem("System.SetUp.LLB.IsInstalled") && SC.GetValue<bool>("System.SetUp.LLB.IsInstalled");

        public bool OtherRoughValveIsClose(ModuleName module)
        {
            lock (_locker)
            {

                if (!_timer.IsIdle() && !_timer.IsTimeout())
                    return false;

                if (_isVCEAInstalled && module != ModuleName.VCEA && ((VceModuleBase)EquipmentManager.Modules[ModuleName.VCEA]).CheckIsPumping())
                {
                    return false;
                }

                if (_isVCEBInstalled && module != ModuleName.VCEB && ((VceModuleBase)EquipmentManager.Modules[ModuleName.VCEB]).CheckIsPumping())
                {
                    return false;
                }

                if (_isLLAInstalled && module != ModuleName.LLA && ((LoadLockModuleBase)EquipmentManager.Modules[ModuleName.LLA]).CheckIsPumping())
                {
                    return false;
                }

                if (_isLLBInstalled && module != ModuleName.LLB && ((LoadLockModuleBase)EquipmentManager.Modules[ModuleName.LLB]).CheckIsPumping())
                {
                    return false;
                }

                if (module != ModuleName.TM && ((TMModuleBase)EquipmentManager.Modules[ModuleName.TM]).CheckIsPumping())
                {
                    return false;
                }

                if (_isPM1Installed && module != ModuleName.PM1 && _PM1Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM1]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM2Installed && module != ModuleName.PM2 && _PM2Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM2]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM3Installed && module != ModuleName.PM3 && _PM3Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM3]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM4Installed && module != ModuleName.PM4 && _PM4Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM4]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM5Installed && module != ModuleName.PM5 && _PM5Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM5]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM6Installed && module != ModuleName.PM6 && _PM6Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM6]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM7Installed && module != ModuleName.PM7 && _PM7Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM7]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isCoolerInstalled && module != ModuleName.Cooler && DEVICE.GetDevice<Cooler>(ModuleName.Cooler.ToString()).CheckIsPumping())
                {
                    return false;
                }

                _timer.Start(_occupyMinTime);
                return true;
            }


        }


        public bool MonitorOtherRoughValveIsClose(ModuleName module)
        {
            lock (_monitorlocker)
            {
                if (_isVCEAInstalled && module != ModuleName.VCEA && ((VceModuleBase)EquipmentManager.Modules[ModuleName.VCEA]).CheckIsPumping())
                {
                    return false;
                }

                if (_isVCEBInstalled && module != ModuleName.VCEB && ((VceModuleBase)EquipmentManager.Modules[ModuleName.VCEB]).CheckIsPumping())
                {
                    return false;
                }

                if (_isLLAInstalled && module != ModuleName.LLA && ((LoadLockModuleBase)EquipmentManager.Modules[ModuleName.LLA]).CheckIsPumping())
                {
                    return false;
                }

                if (_isLLBInstalled && module != ModuleName.LLB && ((LoadLockModuleBase)EquipmentManager.Modules[ModuleName.LLB]).CheckIsPumping())
                {
                    return false;
                }

                if (module != ModuleName.TM && ((TMModuleBase)EquipmentManager.Modules[ModuleName.TM]).CheckIsPumping())
                {
                    return false;
                }

                if (_isPM1Installed && module != ModuleName.PM1 && _PM1Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM1]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM2Installed && module != ModuleName.PM2 && _PM2Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM2]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM3Installed && module != ModuleName.PM3 && _PM3Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM3]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM4Installed && module != ModuleName.PM4 && _PM4Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM4]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM5Installed && module != ModuleName.PM5 && _PM5Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM5]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM6Installed && module != ModuleName.PM6 && _PM6Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM6]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isPM7Installed && module != ModuleName.PM7 && _PM7Type == "PVD" && ((PMModuleBase)EquipmentManager.Modules[ModuleName.PM7]).CheckPumpIsOn())
                {
                    return false;
                }

                if (_isCoolerInstalled && module != ModuleName.Cooler && DEVICE.GetDevice<Cooler>(ModuleName.Cooler.ToString()).CheckIsPumping())
                {
                    return false;
                }

                return true;
            }



        }
    }


}
