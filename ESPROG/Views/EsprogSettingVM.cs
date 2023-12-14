using ESPROG.Models;
using System;
using System.Collections.Generic;

namespace ESPROG.Views
{
    class EsprogSettingVM : BaseViewModel
    {
        private List<string> portList;
        public List<string> PortList
        {
            get => portList;
            set => SetProperty(ref portList, value);
        }

        private string selectedPort;
        public string SelectedPort
        {
            get => selectedPort;
            set => SetProperty(ref selectedPort, value);
        }

        private string esprogInfo;
        public string EsprogInfo
        {
            get => esprogInfo;
            set => SetProperty(ref esprogInfo, value);
        }

        private string esprogCompileTime;
        public string EsprogCompileTime
        {
            get => esprogCompileTime;
            set => SetProperty(ref esprogCompileTime, value);
        }

        public bool IsPortNotLocked
        {
            get => !isPortConnected;
        }

        public string BtnConnectPortText
        {
            get => isPortConnected ? "Disconnect ESPROG" : "Connect ESPROG";
        }

        private bool isPortConnected;
        public bool IsPortConnected
        {
            get => isPortConnected;
            set
            {
                if (isPortConnected != value)
                {
                    SetProperty(ref isPortConnected, value);
                    OnPropertyChanged(nameof(IsPortNotLocked));
                    OnPropertyChanged(nameof(BtnConnectPortText));
                }
            }
        }

        public delegate void SelectedVddModeChangedHandler(object sender, VddModeEventArgs e);
        public event SelectedVddModeChangedHandler? SelectedVddModeChanged;

        public List<ComboBoxModel<string, byte>> VddCtrlModeList { get; private set; }

        private byte selectedVddCtrlMode;
        public byte SelectedVddCtrlMode
        {
            get => selectedVddCtrlMode;
            set
            {
                if (selectedVddCtrlMode != value)
                {
                    SelectedVddModeChanged?.Invoke(this, new(value, selectedVddVol, selectedVddCtrlMode, selectedVddVol));
                    SetProperty(ref selectedVddCtrlMode, value);
                }
            }
        }

        public List<ComboBoxModel<string, byte>> VddVolList { get; private set; }

        private byte selectedVddVol;
        public byte SelectedVddVol
        {
            get => selectedVddVol;
            set
            {
                if (selectedVddVol != value)
                {
                    SelectedVddModeChanged?.Invoke(this, new(selectedVddCtrlMode, value, selectedVddCtrlMode, selectedVddVol));
                    SetProperty(ref selectedVddVol, value);
                }
            }
        }

        public class VddModeEventArgs : EventArgs
        {
            public byte NewCtrlMode { get; private set; }
            public byte NewVol { get; private set; }
            public byte LastCtrlMode { get; private set; }
            public byte LastVol { get; private set; }
            public VddModeEventArgs(byte newCtrlMode, byte newVol, byte lastCtrlMode, byte lastVol)
            {
                NewCtrlMode = newCtrlMode;
                NewVol = newVol;
                LastCtrlMode = lastVol;
                LastVol = lastVol;
            }
        }

        public void UpdateSelectedVddCtrlMode(byte mode)
        {
            SetProperty(ref selectedVddCtrlMode, mode, nameof(SelectedVddCtrlMode));
        }

        public void UpdateSelectedVddVol(byte vol)
        {
            SetProperty(ref selectedVddVol, vol, nameof(SelectedVddVol));
        }

        public EsprogSettingVM()
        {
            portList = new();
            selectedPort = string.Empty;
            esprogInfo = string.Empty;
            esprogCompileTime = string.Empty;
            isPortConnected = false;
            VddCtrlModeList = new() { new("On Demand", 0x00), new("Always On", 0x01), new("Always Off", 0x02) };
            selectedVddCtrlMode = VddCtrlModeList[0].Value;
            VddVolList = new() { new("5V", 0x00), new("3.3V", 0x01) };
            selectedVddVol = VddVolList[0].Value;
        }
    }
}
