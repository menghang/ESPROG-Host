namespace ESPROG.Views
{
    internal class MainWindowVM : BaseViewModel
    {
        private bool portConnected;
        public bool PortConnected
        {
            get => this.portConnected;
            set
            {
                this.portConnected = value;
                EsprogSelView.PortNotLocked = !value;
                ChipSelView.PortConnected = value;
            }
        }

        private string fwFile;
        public string FwFile
        {
            get => this.fwFile;
            set => SetProperty(ref this.fwFile, value);
        }

        public EsprogSelVM EsprogSelView { get; private set; }
        public ChipSelVM ChipSelView { get; private set; }
        public FwContentVM FwContent { get; private set; }

        public MainWindowVM()
        {
            portConnected = false;
            fwFile = string.Empty;
            EsprogSelView = new();
            ChipSelView = new();
            FwContent = new();
        }
    }
}
