using ESPROG.Utils;
using System;
using System.Windows.Media;

namespace ESPROG.Views
{
    internal class MainWindowVM : BaseViewModel
    {
        private bool isPortConnected;
        public bool IsPortConnected
        {
            get => isPortConnected;
            set
            {
                SetProperty(ref isPortConnected, value);
                EsprogSettingView.IsPortConnected = value;
                ChipSettingView.IsPortConnected = value;
            }
        }

        private bool isIdle;
        public bool IsIdle
        {
            get => isIdle;
            set => SetProperty(ref isIdle, value);
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
        public ProgressVM ProgressView { get; private set; }

        public MainWindowVM()
        {
            isPortConnected = false;
            isIdle = true;
            fwFile = string.Empty;
            regAddr = null;
            regVal = null;
            sendCmd = string.Empty;
            EsprogSettingView = new();
            ChipSettingView = new();
            WriteFwContent = new();
            ReadFwContent = new();
            ProgressView = new();
        }
    }
}
