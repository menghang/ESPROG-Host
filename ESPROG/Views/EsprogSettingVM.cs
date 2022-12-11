using ESPROG.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

        public delegate void SelectedGateCtrlModeChangedHandler(object sender, GateCtrlModeEventArgs e);
        public event SelectedGateCtrlModeChangedHandler? SelectedGateCtrlModeChanged;

        public List<ComboBoxModel<string, byte>> GateCtrlModeList { get; private set; }

        private byte selectedGateCtrlMode;
        public byte SelectedGateCtrlMode
        {
            get => selectedGateCtrlMode;
            set
            {
                if (selectedGateCtrlMode != value)
                {
                    SelectedGateCtrlModeChanged?.Invoke(this, new(value, selectedGateCtrlMode));
                    SetProperty(ref selectedGateCtrlMode, value);
                }
            }
        }

        public class GateCtrlModeEventArgs : EventArgs
        {
            public byte NewMode { get; private set; }
            public byte LastMode { get; private set; }
            public GateCtrlModeEventArgs(byte newMode, byte lastMode)
            {
                NewMode = newMode;
                LastMode = lastMode;
            }
        }

        public void UpdateSelectedGateCtrlMode(byte mode)
        {
            SetProperty(ref selectedGateCtrlMode, mode, nameof(SelectedGateCtrlMode));
        }

        public EsprogSettingVM()
        {
            portList = new();
            selectedPort = string.Empty;
            esprogInfo = string.Empty;
            esprogCompileTime = string.Empty;
            isPortConnected = false;
            GateCtrlModeList = new() { new("On Demand", 0x00), new("Always On", 0x01), new("Always Off", 0x02) };
            selectedGateCtrlMode = GateCtrlModeList[0].Value;
        }
    }
}
