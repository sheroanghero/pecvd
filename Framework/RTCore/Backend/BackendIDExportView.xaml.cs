using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MECF.Framework.RT.Core.Backend
{
    /// <summary>
    /// IDExport.xaml 的交互逻辑
    /// </summary>
    public partial class BackendIDExportView : UserControl
    {
        public BackendIDExportView()
        {
            InitializeComponent();

            DataContext = new BackendIDExportViewModel();

            this.IsVisibleChanged += IDExportViewModel_IsVisibleChanged;
        }

        private void IDExportViewModel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            (DataContext as SubscriptionViewModelBase).EnableTimer(IsVisible);
        }
    }


    public class BackendIDExportViewModel : SubscriptionViewModelBase
    {

        #region Command

        public ICommand ExportEcidCommand { get; set; }
        public ICommand ExportAlidCommand { get; set; }
        public ICommand ExportSvidCommand { get; set; }
        public ICommand ExportTMSvidCommand { get; set; }


        #endregion

        public BackendIDExportViewModel() : base("BackendIDExportViewModel")
        {
            ExportEcidCommand = new DelegateCommand<object>(PerformExportEcid);
            //ExportAlidCommand = new DelegateCommand<object>(PerformExportAlid);
            ExportSvidCommand = new DelegateCommand<object>(PerformExportSvid);
            ExportTMSvidCommand = new DelegateCommand<object>(PerformExportTMSvid);
        }

        private void PerformExportEcid(object obj)
        {
            var lists = SC.GetItemList();
            int id = 3001;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".xlsx"; // Default file extension 
            dlg.FileName = $"Equipment_Constant_{DateTime.Now:yyyyMMdd_HHmmss}";
            dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
            bool? result = dlg.ShowDialog();// Show open file dialog box

            if (result == true)
            {
                System.Data.DataSet ds = new System.Data.DataSet();
                ds.Tables.Add(new System.Data.DataTable("ECID(Equipment Constant)"));
                ds.Tables[0].Columns.Add("ECID");
                ds.Tables[0].Columns.Add("Name");
                ds.Tables[0].Columns.Add("Format");
                ds.Tables[0].Columns.Add("Description");

                foreach (var item in lists)
                {
                    var row = ds.Tables[0].NewRow();
                    row[0] = id++;
                    row[1] = item.PathName;
                    row[2] = item.Type;
                    row[3] = item.Description;
                    ds.Tables[0].Rows.Add(row);
                }

                if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason))
                {
                    MessageBox.Show($"Export failed, {reason}", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show($"Export succeed, file save as {dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PerformExportAlid(object obj)
        {
            var lists = AdsDicPlcIndex.DicAlarmNotify;
            int id = 70001;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".xlsx"; // Default file extension 
            dlg.FileName = $"AlarmID_{DateTime.Now:yyyyMMdd_HHmmss}";
            dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
            bool? result = dlg.ShowDialog();// Show open file dialog box

            if (result == true)
            {
                System.Data.DataSet ds = new System.Data.DataSet();
                ds.Tables.Add(new System.Data.DataTable("ALID(AlarmID)"));
                ds.Tables[0].Columns.Add("ALID");
                ds.Tables[0].Columns.Add("Name");
                ds.Tables[0].Columns.Add("Description");
                ds.Tables[0].Columns.Add("EventSet");
                ds.Tables[0].Columns.Add("EventClear");

                foreach (var item in lists)
                {
                    var row = ds.Tables[0].NewRow();
                    row[0] = id++;
                    row[1] = item.Value[0];
                    row[2] = item.Value[1];
                    row[3] = 100001;
                    row[4] = 200001;
                    ds.Tables[0].Rows.Add(row);
                }

                if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason))
                {
                    MessageBox.Show($"Export failed, {reason}", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show($"Export succeed, file save as {dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PerformExportSvid(object obj)
        {
            var dataList = Singleton<DataManager>.Instance.BuiltInDataList;

            var lists = dataList.Where(m => m.StartsWith("PM")).OrderBy(m => m).ToList();

            int pm1Id = 10001;
            int pm2Id = 20001;
            int pm3Id = 30001;
            int pm4Id = 40001;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".xlsx"; // Default file extension 
            dlg.FileName = $"Chamber_Status_Variable_{DateTime.Now:yyyyMMdd_HHmmss}";
            dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
            bool? result = dlg.ShowDialog();// Show open file dialog box

            if (result == true)
            {
                System.Data.DataSet ds = new System.Data.DataSet();

                ds.Tables.Add(new System.Data.DataTable("SVID(Chamber Status Variable)"));
                ds.Tables[0].Columns.Add("SVID");
                ds.Tables[0].Columns.Add("Name");
                ds.Tables[0].Columns.Add("Format");
                ds.Tables[0].Columns.Add("Description");

                foreach (var item in lists)
                {
                    var row = ds.Tables[0].NewRow();

                    if (item.StartsWith("PM1"))
                        row[0] = pm1Id++;
                    else if (item.StartsWith("PM2"))
                        row[0] = pm2Id++;
                    else if (item.StartsWith("PM3"))
                        row[0] = pm3Id++;
                    else if (item.StartsWith("PM4"))
                        row[0] = pm4Id++;

                    row[1] = item;
                    row[2] = "Ascii";

                    var arr = item.Split('.');
                    for (int i = 0; i < arr.Length; i++)
                        row[3] += $"{arr[i]} "; 

                    ds.Tables[0].Rows.Add(row);
                }              

                if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason))
                {
                    MessageBox.Show($"Export failed, {reason}", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show($"Export succeed, file save as {dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PerformExportTMSvid(object obj)
        {
            var dataList = Singleton<DataManager>.Instance.BuiltInDataList;

            var tmLists = dataList.Where(m => !m.StartsWith("PM")).OrderBy(m => m).ToList();

            int tmId = 5001;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".xlsx"; // Default file extension 
            dlg.FileName = $"TM_Status_Variable_{DateTime.Now:yyyyMMdd_HHmmss}";
            dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
            bool? result = dlg.ShowDialog();// Show open file dialog box

            if (result == true)
            {
                System.Data.DataSet ds = new System.Data.DataSet();
     
                ds.Tables.Add(new System.Data.DataTable("SVID(TM Status Variable)"));
                ds.Tables[0].Columns.Add("SVID");
                ds.Tables[0].Columns.Add("Name");
                ds.Tables[0].Columns.Add("Format");
                ds.Tables[0].Columns.Add("Description");

                foreach (var item in tmLists)
                {
                    var row = ds.Tables[0].NewRow();
                    row[0] = tmId++;
                    row[1] = item;
                    row[2] = "Ascii";

                    var arr = item.Split('.');
                    for (int i = 0; i < arr.Length; i++)
                        row[3] += $"{arr[i]} ";

                    ds.Tables[0].Rows.Add(row);
                }

                if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason))
                {
                    MessageBox.Show($"Export failed, {reason}", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show($"Export succeed, file save as {dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class AdsDicPlcIndex
    {
        #region Dictionary

        // Alarm Info
        public static Dictionary<int, string[]> DicAlarmNotify = new Dictionary<int, string[]>()
        {
            { 1, new string[]{ "NH3SolubeDetectAlarm", "NH3 Solube Detect Alarm" }},
            { 2, new string[]{ "O3SolubeDetectAlarm", "O3 Solube Detect Alarm" }},
            { 3, new string[]{ "Res1SolubeDetectAlarm", "Res1 Solube Detect Alarm" }},
            { 4, new string[]{ "Res2SolubeDetectAlarm", "Res2 Solube Detect Alarm" }},
            { 5, new string[]{ "DetectorAlarm", "Detector Alarm" }},
            { 6, new string[]{ "LiftHandleStop", "Lift Handle Stop" }},
            { 7, new string[]{ "CDAPresDetect", "CDA Pressure Detect" }},
            { 8, new string[]{ "PMStop", "PM Stop" }},
            { 9, new string[]{ "O2PresSwitch", "O2 Pressure Switch" }},
            { 10, new string[]{ "N2PresSwitch", "N2 Pressure Switch" }},
            { 11, new string[]{ "NH3PresSwitch", "NH3 Pressure Switch" }},
            { 12, new string[]{ "EndPresSwitch", "End Pressure Switch" }},
            { 13, new string[]{ "HeadMagneticClose", "Head Magnetic Close" }},
            { 14, new string[]{ "HeadMagneticOpen", "Head Magnetic Open" }},
            { 15, new string[]{ "ChamBackTempSwitch", "Chamber Back Temp Switch" }},
            { 16, new string[]{ "ChamLeftUpTempSwitch", "Chamber Left Up Temp Switch" }},
            { 17, new string[]{ "ChamLeftDownTempSwitch", "Chamber Left Down Temp Switch" }},
            { 18, new string[]{ "ChamRightUpTempSwitch", "Chamber Right Up Temp Switch" }},
            { 19, new string[]{ "ChamRightDownTempSwitch", "Chamber Right Down Temp Switch" }},
            { 20, new string[]{ "ChamResTempSwitch", "Chamber Reserve Temp Switch" }},
            { 21, new string[]{ "WaterLeakDetect", "Water Leak Detect" }},
            { 22, new string[]{ "WaterVlvLeakDetect", "Water Valve Leak Detect" }},
            { 23, new string[]{ "GasBox1SafeDoor", "GasBox1 Safe Door" }},
            { 24, new string[]{ "GasBox2SafeDoor", "GasBox2 Safe Door" }},
            { 25, new string[]{ "GasBox3SafeDoor", "GasBox3 Safe Door" }},
            { 26, new string[]{ "GasBox4SafeDoor", "GasBox4 Safe Door" }},
            { 27, new string[]{ "PMPumpNotRun", "PM Pump Not Run" }},
            { 28, new string[]{ "ScrubberAlarm", "Scrubber Alarm" }},
            { 29, new string[]{ "SternRowPressureSwitchAlarm", "Stern Row Pressure Switch Alarm" }},
            { 30, new string[]{ "", "" }},
            { 31, new string[]{ "TubeHeatTempSwitch1", "Tube Heat Temp Switch 1" }},
            { 32, new string[]{ "TubeHeatTempSwitch2", "Tube Heat Temp Switch 2" }},
            { 33, new string[]{ "TubeHeatTempSwitch3", "Tube Heat Temp Switch 3" }},
            { 34, new string[]{ "TubeHeatTempSwitch4", "Tube Heat Temp Switch 4" }},
            { 35, new string[]{ "TubeHeatTempSwitch5", "Tube Heat Temp Switch 5" }},
            { 36, new string[]{ "TubeHeatTempSwitch6", "Tube Heat Temp Switch 6" }},
            { 37, new string[]{ "TubeHeatTempSwitch7", "Tube Heat Temp Switch 7" }},
            { 38, new string[]{ "TubeHeatTempSwitch8", "Tube Heat Temp Switch 8" }},
            { 39, new string[]{ "TubeHeatTempSwitch9", "Tube Heat Temp Switch 9" }},
            { 40, new string[]{ "TubeHeatTempSwitch10", "Tube Heat Temp Switch 10" }},
            { 41, new string[]{ "", "" }},
            { 42, new string[]{ "", "" }},
            { 43, new string[]{ "OverVoltProtect", "Over Volt Protect" }},
            { 44, new string[]{ "UndVoltLacPhaRevPhaProtect", "Under Volt, Lack Phase, Reverse Phase Protect" }},
            { 45, new string[]{ "", "" }},
            { 46, new string[]{ "O3GeneratorAlarm", "O3 Generator Alarm" }},
            { 47, new string[]{ "PMVacPumpAlarm", "PM Vacuum Pump Alarm" }},
            { 48, new string[]{ "ChuckServoAlarm", "Chuck Servo Alarm" }},
            { 49, new string[]{ "", "" }},
            { 50, new string[]{ "ChuckLiftUpperLimit", "Chuck Lift Upper Limit" }},
            { 51, new string[]{ "ChuckLiftLowerLimit", "Chuck Lift Lower Limit" }},
            { 52, new string[]{ "V1OpenAlarm", "Angle Valve V1 Open Alarm" }},
            { 53, new string[]{ "V1CloseAlarm", "Angle Valve V1 Close Alarm" }},
            { 54, new string[]{ "V3OpenAlarm", "Angle Valve V3 Open Alarm" }},
            { 55, new string[]{ "V3CloseAlarm", "Angle Valve V3 Close Alarm" }},
            { 56, new string[]{ "V4OpenAlarm", "Angle Valve V4 Open Alarm" }},
            { 57, new string[]{ "V4CloseAlarm", "Angle Valve V4 Close Alarm" }},
            { 58, new string[]{ "1#SourceLiquidLevelHH", "1# Source Liquid Level HH" }},
            { 59, new string[]{ "1#SourceLiquidLevelLL", "1# Source Liquid Level LL" }},
            { 60, new string[]{ "2#SourceLiquidLevelHH", "2# Source Liquid Level HH" }},
            { 61, new string[]{ "2#SourceLiquidLevelLL", "2# Source Liquid Level LL" }},
            { 62, new string[]{ "3#SourceLiquidLevelHH", "3# Source Liquid Level HH" }},
            { 63, new string[]{ "3#SourceLiquidLevelLL", "3# Source Liquid Level LL" }},
            { 64, new string[]{ "4#SourceLiquidLevelHH", "4# Source Liquid Level HH" }},
            { 65, new string[]{ "4#SourceLiquidLevelLL", "4# Source Liquid Level LL" }},
            { 66, new string[]{ "TC01SprayHeaterThermoCp", "TC01 Spray Heater Thermo Cp" }},
            { 67, new string[]{ "TC02BottomRingThermoCp", "TC02 Bottom Ring Thermo Cp", }},
            { 68, new string[]{ "TC03BottomAssistThermoCp", "TC03 Bottom Assist Thermo Cp" }},
            { 69, new string[]{ "TC04WaferHeaterThermoCpA", "TC04 Wafer Heater Thermo CpA" }},
            { 70, new string[]{ "TC05WaferHeaterThermoCpB", "TC05 Wafer Heater Thermo CpB" }},
            { 71, new string[]{ "TC06WaferHeaterThermoCpC", "TC06 Wafer Heater Thermo CpC" }},
            { 72, new string[]{ "TC07WaferHeaterThermoCpD", "TC07 Wafer Heater Thermo CpD" }},
            { 73, new string[]{ "TC08GasHeaterAThermoCp", "TC08GasHeaterAThermoCp" }},
            { 74, new string[]{ "TC09GasHeaterBThermoCp", "TC09GasHeaterBThermoCp" }},
            { 75, new string[]{ "TC10ChamTempAThermoCp", "TC10 Chamber TempA Thermo Cp" }},
            { 76, new string[]{ "TC11ChamTempBThermoCp", "TC11 Chamber TempB Thermo Cp" }},
            { 77, new string[]{ "TC12ChamTempCThermoCp", "TC12 Chamber TempC Thermo Cp" }},
            { 78, new string[]{ "TC13ChamTempDThermoCp", "TC13 Chamber TempD Thermo Cp" }},
            { 79, new string[]{ "TC14ChamTempEThermoCp", "TC14 Chamber TempE Thermo Cp" }},
            { 80, new string[]{ "TC15ChamTempFThermoCp", "TC15 Chamber TempF Thermo Cp" }},
            { 81, new string[]{ "TC16ChamTempGThermoCp", "TC16 Chamber TempG Thermo Cp" }},
            { 82, new string[]{ "TC17ChamTempHThermoCp", "TC17 Chamber TempH Thermo Cp" }},
            { 83, new string[]{ "TC18ChamTempIThermoCp", "TC18 Chamber TempI Thermo Cp" }},
            { 84, new string[]{ "TC19ChamTempJThermoCp", "TC19 Chamber TempJ Thermo Cp" }},
            { 85, new string[]{ "TC20ChamTempKThermoCp", "TC20 Chamber TempK Thermo Cp" }},
            { 86, new string[]{ "TC21ChamTempLThermoCp", "TC21 Chamber TempL Thermo Cp" }},
            { 87, new string[]{ "TC25PipeHeatThermoCpCH1", "TC25 Pipe Heat Thermo Cp CH1" }},
            { 88, new string[]{ "TC26PipeHeatThermoCpCH2", "TC26 Pipe Heat Thermo Cp CH2" }},
            { 89, new string[]{ "TC27PipeHeatThermoCpCH3", "TC27 Pipe Heat Thermo Cp CH3" }},
            { 90, new string[]{ "TC28PipeHeatThermoCpCH4", "TC28 Pipe Heat Thermo Cp CH4" }},
            { 91, new string[]{ "TC29PipeHeatThermoCpCH5", "TC29 Pipe Heat Thermo Cp CH5" }},
            { 92, new string[]{ "TC30PipeHeatThermoCpCH6", "TC30 Pipe Heat Thermo Cp CH6" }},
            { 93, new string[]{ "TC31PipeHeatThermoCpCH7", "TC31 Pipe Heat Thermo Cp CH7" }},
            { 94, new string[]{ "TC32PipeHeatThermoCpCH8", "TC32 Pipe Heat Thermo Cp CH8" }},
            { 95, new string[]{ "TC33PipeHeatThermoCpCH9", "TC33 Pipe Heat Thermo Cp CH9" }},
            { 96, new string[]{ "TC34PipeHeatThermoCpCH10", "TC34 Pipe Heat Thermo Cp CH10" }},
            { 97, new string[]{ "TC35PipeHeatThermoCpCH11", "TC35 Pipe Heat Thermo Cp CH11" }},
            { 98, new string[]{ "TC36PipeHeatThermoCpCH12", "TC36 Pipe Heat Thermo Cp CH12" }},
            { 99, new string[]{ "TC37PipeHeatThermoCpCH13", "TC37 Pipe Heat Thermo Cp CH13" }},
            { 100, new string[]{ "TC38PipeHeatThermoCpCH14", "TC38 Pipe Heat Thermo Cp CH14" }},
            { 101, new string[]{ "TC39PipeHeatThermoCpCH15", "TC39 Pipe Heat Thermo Cp CH15" }},
            { 102, new string[]{ "TC40PipeHeatThermoCpCH16", "TC40 Pipe Heat Thermo Cp CH16" }},
            { 103, new string[]{ "TC41PipeHeatThermoCpCH17", "TC41 Pipe Heat Thermo Cp CH17" }},
            { 104, new string[]{ "TC42PipeHeatThermoCpCH18", "TC42 Pipe Heat Thermo Cp CH18" }},
            { 105, new string[]{ "TC43PipeHeatThermoCpCH19", "TC43 Pipe Heat Thermo Cp CH19" }},
            { 106, new string[]{ "TC44PipeHeatThermoCpCH20", "TC44 Pipe Heat Thermo Cp CH20" }},
            { 107, new string[]{ "TC45BaSucHeat1ThermoCpCH21", "TC45 BaSuc Heat1 Thermo Cp CH21" }},
            { 108, new string[]{ "TC46BaSucHeat2ThermoCpCH22", "TC46 BaSuc Heat2 Thermo Cp CH22" }},
            { 109, new string[]{ "TC47BaSucHeat3ThermoCpCH23", "TC47 BaSuc Heat3 Thermo Cp CH23" }},
            { 110, new string[]{ "TC49AngleValveHeaterAThermoCp", "TC49 Angle Valve HeaterA Thermo Cp" }},
            { 111, new string[]{ "TC50AngleValveHeaterBThermoCp", "TC50 Angle Valve HeaterB Thermo Cp" }},
            { 112, new string[]{ "TC51AngleValveHeaterCThermoCp", "TC51 Angle Valve HeaterC Thermo Cp" }},
            { 113, new string[]{ "", "" }},
            { 114, new string[]{ "TC53Source1ThermoCp", "TC53 Source1 Thermo Cp" }},
            { 115, new string[]{ "TC54Source2ThermoCp", "TC54 Source2 Thermo Cp" }},
            { 116, new string[]{ "TC55Source3ThermoCp", "TC55 Source3 Thermo Cp" }},
            { 117, new string[]{ "TC56Source4ThermoCp", "TC56 Source4 Thermo Cp" }},
            { 118, new string[]{ "CoolWaterTemp1", "Cool Water Temp1" }},
            { 119, new string[]{ "CoolWaterTemp2", "Cool Water Temp2" }},
            { 120, new string[]{ "CoolWaterTemp3", "Cool Water Temp3" }},
            { 121, new string[]{ "CoolWaterTemp4", "Cool Water Temp4" }},
            { 122, new string[]{ "CoolWaterTemp5", "Cool Water Temp5" }},
            { 123, new string[]{ "CoolWaterTemp6", "Cool Water Temp6" }},
            { 124, new string[]{ "CoolWaterTemp7", "Cool Water Temp7" }},
            { 125, new string[]{ "CoolWaterTemp8", "Cool Water Temp8" }},
            { 126, new string[]{ "CoolWaterFlowDetect1", "Cool Water Flow Detect1" }},
            { 127, new string[]{ "CoolWaterFlowDetect2", "Cool Water Flow Detect2" }},
            { 128, new string[]{ "CoolWaterFlowDetect3", "Cool Water Flow Detect3" }},
            { 129, new string[]{ "CoolWaterFlowDetect4", "Cool Water Flow Detect4" }},
            { 130, new string[]{ "CoolWaterFlowDetect5", "Cool Water Flow Detect5" }},
            { 131, new string[]{ "CoolWaterFlowDetect6", "Cool Water Flow Detect6" }},
            { 132, new string[]{ "CoolWaterFlowDetect7", "Cool Water Flow Detect7" }},
            { 133, new string[]{ "CoolWaterFlowDetect8", "Cool Water Flow Detect8" }},
            { 134, new string[]{ "", "" }},
            { 135, new string[]{ "MFC1FlowAlarm", "MFC1 Flow Alarm" }},
            { 136, new string[]{ "MFC2FlowAlarm", "MFC2 Flow Alarm" }},
            { 137, new string[]{ "MFC3FlowAlarm", "MFC3 Flow Alarm" }},
            { 138, new string[]{ "MFC4FlowAlarm", "MFC4 Flow Alarm" }},
            { 139, new string[]{ "MFC5FlowAlarm", "MFC5 Flow Alarm" }},
            { 140, new string[]{ "MFC6FlowAlarm", "MFC6 Flow Alarm" }},
            { 141, new string[]{ "MFC7FlowAlarm", "MFC7 Flow Alarm" }},
            { 142, new string[]{ "MFC8FlowAlarm", "MFC8 Flow Alarm" }},
            { 143, new string[]{ "MFC9FlowAlarm", "MFC9 Flow Alarm" }},
            { 144, new string[]{ "MFC10FlowAlarm", "MFC10 Flow Alarm" }},
            { 145, new string[]{ "MFC11FlowAlarm", "MFC11 Flow Alarm" }},
            { 146, new string[]{ "", "" }},
            { 147, new string[]{ "1#SubStationCommFail", "1# Substation Communication Failure" }},
            { 148, new string[]{ "2#SubStationCommFail", "2# Substation Communication Failure" }},
            { 149, new string[]{ "3#SubStationCommFail", "3# Substation Communication Failure" }},
            { 150, new string[]{ "4#SubStationCommFail", "4# Substation Communication Failure" }},
            { 151, new string[]{ "5#SubStationCommFail", "5# Substation Communication Failure" }},
            { 152, new string[]{ "6#SubStationCommFail", "6# Substation Communication Failure" }},
            { 153, new string[]{ "7#SubStationCommFail", "7# Substation Communication Failure" }},
            { 154, new string[]{ "8#SubStationCommFail", "8# Substation Communication Failure" }},
            { 155, new string[]{ "9#SubStationCommFail", "9# Substation Communication Failure" }},
            { 156, new string[]{ "10#SubStationCommFail", "10# Substation Communication Failure" }},
            { 157, new string[]{ "11#SubStationCommFail", "11# Substation Communication Failure" }},
            { 158, new string[]{ "12#SubStationCommFail", "12# Substation Communication Failure" }},
            { 159, new string[]{ "13#SubStationCommFail", "13# Substation Communication Failure" }},
            { 160, new string[]{ "14#SubStationCommFail", "14# Substation Communication Failure" }},
            { 161, new string[]{ "15#SubStationCommFail", "15# Substation Communication Failure" }},
            { 162, new string[]{ "16#SubStationCommFail", "16# Substation Communication Failure" }},
            { 163, new string[]{ "17#SubStationCommFail", "17# Substation Communication Failure" }},
            { 164, new string[]{ "18#SubStationCommFail", "18# Substation Communication Failure" }},
            { 165, new string[]{ "19#SubStationCommFail", "19# Substation Communication Failure" }},
            { 166, new string[]{ "20#SubStationCommFail", "20# Substation Communication Failure" }},
            { 167, new string[]{ "21#SubStationCommFail", "21# Substation Communication Failure" }},
            { 168, new string[]{ "22#SubStationCommFail", "22# Substation Communication Failure" }},
            { 169, new string[]{ "23#SubStationCommFail", "23# Substation Communication Failure" }},
            { 170, new string[]{ "24#SubStationCommFail", "24# Substation Communication Failure" }},
            { 171, new string[]{ "25#SubStationCommFail", "25# Substation Communication Failure" }},
            { 172, new string[]{ "26#SubStationCommFail", "26# Substation Communication Failure" }},
            { 173, new string[]{ "27#SubStationCommFail", "27# Substation Communication Failure" }},
            { 174, new string[]{ "28#SubStationCommFail", "28# Substation Communication Failure" }},
            { 175, new string[]{ "29#SubStationCommFail", "29# Substation Communication Failure" }},
            { 176, new string[]{ "30#SubStationCommFail", "30# Substation Communication Failure" }},
            { 177, new string[]{ "31#SubStationCommFail", "31# Substation Communication Failure" }},
            { 178, new string[]{ "32#SubStationCommFail", "32# Substation Communication Failure" }},
            { 179, new string[]{ "33#SubStationCommFail", "33# Substation Communication Failure" }},
            { 180, new string[]{ "34#SubStationCommFail", "34# Substation Communication Failure" }},
            { 181, new string[]{ "PumpTimeoutAlarm", "Pump Vacuum Timeout Alarm" }},
            { 182, new string[]{ "VentTimeoutAlarm", "Vent Vacuum Timeout Alarm" }},
            { 183, new string[]{ "ChuckPosAlarm", "Chuck Position Exception Alarm" }},
            { 184, new string[]{ "ProcessTempTimeoutAlarm", "Process Temp Check Timeout Alarm" }},
            { 185, new string[]{ "ProcessPresHighAlarm", "Process Pressure High Exception Alarm" }},
            { 186, new string[]{ "ChamPresSwitchVS20", "Chamber Pressure Switch VS20 Exception" }},
            { 187, new string[]{ "", "" }},
            { 188, new string[]{ "", "" }},
            { 189, new string[]{ "", "" }},
            { 190, new string[]{ "", "" }},
            { 191, new string[]{ "TC45PipeHeatThermoCpCH21", "TC45 Pipe Heat Thermo Cp CH21" }},
            { 192, new string[]{ "TC46PipeHeatThermoCpCH22", "TC46 Pipe Heat Thermo Cp CH22" }},
            { 193, new string[]{ "TC47PipeHeatThermoCpCH23", "TC47 Pipe Heat Thermo Cp CH23" }},
            { 194, new string[]{ "TC48PipeHeatThermoCpCH24", "TC48 Pipe Heat Thermo Cp CH24" }},
            { 195, new string[]{ "TC49PipeHeatThermoCpCH25", "TC49 Pipe Heat Thermo Cp CH25" }},
            { 196, new string[]{ "TC50PipeHeatThermoCpCH26", "TC50 Pipe Heat Thermo Cp CH26" }},
            { 197, new string[]{ "TC51PipeHeatThermoCpCH27", "TC51 Pipe Heat Thermo Cp CH27" }},
            { 198, new string[]{ "TC52PipeHeatThermoCpCH28", "TC52 Pipe Heat Thermo Cp CH28" }},
            { 199, new string[]{ "TC53PipeHeatThermoCpCH29", "TC53 Pipe Heat Thermo Cp CH29" }},
            { 200, new string[]{ "TC54PipeHeatThermoCpCH30", "TC54 Pipe Heat Thermo Cp CH30" }},
            { 201, new string[]{ "TC55PipeHeatThermoCpCH31", "TC55 Pipe Heat Thermo Cp CH31" }},
            { 202, new string[]{ "TC56PipeHeatThermoCpCH32", "TC56 Pipe Heat Thermo Cp CH32" }},
            { 203, new string[]{ "TC57PipeHeatThermoCpCH33", "TC57 Pipe Heat Thermo Cp CH33" }},
        };

        #endregion
    }
}
