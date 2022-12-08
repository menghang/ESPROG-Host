using ESPROG.Utils;
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

        private byte? regAddr;
        public byte? RegAddr
        {
            get => regAddr;
            set => SetProperty(ref regAddr, value, nameof(RegAddrText));
        }
        public string RegAddrText
        {
            get => regAddr.HasValue ? HexUtil.GetHexStr(regAddr.Value) : string.Empty;
            set => SetProperty(ref regAddr, HexUtil.GetByteFromStr(value));
        }

        private byte? regVal;
        public byte? RegVal
        {
            get => regVal;
            set => SetProperty(ref regVal, value, nameof(RegValText));
        }
        public string RegValText
        {
            get => regVal.HasValue ? HexUtil.GetHexStr(regVal.Value) : string.Empty;
            set => SetProperty(ref regVal, HexUtil.GetByteFromStr(value));
        }

        private string sendCmd;
        public string SendCmd
        {
            get => sendCmd;
            set => SetProperty(ref sendCmd, value);
        }

        public EsprogSettingVM EsprogSettingView { get; private set; }
        public ChipSettingVM ChipSettingView { get; private set; }
        public FwContentVM WriteFwContent { get; private set; }
        public FwContentVM ReadFwContent { get; private set; }

        public MainWindowVM()
        {
            portConnected = false;
            fwFile = string.Empty;
            regAddr = null;
            regVal = null;
            sendCmd = string.Empty;
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
