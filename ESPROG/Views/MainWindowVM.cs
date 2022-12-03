using System.Collections.Generic;

namespace ESPROG.Views
{
    internal class MainWindowVM : BaseViewModel
    {
        private List<string> portList;
        public List<string> PortList
        {
            get => this.portList;
            set => SetProperty(ref this.portList, value);
        }

        private string selectedPort;
        public string SelectedPort
        {
            get => this.selectedPort;
            set => SetProperty(ref this.selectedPort, value);
        }

        private string esprogInfo;
        public string EsprogInfo
        {
            get => this.esprogInfo;
            set => SetProperty(ref this.esprogInfo, value);
        }

        private string esprogCompileTime;
        public string EsprogCompileTime
        {
            get => this.esprogCompileTime;
            set => SetProperty(ref this.esprogCompileTime, value);
        }

        private List<string> chipAddrList;
        public List<string> ChipAddrList
        {
            get => this.chipAddrList;
            set => SetProperty(ref this.chipAddrList, value);
        }

        private string selectedChipAddr;
        public string SelectedChipAddr
        {
            get => this.selectedChipAddr;
            set => SetProperty(ref this.selectedChipAddr, value);
        }

        private bool enableAutoChipAddr;
        public bool EnableAutoChipAddr
        {
            get => this.enableAutoChipAddr;
            set => SetProperty(ref this.enableAutoChipAddr, value);
        }

        private string chipInfo;
        public string ChipInfo
        {
            get => this.chipInfo;
            set => SetProperty(ref this.chipInfo, value);
        }

        private string fwFile;
        public string FwFile
        {
            get => this.fwFile;
            set => SetProperty(ref this.fwFile, value);
        }

        public FwContentVM FwContent { get; private set; }

        public MainWindowVM()
        {
            portList = new();
            selectedPort = string.Empty;
            esprogInfo = string.Empty;
            esprogCompileTime = string.Empty;
            chipAddrList = new();
            selectedChipAddr = string.Empty;
            chipInfo = string.Empty;
            fwFile = string.Empty;
            FwContent = new();
        }
    }
}
