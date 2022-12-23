using ESPROG.Models;
using ESPROG.Services;
using ESPROG.Utils;
using System.Collections.Generic;

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

        private string configFile;
        public string ConfigFile
        {
            get => configFile;
            set => SetProperty(ref configFile, value);
        }

        private string trimFile;
        public string TrimFile
        {
            get => trimFile;
            set => SetProperty(ref trimFile, value);
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

        private byte selectedWriteZone;
        public byte SelectedWriteZone
        {
            get => selectedWriteZone;
            set => SetProperty(ref selectedWriteZone, value);
        }
        public List<ComboBoxModel<string, byte>> WriteZoneList { get; private set; }

        private byte selectedReadZone;
        public byte SelectedReadZone
        {
            get => selectedReadZone;
            set => SetProperty(ref selectedReadZone, value);
        }
        public List<ComboBoxModel<string, byte>> ReadZoneList { get; private set; }

        public EsprogSettingVM EsprogSettingView { get; private set; }
        public ChipSettingVM ChipSettingView { get; private set; }
        public FwContentVM WriteFwContent { get; private set; }
        public FwContentVM WriteConfigContent { get; private set; }
        public FwContentVM WriteTrimContent { get; private set; }
        public FwContentVM ReadFwContent { get; private set; }
        public FwContentVM ReadConfigContent { get; private set; }
        public FwContentVM ReadTrimContent { get; private set; }
        public ProgressVM ProgressView { get; private set; }

        public MainWindowVM()
        {
            isPortConnected = false;
            isIdle = true;
            fwFile = string.Empty;
            configFile = string.Empty;
            trimFile = string.Empty;
            regAddr = null;
            regVal = null;
            sendCmd = string.Empty;
            EsprogSettingView = new();
            ChipSettingView = new();
            WriteFwContent = new(NuProgService.MTPAddrOffset);
            WriteConfigContent = new(NuProgService.CFGAddrOffset);
            WriteTrimContent = new(NuProgService.TrimAddrOffset);
            ReadFwContent = new(NuProgService.MTPAddrOffset);
            ReadConfigContent = new(NuProgService.CFGAddrOffset);
            ReadTrimContent = new(NuProgService.TrimAddrOffset);
            ProgressView = new();
            WriteZoneList = new() {
                new("Firmware", 0x01), new("Config", 0x02), new("Firmware + Config", 0x03), new("Trim", 0x04)
            };
            selectedWriteZone = WriteZoneList[0].Value;
            ReadZoneList = new() { new("Firmware", 0x01), new("Config", 0x02), new("Trim", 0x04) };
            selectedReadZone = WriteZoneList[0].Value;
        }
    }
}
