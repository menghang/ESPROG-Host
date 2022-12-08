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
                EsprogSettingView.PortNotLocked = !value;
                ChipSettingView.PortConnected = value;
            }
        }

        private string fwFile;
        public string FwFile
        {
            get => fwFile;
            set => SetProperty(ref fwFile, value);
        }

        public EsprogSettingVM EsprogSettingView { get; private set; }
        public ChipSettingVM ChipSettingView { get; private set; }
        public FwContentVM WriteFwContent { get; private set; }
        public FwContentVM ReadFwContent { get; private set; }

        public MainWindowVM()
        {
            portConnected = false;
            fwFile = string.Empty;
            EsprogSettingView = new();
            ChipSettingView = new();
            ChipSettingView.SelectedChipChanged += ChipSelView_SelectedChipChanged;
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
            WriteFwContent.MaxFwSize = ChipSettingVM.ChipSizeDict[ChipSettingView.SelectedChip];
            ReadFwContent.MaxFwSize = ChipSettingVM.ChipSizeDict[ChipSettingView.SelectedChip];
        }
    }
}
