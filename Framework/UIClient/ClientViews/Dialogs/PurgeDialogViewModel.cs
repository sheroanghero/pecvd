using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Dialogs
{
    public class PurgeDialogViewModel : DialogViewModel<string>
    {
        public int CycleCountValue { get; set; }
        public double PumpPressureValue { get; set; }
        public double VentPressureValue { get; set; }

        public string CycleCount { get; set; }
        public string PumpPressure { get; set; }
        public string VentPressure { get; set; }
 
        public void OK()
        {
            if (!int.TryParse(CycleCount, out int cycleCount) || cycleCount<0)
            {
                DialogBox.ShowWarning($"{CycleCount} is not a valid cycle count value");
                return;
            }

            if (!double.TryParse(PumpPressure, out double pumpPressureValue) || pumpPressureValue < 0)
            {
                DialogBox.ShowWarning($"{PumpPressure} is not a valid pump base value");
                return;
            }

            if (!double.TryParse(VentPressure, out double ventPressureValue) || ventPressureValue < 0)
            {
                DialogBox.ShowWarning($"{VentPressure} is not a valid vent pressure value");
                return;
            }

            if (pumpPressureValue >= ventPressureValue)
            {
                DialogBox.ShowWarning($"Pump pressure {pumpPressureValue} should be less than {ventPressureValue}");
                return;
            }

            CycleCountValue = cycleCount;
            PumpPressureValue = pumpPressureValue;
            VentPressureValue = ventPressureValue;

            IsCancel = false;
            TryClose(true);
        }

        public void Cancel()
        {
            IsCancel = true;
            TryClose(false);
        }

    }
}
