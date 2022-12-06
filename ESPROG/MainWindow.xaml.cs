using ESPROG.Services;
using ESPROG.Utils;
using ESPROG.Views;
using Microsoft.Win32;
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
            log.Info(string.Format("Find ESPROG on port({0})", port));
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

        private void TextBoxFwFile_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Multiselect = false,
                Title = "Open firmware file",
                Filter = "Binary File (*.bin)|*.bin|All Files|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                view.FwFile = dialog.FileName;
                if (view.WriteFwContent.LoadFwFile(view.FwFile, out string logMsg))
                {
                    log.Info(logMsg);
                }
                else
                {
                    log.Error(logMsg);
                }
            }
        }

        private async Task<string?> GetChipInfo(string chipStr, string devAddrStr)
        {
            uint chip;
            switch (chipStr)
            {
                case "NU1705":
                    chip = 0x1705;
                    break;
                case "NU1708":
                    chip = 0x1708;
                    break;
                case "NU1718":
                    chip = 0x1718;
                    break;
                default:
                    return null;
            }
            if (!await nuprog.SetChip(chip))
            {
                log.Error(string.Format("Set chip ({0}) fail", chipStr));
                return null;
            }
            byte? devAddr = HexUtil.GetByteFromStr(devAddrStr);
            if (devAddr == null)
            {
                log.Error(string.Format("Wrong chip addr ({0})", devAddrStr));
                return null;
            }
            if (!await nuprog.SetDevAddr(devAddr.Value))
            {
                log.Error(string.Format("Set chip addr ({0}) fail", devAddr.Value));
                return null;
            }
            (byte chipPn, byte chipVersion)? chipInfo = await nuprog.GetChipInfo();
            if (chipInfo == null)
            {
                log.Error(string.Format("Get chip info fail"));
                return null;
            }
            uint? chipUID = await nuprog.GetChipUID();
            if (chipUID == null)
            {
                log.Error(string.Format("Get chip uid fail"));
                return null;
            }
            return string.Format("PN:{0}, VER:{1}, UID:{2}", HexUtil.GetHexStr(chipInfo.Value.chipPn),
                HexUtil.GetHexStr(chipInfo.Value.chipVersion), HexUtil.GetHexStr(chipUID.Value));
        }

        private async void ButtonGetChipInfo_Click(object sender, RoutedEventArgs e)
        {
            view.ChipSelView.ChipInfo = string.Empty;
            string? chipInfo = await GetChipInfo(view.ChipSelView.SelectedChip, view.ChipSelView.SelectedChipAddr);
            view.ChipSelView.ChipInfo = string.IsNullOrEmpty(chipInfo) ? string.Empty : chipInfo;
        }

        private void ButtonClearLog_Click(object sender, RoutedEventArgs e)
        {
            log.ClearLogBox();
        }

        private void ButtonClearCmds_Click(object sender, RoutedEventArgs e)
        {
            uart.ClearLogBox();
        }

        private async void ButtonAutodetectChip_Click(object sender, RoutedEventArgs e)
        {
            view.ChipSelView.ChipInfo = string.Empty;
            foreach (string chipStr in ChipSelVM.Chips.Keys)
            {
                foreach (string devAddrStr in ChipSelVM.Chips[chipStr])
                {
                    string? chipInfo = await GetChipInfo(chipStr, devAddrStr);
                    if (chipInfo != null)
                    {
                        view.ChipSelView.SelectedChip = chipStr;
                        view.ChipSelView.SelectedChipAddr = devAddrStr;
                        view.ChipSelView.ChipInfo = chipInfo;
                        return;
                    }
                }
            }
        }

        private async void ButtonFormatEsprogStorage_Click(object sender, RoutedEventArgs e)
        {
            if (await nuprog.FormatEsprog())
            {
                log.Info("Format ESPROG storage succeed");
            }
            else
            {
                log.Error("Format ESPROG storage fail");
            }
        }
    }
}
