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
            }
        }

        public string BtnConnectPortText
        {
            get => portNotLocked ? "Connect Port" : "Close Port";
        }

        public EsprogSelVM()
        {
            PortList = new();
            selectedPort = string.Empty;
            esprogInfo = string.Empty;
            esprogCompileTime = string.Empty;
            portNotLocked = true;
        }
    }
}
