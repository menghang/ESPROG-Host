using System;

namespace ESPROG.Views
{
    internal class MainWindowVM : BaseViewModel
    {
        private bool portConnected;
        public bool PortConnected
        {
            get => portConnected;
            set
            {
                portConnected = value;
                EsprogSelView.PortNotLocked = !value;
                ChipSelView.PortConnected = value;
            }
        }

        private string fwFile;
        public string FwFile
        {
            get => fwFile;
            set => SetProperty(ref fwFile, value);
        }

        public EsprogSelVM EsprogSelView { get; private set; }
        public ChipSelVM ChipSelView { get; private set; }
        public FwContentVM WriteFwContent { get; private set; }
        public FwContentVM ReadFwContent { get; private set; }

        public MainWindowVM()
        {
            portConnected = false;
            fwFile = string.Empty;
            EsprogSelView = new();
            ChipSelView = new();
            ChipSelView.SelectedChipChanged += ChipSelView_SelectedChipChanged;
            WriteFwContent = new();
            ReadFwContent = new();
            UpdateMaxFwSizeWithChip();
        }

        private void ChipSelView_SelectedChipChanged(object sender, EventArgs e)
        {
            UpdateMaxFwSizeWithChip();
        }

        private void UpdateMaxFwSizeWithChip()
        {
            WriteFwContent.MaxFwSize = ChipSelVM.ChipSizeDict[ChipSelView.SelectedChip];
            ReadFwContent.MaxFwSize = ChipSelVM.ChipSizeDict[ChipSelView.SelectedChip];
        }
    }
}
