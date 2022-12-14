using ESPROG.Models;
using ESPROG.Services;
using ESPROG.Utils;
using ESPROG.Views;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UsbMonitor;

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
        private readonly UsbMonitorManager usbMonitor;
        private readonly Timer usbRemovalTimer, usbArrivalTimer;
        private static readonly int usbDelay = 100;

        public MainWindow()
        {
            InitializeComponent();
            view = new();
            DataContext = view;

            log = new(TextBoxLog);
            uart = new(log);
            nuprog = new(log, uart);
            usbMonitor = new(this);
            usbMonitor.UsbDeviceInterface += UsbMonitor_UsbDeviceInterface;

            usbRemovalTimer = new Timer(new((o) =>
            {
                usbRemovalTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                if (!ReloadSerialPort())
                {
                    uart.Close();
                    view.IsPortConnected = false;
                }
            }), null, Timeout.Infinite, Timeout.Infinite);

            usbArrivalTimer = new Timer(new((o) =>
            {
                usbArrivalTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                ReloadSerialPort();
            }), null, Timeout.Infinite, Timeout.Infinite);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadSerialPort();
            view.EsprogSettingView.SelectedGateCtrlModeChanged += EsprogSelView_SelectedGateCtrlModeChanged;
        }

        private async void EsprogSelView_SelectedGateCtrlModeChanged(object sender, EsprogSettingVM.GateCtrlModeEventArgs e)
        {
            view.IsIdle = false;
            view.ProgressView.SetSate(ProgressVM.State.Running);
            if (!await nuprog.SetGateCtrl(e.NewMode))
            {
                log.Error(string.Format("Set gate mode ({0}) fail", view.EsprogSettingView.SelectedGateCtrlMode));
                view.EsprogSettingView.UpdateSelectedGateCtrlMode(e.LastMode);
                view.ProgressView.SetSate(ProgressVM.State.Fail);
            }
            else
            {
                log.Info(string.Format("Set gate mode ({0}) succeed", view.EsprogSettingView.SelectedGateCtrlMode));
                view.ProgressView.SetSate(ProgressVM.State.Succeed);
            }
            view.IsIdle = true;
        }

        private bool ReloadSerialPort()
        {
            List<string> ports = uart.Scan();
            view.EsprogSettingView.PortList = ports;
            if (ports.Count == 0)
            {
                view.EsprogSettingView.SelectedPort = string.Empty;
                return false;
            }
            else if (!ports.Contains(view.EsprogSettingView.SelectedPort))
            {
                view.EsprogSettingView.SelectedPort = ports[0];
                return false;
            }
            return true;
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
            string? version = await nuprog.GetEsprogInfoAsync();
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
            if (!view.IsPortConnected)
            {
                if (await TryConnectPort(view.EsprogSettingView.SelectedPort))
                {
                    view.IsPortConnected = true;
                }
            }
            else
            {
                uart.Close();
                view.IsPortConnected = false;
            }
        }

        private void UsbMonitor_UsbDeviceInterface(object? sender, UsbEventDeviceInterfaceArgs e)
        {
            switch (e.Action)
            {
                case UsbDeviceChangeEvent.RemoveComplete:
                    usbRemovalTimer.Change(usbDelay, Timeout.Infinite);
                    break;
                case UsbDeviceChangeEvent.Arrival:
                    usbArrivalTimer.Change(usbDelay, Timeout.Infinite);
                    break;
                default:
                    break;
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
                    view.IsPortConnected = true;
                    return;
                }
            }
        }

        private async void TextBoxFwFile_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
                string logMsg = await view.WriteFwContent.LoadFwFile(view.FwFile);
                if (view.WriteFwContent.FwAvailable)
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
            await RunTask(GetChipInfoSubTask);
        }

        private async Task<bool> GetChipInfoSubTask()
        {
            view.ChipSettingView.ChipInfo = string.Empty;
            string? chipInfo = await GetChipInfo(view.ChipSettingView.SelectedChip, view.ChipSettingView.SelectedChipAddr);
            if (chipInfo == null)
            {
                return false;
            }
            else
            {
                view.ChipSettingView.ChipInfo = chipInfo;
                return true;
            }
        }

        private void ButtonClearLog_Click(object sender, RoutedEventArgs e)
        {
            log.ClearLogBox();
        }

        private async void ButtonAutodetectChip_Click(object sender, RoutedEventArgs e)
        {
            await RunTask(AutodetectChipSubTask);
        }

        private async Task<bool> AutodetectChipSubTask()
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
                        return true;
                    }
                }
            }
            return false;
        }

        private async void ButtonFormatEsprogStorage_Click(object sender, RoutedEventArgs e)
        {
            await RunTask(FormatEsprogStorageSubTask);
        }

        private async Task<bool> FormatEsprogStorageSubTask()
        {

            if (!await nuprog.FormatEsprog())
            {
                log.Error("Format ESPROG storage fail");
                return false;
            }
            log.Info("Format ESPROG storage succeed");
            return true;
        }

        private async void ButtonProgChip_Click(object sender, RoutedEventArgs e)
        {
            await RunTask(ProgChipSubTask);
        }

        private async Task<bool> ProgChipSubTask()
        {
            string logMsg = await view.WriteFwContent.LoadFwFile(view.FwFile);
            if (!view.WriteFwContent.FwAvailable)
            {
                log.Error(logMsg);
                return false;
            }
            log.Info(logMsg);
            if (!await SendFwToESPROG())
            {
                return false;
            }
            if (!await nuprog.FwWriteStart())
            {
                log.Error("Program chip fail");
                return false;
            }
            log.Error("Program chip succeed");
            return true;
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
            if (!await nuprog.WriteFwToEsprog(view.WriteFwContent.FwData, view.WriteFwContent.Size))
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
            await RunTask(ProgEsprogSubTask);
        }

        private async Task<bool> ProgEsprogSubTask()
        {
            string logMsg = await view.WriteFwContent.LoadFwFile(view.FwFile);
            if (!view.WriteFwContent.FwAvailable)
            {
                log.Error(logMsg);
                return false;
            }
            log.Info(logMsg);
            if (!await SendFwToESPROG())
            {
                return false;
            }
            if (!await nuprog.SaveToEsprog())
            {
                log.Error("Save config to ESPROG fail");
                return false;
            }
            log.Info("Save config to ESPROG succeed");
            return true;
        }

        private async void ButtonReadChip_Click(object sender, RoutedEventArgs e)
        {
            await RunTask(ReadChipSubTask);
        }

        private async Task<bool> ReadChipSubTask()
        {
            if (!await nuprog.SetChipAndAddr(view.ChipSettingView.SelectedChip, view.ChipSettingView.SelectedChipAddr))
            {
                return false;
            }
            if (!await nuprog.FwReadStart())
            {
                log.Error("Read firmware from chip fail");
                return false;
            }
            bool res = await nuprog.ReadFwFromEsprog(view.ReadFwContent.FwData);
            view.ReadFwContent.FwAvailable = res;
            view.ReadFwContent.Size = ChipSettingVM.MaxFwSize;
            view.ReadFwContent.UpdateDisplay();
            return res;
        }

        private async void ButtonSaveFw_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new()
            {
                Title = "Save firmware file",
                Filter = "Binary File (*.bin)|*.bin"
            };
            if (dialog.ShowDialog() == true)
            {
                (bool res, string logMsg) = await view.ReadFwContent.SaveFwData(dialog.FileName);
                if (res)
                {
                    log.Info(logMsg);
                }
                else
                {
                    log.Error(logMsg);
                }
            }
        }

        private async void ButtonReadReg_Click(object sender, RoutedEventArgs e)
        {
            await RunTask(ReadRegSubTask);
        }

        private async Task<bool> ReadRegSubTask()
        {
            if (view.RegAddr == null)
            {
                return false;
            }
            if ((view.RegVal = await nuprog.ReadReg(view.RegAddr.Value)) == null)
            {
                log.Error(string.Format("Read reg ({0}) from dev ({1}) fail",
                    view.RegAddr, HexUtil.GetHexStr(view.ChipSettingView.SelectedChipAddr)));
                return false;
            }
            return true;
        }

        private async void ButtonWriteReg_Click(object sender, RoutedEventArgs e)
        {
            await RunTask(WriteRegSubTask);
        }

        private async Task<bool> WriteRegSubTask()
        {
            if (view.RegAddr == null || view.RegVal == null)
            {
                return false;
            }
            if (!await nuprog.WriteReg(view.RegAddr.Value, view.RegVal.Value))
            {
                log.Error(string.Format("Read reg ({0}) from dev ({1}) fail",
                    view.RegAddr, HexUtil.GetHexStr(view.ChipSettingView.SelectedChipAddr)));
                return false;
            }
            return true;
        }

        private void ButtonSendCmd_Click(object sender, RoutedEventArgs e)
        {
            string cmd = view.SendCmd.Trim() + "\r\n";
            uart.SendCmd(cmd);
        }

        private delegate Task<bool> SubTaskHandler();

        private async Task RunTask(SubTaskHandler subTask)
        {
            view.IsIdle = false;
            view.ProgressView.SetSate(ProgressVM.State.Running);
            if (await subTask())
            {
                view.ProgressView.SetSate(ProgressVM.State.Succeed);
            }
            else
            {
                view.ProgressView.SetSate(ProgressVM.State.Fail);
            }
            view.IsIdle = true;
        }
    }
}
