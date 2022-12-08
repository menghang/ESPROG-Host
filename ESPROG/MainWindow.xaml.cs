using ESPROG.Models;
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
            view.EsprogSettingView.SelectedGateCtrlModeChanged += EsprogSelView_SelectedGateCtrlModeChanged;
        }

        private async void EsprogSelView_SelectedGateCtrlModeChanged(object sender, EventArgs e)
        {
            if (await nuprog.SetGateCtrl(view.EsprogSettingView.SelectedGateCtrlMode))
            {
                log.Info(string.Format("Set gate mode ({0}) succeed", view.EsprogSettingView.SelectedGateCtrlMode));
            }
            else
            {
                log.Error(string.Format("Set gate mode ({0}) fail", view.EsprogSettingView.SelectedGateCtrlMode));
            }
        }

        private void ReloadSerialPort()
        {
            view.EsprogSettingView.PortList.Clear();
            string[] ports = uart.Scan();
            if (ports.Length > 0)
            {
                foreach (string port in ports)
                {
                    view.EsprogSettingView.PortList.Add(port);
                }
                if (!ports.Contains(view.EsprogSettingView.SelectedPort))
                {
                    view.EsprogSettingView.SelectedPort = ports[0];
                }
            }
        }

        private async Task<bool> TryConnectPort(string port)
        {
            view.EsprogSettingView.EsprogInfo = string.Empty;
            view.EsprogSettingView.EsprogCompileTime = string.Empty;
            if (!uart.Open(port))
            {
                log.Debug(string.Format("Port({0}) open fail", port));
                return false;
            }
            string? version = await nuprog.GetEsprogVersionAsync();
            if (version == null)
            {
                log.Debug(string.Format("Can not get ESPROG version on port({0})", port));
                return false;
            }
            string? compileTime = await nuprog.GetEsprogCompileTimeAsync();
            if (compileTime == null)
            {
                log.Debug(string.Format("Can not get ESPROG compile time on port({0})", port));
                return false;
            }
            byte? gateCtrlMode = await nuprog.GetGateCtrl();
            if (gateCtrlMode == null)
            {
                log.Debug(string.Format("Can not get ESPROG gate ctrl mode on port({0})", port));
                return false;
            }
            view.EsprogSettingView.EsprogInfo = version;
            view.EsprogSettingView.EsprogCompileTime = compileTime;
            view.EsprogSettingView.UpdateSelectedGateCtrlMode(gateCtrlMode.Value);
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
                if (await TryConnectPort(view.EsprogSettingView.SelectedPort))
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
            foreach (string port in view.EsprogSettingView.PortList)
            {
                if (await TryConnectPort(port))
                {
                    view.EsprogSettingView.SelectedPort = port;
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

        private async Task<string?> GetChipInfo(uint chip, byte devAddr)
        {
            if (!await nuprog.SetChipAndAddr(chip, devAddr))
            {
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
            view.ChipSettingView.ChipInfo = string.Empty;
            string? chipInfo = await GetChipInfo(view.ChipSettingView.SelectedChip, view.ChipSettingView.SelectedChipAddr);
            view.ChipSettingView.ChipInfo = string.IsNullOrEmpty(chipInfo) ? string.Empty : chipInfo;
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
            view.ChipSettingView.ChipInfo = string.Empty;
            foreach (uint chip in ChipSettingVM.ChipDict.Keys)
            {
                foreach (ComboBoxModel<string, byte> devAddr in ChipSettingVM.ChipDict[chip])
                {
                    string? chipInfo = await GetChipInfo(chip, devAddr.Value);
                    if (chipInfo != null)
                    {
                        view.ChipSettingView.SelectedChip = chip;
                        view.ChipSettingView.SelectedChipAddr = devAddr.Value;
                        view.ChipSettingView.ChipInfo = chipInfo;
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

        private async void ButtonProgChip_Click(object sender, RoutedEventArgs e)
        {
            if (!await SendFwToESPROG())
            {
                return;
            }
            if (!await nuprog.FwWriteStart())
            {
                log.Error("Program chip fail");
                return;
            }
            log.Error("Program chip succeed");
            return;
        }

        private async Task<bool> SendFwToESPROG()
        {
            if (view.WriteFwContent.FwData == null)
            {
                log.Error("Firmware is not available");
                return false;
            }
            if (!await nuprog.SetChipAndAddr(view.ChipSettingView.SelectedChip, view.ChipSettingView.SelectedChipAddr))
            {
                return false;
            }
            if (!await nuprog.WriteFwToEsprog(view.WriteFwContent.FwData, view.WriteFwContent.MaxFwSize))
            {
                return false;
            }
            if (!await nuprog.FwWriteChecksum(view.WriteFwContent.FwData))
            {
                log.Error("Write checksum fail");
                return false;
            }
            return true;
        }

        private async void ButtonProgEsprog_Click(object sender, RoutedEventArgs e)
        {
            if (!await SendFwToESPROG())
            {
                return;
            }
            if (!await nuprog.SaveToEsprog())
            {
                log.Error("Save config to ESPROG fail");
                return;
            }
            log.Info("Save config to ESPROG succeed");
        }

        private async void ButtonReadChip_Click(object sender, RoutedEventArgs e)
        {
            if (!await nuprog.SetChipAndAddr(view.ChipSettingView.SelectedChip, view.ChipSettingView.SelectedChipAddr))
            {
                return;
            }
            if (!await nuprog.FwReadStart())
            {
                log.Error("Read firmware from chip fail");
                return;
            }
            byte[]? fwData = await nuprog.ReadFwFromEsprog(view.WriteFwContent.MaxFwSize);
            if (fwData == null)
            {
                return;
            }
            if (view.ReadFwContent.LoadFwData(fwData, out string logMsg))
            {
                log.Info(logMsg);
            }
            else
            {
                log.Error(logMsg);
            }
        }

        private void ButtonSaveFw_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new()
            {
                Title = "Save firmware file",
                Filter = "Binary File (*.bin)|*.bin"
            };
            if (dialog.ShowDialog() == true)
            {
                if (view.ReadFwContent.SaveFwData(dialog.FileName, out string logMsg))
                {
                    log.Info(logMsg);
                }
                else
                {
                    log.Error(logMsg);
                }
            }
        }
    }
}
