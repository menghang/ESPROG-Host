using ESPROG.Models;
using ESPROG.Services;
using ESPROG.Utils;
using System.Collections.Generic;

namespace ESPROG.Views
{
    internal class MainWindowVM : BaseViewModel
    {
        private string title = "ESPROG";
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

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
            set => SetProperty(ref regAddr, HexUtil.GetU8FromStr(value));
        }

        private ushort? regAddr16;
        public ushort? RegAddr16
        {
            get => regAddr16;
            set => SetProperty(ref regAddr16, value, nameof(RegAddr16Text));
        }
        public string RegAddr16Text
        {
            get => regAddr16.HasValue ? HexUtil.GetHexStr(regAddr16.Value) : string.Empty;
            set => SetProperty(ref regAddr16, HexUtil.GetU16FromStr(value));
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
            set => SetProperty(ref regVal, HexUtil.GetU8FromStr(value));
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
            WriteFwContent = new(NuProgService.ChipDict[ChipSettingView.SelectedChip].MTP.Offset);
            WriteConfigContent = new(NuProgService.ChipDict[ChipSettingView.SelectedChip].Config.Offset);
            WriteTrimContent = new(NuProgService.ChipDict[ChipSettingView.SelectedChip].Trim.Offset);
            ReadFwContent = new(NuProgService.ChipDict[ChipSettingView.SelectedChip].MTP.Offset);
            ReadConfigContent = new(NuProgService.ChipDict[ChipSettingView.SelectedChip].Config.Offset);
            ReadTrimContent = new(NuProgService.ChipDict[ChipSettingView.SelectedChip].Trim.Offset);
            ProgressView = new();
            WriteZoneList = new() {
                new("Firmware", 0x01), new("Config", 0x02), new("Firmware + Config", 0x03), new("Trim", 0x04)
            };
            selectedWriteZone = WriteZoneList[0].Value;
            ReadZoneList = new() { new("Firmware", 0x01), new("Config", 0x02), new("Trim", 0x04) };
            selectedReadZone = WriteZoneList[0].Value;
        }

        public void LoadConfig(ConfigModel? config)
        {
            if (config == null)
            {
                return;
            }
            ChipSettingView.SelectedChip = config.Chip.Chip;
            ChipSettingView.SelectedChipAddr = config.Chip.DevAddr;
            FwFile = config.FwWrite.FwFileWrite;
            ConfigFile = config.FwWrite.ConfigFileWrite;
            TrimFile = config.FwWrite.TrimFileWrite;
        }

        public ConfigModel ExportConfig()
        {
            ConfigModel config = new();
            config.Chip.Chip = ChipSettingView.SelectedChip;
            config.Chip.DevAddr = ChipSettingView.SelectedChipAddr;
            config.FwWrite.FwFileWrite = FwFile;
            config.FwWrite.ConfigFileWrite = ConfigFile;
            config.FwWrite.TrimFileWrite = TrimFile;
            return config;
        }
    }
}
