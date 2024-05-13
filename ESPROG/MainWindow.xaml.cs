using ESPROG.Models;
using ESPROG.Services;
using ESPROG.Utils;
using ESPROG.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
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
        private readonly string version;
        private const int usbDelay = 100;

        public MainWindow()
        {
            InitializeComponent();
            view = new();
            DataContext = view;

            log = new(TextBoxLog);
            uart = new(log);
            nuprog = new(log, uart, view.ChipSettingView.SelectedChip);
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

            string? v = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            version = string.IsNullOrEmpty(v) ? "Unknown Version" : v;
            view.Title = "ESPROG - " + version;
            view.ChipSettingView.SelectedChipChanged += ChipSettingView_SelectedChipChanged;
            view.ChipSettingView.SelectedChip = 0x1708;

            ConfigModel config = new();
            if (config.LoadConfig())
            {
                view.LoadConfig(config);
            }
        }

        private void ChipSettingView_SelectedChipChanged(object sender, ChipSettingVM.ChipChangedEventArgs e)
        {
            nuprog.SetChipModel(e.Chip);
            view.WriteFwContent.MaxSize = NuProgService.ChipDict[e.Chip].MTP.Size;
            view.ReadFwContent.MaxSize = NuProgService.ChipDict[e.Chip].MTP.Size;
            view.WriteConfigContent.MaxSize = NuProgService.ChipDict[e.Chip].Config.Size;
            view.ReadConfigContent.MaxSize = NuProgService.ChipDict[e.Chip].Config.Size;
            view.WriteTrimContent.MaxSize = NuProgService.ChipDict[e.Chip].Trim.Size;
            view.ReadTrimContent.MaxSize = NuProgService.ChipDict[e.Chip].Trim.Size;
            view.WriteFwContent.FwAddrOffset = NuProgService.ChipDict[e.Chip].MTP.Offset;
            view.ReadFwContent.FwAddrOffset = NuProgService.ChipDict[e.Chip].MTP.Offset;
            view.WriteConfigContent.FwAddrOffset = NuProgService.ChipDict[e.Chip].Config.Offset;
            view.ReadConfigContent.FwAddrOffset = NuProgService.ChipDict[e.Chip].Config.Offset;
            view.WriteTrimContent.FwAddrOffset = NuProgService.ChipDict[e.Chip].Trim.Offset;
            view.ReadTrimContent.FwAddrOffset = NuProgService.ChipDict[e.Chip].Trim.Offset;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadSerialPort();
            view.EsprogSettingView.SelectedVddModeChanged += EsprogSelView_SelectedVddModeChanged;
            view.EsprogSettingView.SelectedIoModeChanged += EsprogSettingView_SelectedIoModeChanged;
        }

        private async void EsprogSettingView_SelectedIoModeChanged(object sender, EsprogSettingVM.IoModeEventArgs e)
        {
            view.IsIdle = false;
            view.ProgressView.SetSate(ProgressVM.State.Running);
            if (!await nuprog.SetIoVol(e.NewVol))
            {
                log.Error(string.Format("Set io mode ({0}) fail", view.EsprogSettingView.SelectedIoVol));
                view.EsprogSettingView.UpdateSelectedIoVol(e.LastVol);
                view.ProgressView.SetSate(ProgressVM.State.Fail);
            }
            else
            {
                log.Info(string.Format("Set vdd mode ({0}) succeed", view.EsprogSettingView.SelectedIoVol));
                view.ProgressView.SetSate(ProgressVM.State.Succeed);
            }
            view.IsIdle = true;
        }

        private async void EsprogSelView_SelectedVddModeChanged(object sender, EsprogSettingVM.VddModeEventArgs e)
        {
            view.IsIdle = false;
            view.ProgressView.SetSate(ProgressVM.State.Running);
            if (!await nuprog.SetVddCtrl(e.NewCtrlMode, e.NewVol))
            {
                log.Error(string.Format("Set vdd mode ({0} {1}) fail",
                    view.EsprogSettingView.SelectedVddCtrlMode, view.EsprogSettingView.SelectedVddVol));
                view.EsprogSettingView.UpdateSelectedVddCtrlMode(e.LastCtrlMode);
                view.EsprogSettingView.UpdateSelectedVddVol(e.LastVol);
                view.ProgressView.SetSate(ProgressVM.State.Fail);
            }
            else
            {
                log.Info(string.Format("Set vdd mode ({0} {1}) succeed",
                    view.EsprogSettingView.SelectedVddCtrlMode, view.EsprogSettingView.SelectedVddVol));
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
            (byte CtrlMode, byte Vol)? VddMode = await nuprog.GetVddCtrl();
            if (VddMode == null)
            {
                log.Debug(string.Format("Can not get ESPROG vdd ctrl mode on port({0})", port));
                return false;
            }
            byte? IoVol = await nuprog.GetIoVol();
            if (IoVol == null)
            {
                log.Debug(string.Format("Can not get ESPROG io vol on port({0})", port));
                return false;
            }
            view.EsprogSettingView.EsprogInfo = version;
            view.EsprogSettingView.EsprogCompileTime = compileTime;
            view.EsprogSettingView.UpdateSelectedVddCtrlMode(VddMode.Value.CtrlMode);
            view.EsprogSettingView.UpdateSelectedVddVol(VddMode.Value.Vol);
            view.EsprogSettingView.UpdateSelectedIoVol(IoVol.Value);
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
                if (!port.Contains("[ESPROG COMM]"))
                {
                    continue;
                }
                if (await TryConnectPort(port))
                {
                    view.EsprogSettingView.SelectedPort = port;
                    view.IsPortConnected = true;
                    return;
                }
                uart.Close();
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
                    log.Info("MTP " + logMsg);
                }
                else
                {
                    log.Error("MTP " + logMsg);
                }
            }
        }

        private async Task<string?> GetChipInfo(ushort chip, byte devAddr)
        {
            if (!await nuprog.SetChipAndAddr(chip, devAddr))
            {
                return null;
            }
            (byte chipPn, byte chipVersion, uint chipUID)? chipInfo = await nuprog.GetChipInfo();
            if (chipInfo == null)
            {
                log.Error(string.Format("Get chip info fail"));
                return null;
            }
            return string.Format("PN:{0}, VER:{1}, UID:{2}", HexUtil.GetHexStr(chipInfo.Value.chipPn),
                HexUtil.GetHexStr(chipInfo.Value.chipVersion), HexUtil.GetHexStr(chipInfo.Value.chipUID));
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
            foreach (ushort chip in NuProgService.ChipDict.Keys)
            {
                foreach (ComboBoxModel<string, byte> devAddr in NuProgService.ChipDict[chip].Addrs)
                {
                    if (await nuprog.DetectChip(devAddr.Value))
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
            if (!await CheckFwFile())
            {
                return false;
            }
            if (!await SendAllFwToESPROG())
            {
                return false;
            }
            if (!await nuprog.FwWriteStart())
            {
                log.Error("Program chip fail");
                return false;
            }
            log.Info("Program chip succeed");
            return true;
        }

        private async Task<bool> SendAllFwToESPROG()
        {
            if (!await nuprog.SetChipAndAddr(view.ChipSettingView.SelectedChip, view.ChipSettingView.SelectedChipAddr))
            {
                return false;
            }
            if (!await nuprog.SetProgZone(view.SelectedWriteZone))
            {
                return false;
            }
            if ((view.SelectedWriteZone & NuProgService.MtpZoneMask) == NuProgService.MtpZoneMask)
            {
                if (view.WriteFwContent.FwData == null)
                {
                    log.Error("Firmware is not available");
                    return false;
                }
                if (!await SendFwToESPROG(NuProgService.MtpZoneMask, view.WriteFwContent.FwData, view.WriteFwContent.Size))
                {
                    return false;
                }
            }
            if ((view.SelectedWriteZone & NuProgService.CfgZoneMask) == NuProgService.CfgZoneMask)
            {
                if (view.WriteConfigContent.FwData == null)
                {
                    log.Error("Config is not available");
                    return false;
                }
                if (!await SendFwToESPROG(NuProgService.CfgZoneMask, view.WriteConfigContent.FwData, view.WriteConfigContent.Size))
                {
                    return false;
                }
            }
            if ((view.SelectedWriteZone & NuProgService.TrimZoneMask) == NuProgService.TrimZoneMask)
            {
                if (view.WriteTrimContent.FwData == null)
                {
                    log.Error("Trim is not available");
                    return false;
                }
                if (!await SendFwToESPROG(NuProgService.TrimZoneMask, view.WriteTrimContent.FwData, view.WriteTrimContent.Size))
                {
                    return false;
                }
            }
            return true;
        }

        private async Task<bool> SendFwToESPROG(byte zone, byte[] data, uint size)
        {
            if (!await nuprog.WriteFwToEsprog(zone, data, size))
            {
                return false;
            }
            if (!await nuprog.FwWriteChecksum(zone, data, size))
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
            if (!await CheckFwFile())
            {
                return false;
            }
            if (!await SendAllFwToESPROG())
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

        private async Task<bool> CheckFwFile()
        {
            string logMsgFw = await view.WriteFwContent.LoadFwFile(view.FwFile);
            string logMsgCfg = await view.WriteConfigContent.LoadFwFile(view.ConfigFile);
            string logMsgTrim = await view.WriteTrimContent.LoadFwFile(view.TrimFile);

            if ((view.SelectedWriteZone & NuProgService.MtpZoneMask) == NuProgService.MtpZoneMask)
            {
                if (!view.WriteFwContent.FwAvailable)
                {
                    log.Error("MTP " + logMsgFw);
                    return false;
                }
                else
                {
                    log.Info("MTP " + logMsgFw);
                }
            }
            if ((view.SelectedWriteZone & NuProgService.CfgZoneMask) == NuProgService.CfgZoneMask)
            {
                if (!view.WriteConfigContent.FwAvailable)
                {
                    log.Error("Config " + logMsgCfg);
                    return false;
                }
                else
                {
                    log.Info("Config " + logMsgCfg);
                }
            }
            if ((view.SelectedWriteZone & NuProgService.TrimZoneMask) == NuProgService.TrimZoneMask)
            {
                if (!view.WriteTrimContent.FwAvailable)
                {
                    log.Error("Trim " + logMsgTrim);
                    return false;
                }
                else
                {
                    log.Info("Trim " + logMsgTrim);
                }
            }
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
            if (!await nuprog.SetProgZone(NuProgService.MtpZoneMask | NuProgService.CfgZoneMask | NuProgService.TrimZoneMask))
            {
                return false;
            }
            if (!await nuprog.FwReadStart())
            {
                log.Error("Read firmware from chip fail");
                return false;
            }

            view.ReadFwContent.FwAvailable =
                await nuprog.ReadFwFromEsprog(NuProgService.MtpZoneMask, view.ReadFwContent.FwData);
            view.ReadFwContent.Size = NuProgService.ChipDict[view.ChipSettingView.SelectedChip].MTP.Size;
            view.ReadFwContent.UpdateDisplay();
            if (!view.ReadFwContent.FwAvailable)
            {
                return false;
            }

            view.ReadConfigContent.FwAvailable =
                await nuprog.ReadFwFromEsprog(NuProgService.CfgZoneMask, view.ReadConfigContent.FwData);
            view.ReadConfigContent.Size = NuProgService.ChipDict[view.ChipSettingView.SelectedChip].Config.Size;
            view.ReadConfigContent.UpdateDisplay();
            if (!view.ReadConfigContent.FwAvailable)
            {
                return false;
            }

            view.ReadTrimContent.FwAvailable =
                await nuprog.ReadFwFromEsprog(NuProgService.TrimZoneMask, view.ReadTrimContent.FwData);
            view.ReadTrimContent.Size = NuProgService.ChipDict[view.ChipSettingView.SelectedChip].Trim.Size;
            view.ReadTrimContent.UpdateDisplay();
            if (!view.ReadTrimContent.FwAvailable)
            {
                return false;
            }
            return true;
        }

        private async void ButtonSaveFw_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new()
            {
                Title = "Save data file",
                Filter = "Binary File (*.bin)|*.bin"
            };
            if (dialog.ShowDialog() == true)
            {
                (bool res, string logMsg) rsp;
                switch (view.SelectedReadZone)
                {
                    case NuProgService.MtpZoneMask:
                        rsp = await view.ReadFwContent.SaveFwData(dialog.FileName);
                        if (rsp.res)
                        {
                            log.Info("MTP " + rsp.logMsg);
                        }
                        else
                        {
                            log.Error("MTP " + rsp.logMsg);
                        }
                        break;
                    case NuProgService.CfgZoneMask:
                        rsp = await view.ReadConfigContent.SaveFwData(dialog.FileName);
                        if (rsp.res)
                        {
                            log.Info("Config " + rsp.logMsg);
                        }
                        else
                        {
                            log.Error("Config " + rsp.logMsg);
                        }
                        break;
                    case NuProgService.TrimZoneMask:
                        rsp = await view.ReadTrimContent.SaveFwData(dialog.FileName);
                        if (rsp.res)
                        {
                            log.Info("Trim " + rsp.logMsg);
                        }
                        else
                        {
                            log.Error("Trim " + rsp.logMsg);
                        }
                        break;
                    default:
                        log.Error(string.Format("Wrong zone value ({0})", view.SelectedReadZone));
                        break;
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

        private async void ButtonReadRegAddr16_Click(object sender, RoutedEventArgs e)
        {
            await RunTask(ReadRegAddr16SubTask);
        }

        private async Task<bool> ReadRegAddr16SubTask()
        {
            if (view.RegAddr16 == null)
            {
                return false;
            }
            if ((view.RegVal = await nuprog.ReadRegAddr16(view.RegAddr16.Value)) == null)
            {
                log.Error(string.Format("Read reg ({0}) from dev ({1}) fail",
                    view.RegAddr16, HexUtil.GetHexStr(view.ChipSettingView.SelectedChipAddr)));
                return false;
            }
            return true;
        }

        private async void ButtonWriteRegAddr16_Click(object sender, RoutedEventArgs e)
        {
            await RunTask(WriteRegAddr16SubTask);
        }

        private async Task<bool> WriteRegAddr16SubTask()
        {
            if (view.RegAddr16 == null || view.RegVal == null)
            {
                return false;
            }
            if (!await nuprog.WriteRegAddr16(view.RegAddr16.Value, view.RegVal.Value))
            {
                log.Error(string.Format("Read reg ({0}) from dev ({1}) fail",
                    view.RegAddr16, HexUtil.GetHexStr(view.ChipSettingView.SelectedChipAddr)));
                return false;
            }
            return true;
        }

        private void ButtonSendCmd_Click(object sender, RoutedEventArgs e)
        {
            string cmd = view.SendCmd.Trim() + "\r\n";
            uart.SendCmd(cmd);
        }

        private async void TextBoxConfigFile_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Multiselect = false,
                Title = "Open config file",
                Filter = "Binary File (*.bin)|*.bin|All Files|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                view.ConfigFile = dialog.FileName;
                string logMsg = await view.WriteConfigContent.LoadFwFile(view.ConfigFile);
                if (view.WriteConfigContent.FwAvailable)
                {
                    log.Info("Config" + logMsg);
                }
                else
                {
                    log.Error("Config" + logMsg);
                }
            }
        }

        private async void TextBoxTrimFile_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Multiselect = false,
                Title = "Open trim file",
                Filter = "Binary File (*.bin)|*.bin|All Files|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                view.TrimFile = dialog.FileName;
                string logMsg = await view.WriteTrimContent.LoadFwFile(view.TrimFile);
                if (view.WriteTrimContent.FwAvailable)
                {
                    log.Info("Trim" + logMsg);
                }
                else
                {
                    log.Error("Trim" + logMsg);
                }
            }
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow dialog = new();
            dialog.SetVersion(version);
            dialog.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            view.ExportConfig().SaveConfig();
        }

        private const int writeBufferSize = 256 * 4;

        private async void ButtonGenBinFile_Click(object sender, RoutedEventArgs e)
        {
            if (view.BinaryGeneratorView.BinSize == 0 || view.BinaryGeneratorView.BinSize % 4 != 0)
            {
                log.Error("Wrong binary size");
                return;
            }

            SaveFileDialog dialog = new()
            {
                Title = "Save binary file",
                Filter = "Binary File (*.bin)|*.bin"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using FileStream fs = new(dialog.FileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                    using BufferedStream bs = new(fs);
                    byte[] buf;
                    if (view.BinaryGeneratorView.DataPattern > uint.MaxValue)
                    {
                        buf = RandomNumberGenerator.GetBytes(writeBufferSize);
                    }
                    else
                    {
                        buf = new byte[writeBufferSize];
                        uint pattern = (uint)view.BinaryGeneratorView.DataPattern;
                        for (int ii = 0; ii < buf.Length; ii += 4)
                        {
                            buf[ii] = (byte)pattern;
                            buf[ii + 1] = (byte)(pattern >> 8);
                            buf[ii + 2] = (byte)(pattern >> 16);
                            buf[ii + 3] = (byte)(pattern >> 24);
                        }
                    }
                    long pos = 0;
                    long size = view.BinaryGeneratorView.BinSize;
                    while (pos < view.BinaryGeneratorView.BinSize)
                    {
                        long bufLength = size - pos < writeBufferSize ? size - pos : writeBufferSize;
                        await bs.WriteAsync(buf.AsMemory(0, (int)bufLength));
                        pos += bufLength;
                    }
                    bs.Flush();
                    log.Info("Save binary file succeed");
                }
                catch (Exception ex)
                {
                    log.Error("Save binary file fail" + Environment.NewLine + ex.ToString());
                }
            }
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
