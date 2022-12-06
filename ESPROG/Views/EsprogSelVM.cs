using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ESPROG.Views
{
    class EsprogSelVM : BaseViewModel
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

        public List<string> GateCtrlModeList { get; private set; }

        private string selectedGateCtrlMode;
        public string SelectedGateCtrlMode
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

        public EsprogSelVM()
        {
            PortList = new();
            selectedPort = string.Empty;
            esprogInfo = string.Empty;
            esprogCompileTime = string.Empty;
            portNotLocked = true;
            GateCtrlModeList = new() { "On Demand", "Always On", "Always Off" };
            selectedGateCtrlMode = GateCtrlModeList[0];
        }
    }
}
