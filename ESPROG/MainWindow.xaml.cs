using ESPROG.Services;
using ESPROG.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ESPROG
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowVM view;
        private readonly LogService log;
        private readonly UartService uart;
        private readonly NuProgService nuprog;

        public MainWindow()
        {
            InitializeComponent();
            view = new();
            DataContext = view;

            log = new(TextBoxLog);
            uart = new(log, TextBoxCmds);
            nuprog = new(log, uart);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadSerialPort();
        }

        private void ReloadSerialPort()
        {
            view.EsprogSelView.PortList.Clear();
            string[] ports = uart.Scan();
            if (ports.Length > 0)
            {
                foreach (string port in ports)
                {
                    view.EsprogSelView.PortList.Add(port);
                }
                if (!ports.Contains(view.EsprogSelView.SelectedPort))
                {
                    view.EsprogSelView.SelectedPort = ports[0];
                }
            }
        }

        private async Task<bool> TryConnectPort(string port)
        {
            view.EsprogSelView.EsprogInfo = string.Empty;
            view.EsprogSelView.EsprogCompileTime = string.Empty;
            if (!uart.Open(port))
            {
                log.Debug(string.Format("Port({0}) open fail", port));
                return false;
            }
            string? version = await nuprog.GetEsprogVersionAsync();
            if (version == null)
            {
                log.Debug(string.Format("Can not find ESPROG on port({0})", port));
                return false;
            }
            string? compileTime = await nuprog.GetEsprogCompileTimeAsync();
            if (compileTime == null)
            {
                log.Debug(string.Format("Can not find ESPROG on port({0})", port));
                return false;
            }
            view.EsprogSelView.EsprogInfo = version;
            view.EsprogSelView.EsprogCompileTime = compileTime;
            return true;
        }

        private void ButtonReloadPortList_Click(object sender, RoutedEventArgs e)
        {
            ReloadSerialPort();
        }

        private async void ButtonOpenPort_Click(object sender, RoutedEventArgs e)
        {
            if (!view.PortConnected)
            {
                if (await TryConnectPort(view.EsprogSelView.SelectedPort))
                {
                    view.PortConnected = true;
                }
            }
            else
            {
                uart.Close();
                view.PortConnected = false;
            }
        }

        private async void ButtonAutodetectPort_Click(object sender, RoutedEventArgs e)
        {
            ReloadSerialPort();
            foreach (string port in view.EsprogSelView.PortList)
            {
                if (await TryConnectPort(port))
                {
                    view.EsprogSelView.SelectedPort = port;
                    view.PortConnected = true;
                    return;
                }
            }
        }
    }
}
