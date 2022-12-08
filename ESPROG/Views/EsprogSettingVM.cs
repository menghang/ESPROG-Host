using ESPROG.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ESPROG.Views
{
    class EsprogSettingVM : BaseViewModel
    {
        public ObservableCollection<string> PortList { get; private set; }

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

        private bool portNotLocked;
        public bool PortNotLocked
        {
            get => portNotLocked;
            set
            {
                SetProperty(ref portNotLocked, value);
                OnPropertyChanged(nameof(BtnConnectPortText));
                OnPropertyChanged(nameof(PortConnected));
            }
        }

        public string BtnConnectPortText
        {
            get => portNotLocked ? "Connect Port" : "Close Port";
        }

        public bool PortConnected
        {
            get => !portNotLocked;
        }

        public delegate void SelectedGateCtrlModeChangedHandler(object sender, EventArgs e);
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
                    SetProperty(ref selectedGateCtrlMode, value);
                    SelectedGateCtrlModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void UpdateSelectedGateCtrlMode(byte mode)
        {
            SetProperty(ref selectedGateCtrlMode, mode, nameof(SelectedGateCtrlMode));
        }

        public EsprogSettingVM()
        {
            PortList = new();
            selectedPort = string.Empty;
            esprogInfo = string.Empty;
            esprogCompileTime = string.Empty;
            portNotLocked = true;
            GateCtrlModeList = new() { new("On Demand", 0x00), new("Always On", 0x01), new("Always Off", 0x02) };
            selectedGateCtrlMode = GateCtrlModeList[0].Value;
        }
    }
}
