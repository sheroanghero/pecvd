using System.Collections.ObjectModel;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.IOCore;

namespace MECF.Framework.Simulator.Core.IoProviders
{
    public class IOViewModel : TimerViewModelBase
    {
        public SimulatorIO Plc { get; set; }

        public ObservableCollection<NotifiableIoItem> DiItemList { get; set; }
        public ObservableCollection<NotifiableIoItem> DoItemList { get; set; }
        public ObservableCollection<NotifiableIoItem> AiItemList { get; set; }
        public ObservableCollection<NotifiableIoItem> AoItemList { get; set; }

        public IOViewModel(int port, string source, string ioMapPathFile) : base(nameof(IOViewModel))
        {
            Plc = new SimulatorIO(port, source, ioMapPathFile, "");

            DiItemList = Plc.DiItemList;
            DoItemList = Plc.DoItemList;
            AiItemList = Plc.AiItemList;
            AoItemList = Plc.AoItemList;
        }


        protected override void Poll()
        {


        }
    }
}
