using System;
using System.Collections.Generic;
using System.Linq;
using Aitex.Sorter.Common;
using MECF.Framework.UI.Client.ClientBase;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

namespace MECF.Framework.UI.Client.ClientControls.LoadLockControls
{
    /// <summary>
    /// LoadLockTopView.xaml 的交互逻辑
    /// </summary>
    public partial class LoadLock : UserControl
    {
        public LoadLock()
        {
            InitializeComponent();
        }

        #region UnitData (DependencyProperty)
        public ModuleInfo UnitData
        {
            get { return (ModuleInfo)GetValue(UnitDataProperty); }
            set { SetValue(UnitDataProperty, value); }
        }
        public static readonly DependencyProperty UnitDataProperty =
            DependencyProperty.Register("UnitData", typeof(ModuleInfo), typeof(LoadLock), new UIPropertyMetadata(null));

        public FoupDoorState ATMDoorStatus
        {
            get { return (FoupDoorState)GetValue(ATMDoorStatusProperty); }
            set { SetValue(ATMDoorStatusProperty, value); }
        }
        public static readonly DependencyProperty ATMDoorStatusProperty =
            DependencyProperty.Register("ATMDoorStatus", typeof(FoupDoorState), typeof(LoadLock), new UIPropertyMetadata(FoupDoorState.Close));

        public FoupDoorState SlitValveStatus
        {
            get { return (FoupDoorState)GetValue(SlitValveStatusProperty); }
            set { SetValue(SlitValveStatusProperty, value); }
        }
        public static readonly DependencyProperty SlitValveStatusProperty =
            DependencyProperty.Register("SlitValveStatus", typeof(FoupDoorState), typeof(LoadLock), new UIPropertyMetadata(FoupDoorState.Close));

        public int CurrentSlot
        {
            get { return (int)GetValue(CurrentSlotProperty); }
            set { SetValue(CurrentSlotProperty, value); }
        }
        public static readonly DependencyProperty CurrentSlotProperty =
            DependencyProperty.Register("CurrentSlot", typeof(int), typeof(LoadLock), new UIPropertyMetadata(0));
        #endregion  
    }
}
