using ESPROG.Models;
using ESPROG.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESPROG.Services
{
    class NuProgService
    {
        public static readonly Dictionary<uint, NuChipModel> ChipDict = new()
        {
            { 0x1708, new(0x1708, new() { 0x50, 0x51, 0x52, 0x53 }, new(0x00000000, 32 * 1024), new(0x00008000, 1 * 512), new(0x00008200, 3 * 512)) },
            { 0x1718, new(0x1718, new() { 0x70, 0x71, 0x72, 0x73 }, new(0x00000000, 64 * 1024), new(0x00010000, 1 * 128), new(0x00010080, 1 * 128)) }
        };

        private readonly LogService log;
        private readonly UartService uart;
        private readonly Queue<UartCmdModel> cmdQueue;

        private const int shortCheckInterval = 20;
        private const int shortCheckTimeout = 500;
        private const int midCheckInterval = 50;
        private const int midCheckTimeout = 1000;
        private const int longCheckInterval = 100;
        private const int longCheckTimeout = 2000;

        public const byte MtpZoneMask = 0x01;
        public const byte CfgZoneMask = 0x02;
        public const byte TrimZoneMask = 0x04;

        private uint chip;

        public NuProgService(LogService logService, UartService uartService, uint chip)
        {
            log = logService;
            uart = uartService;
            cmdQueue = new Queue<UartCmdModel>();
            uart.CmdReceived += Uart_CmdReceived;
            this.chip = chip;
        }

        public void SetChipModel(uint chip)
        {
            this.chip = chip;
        }

        private void Uart_CmdReceived(object sender, UartService.UartCmdReceivedEventArgs e)
        {
            while (e.Cmds.Count > 0)
            {
                cmdQueue.Enqueue(e.Cmds.Dequeue());
            }
        }

        private async Task<UartCmdModel?> SendCmdAsync(UartCmdModel cmd, int interval, int timeout)
        {
            cmdQueue.Clear();
            uart.SendCmd(cmd);
            DateTime start = DateTime.Now;
            await Task.Delay(interval);
            while (true)
            {
                if ((DateTime.Now - start).TotalMilliseconds > timeout) // Timeout
                {
                    return null;
                }
                if (cmdQueue.Count == 0) // No available cmd
                {
                    await Task.Delay(interval);
                    continue;
                }
                UartCmdModel recvCmd = cmdQueue.Dequeue();
                if (recvCmd.Cmd != cmd.Cmd) // Not the right cmd
                {
                    continue;
                }
                if (recvCmd.Rsp == null || recvCmd.Rsp != 0) // wrong rsp
                {
                    return null;
                }
                return recvCmd;
            }
        }

        private async Task<UartCmdModel?> SendCmdFastRspAsync(UartCmdModel cmd)
        {
            return await SendCmdAsync(cmd, shortCheckInterval, shortCheckTimeout);
        }

        private async Task<UartCmdModel?> SendCmdSlowRspAsync(UartCmdModel cmd)
        {
            return await SendCmdAsync(cmd, midCheckInterval, midCheckTimeout);
        }

        private async Task<UartCmdModel?> SendCmdMultiRspAsync(UartCmdModel cmd, byte successVal)
        {
            int interval = longCheckInterval;
            int timeout = longCheckTimeout;
            cmdQueue.Clear();
            uart.SendCmd(cmd);
            DateTime start = DateTime.Now;
            while (true)
            {
                if ((DateTime.Now - start).TotalMilliseconds > timeout) // Timeout
                {
                    return null;
                }
                if (cmdQueue.Count == 0) // No available cmd
                {
                    await Task.Delay(interval);
                    continue;
                }
                UartCmdModel recvCmd = cmdQueue.Dequeue();
                if (recvCmd.Cmd != cmd.Cmd) // Not the right cmd
                {
                    continue;
                }
                if (recvCmd.Rsp == null || recvCmd.Rsp != 0 || recvCmd.ValCount != 1) // wrong rsp or wrong val size
                {
                    return null;
                }
                if (HexUtil.GetU8FromStr(recvCmd.Val[0]) != successVal) // heart beat cmd
                {
                    start = DateTime.Now;
                    continue;
                }
                return recvCmd;
            }
        }

        public async Task<string?> GetEsprogInfoAsync()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetAppVersion);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 3)
            {
                return null;
            }
            return string.Format("{0}({1}/{2})", recvCmd.Val[0], recvCmd.Val[1], recvCmd.Val[2]);
        }

        public async Task<string?> GetEsprogCompileTimeAsync()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetAppCompileTime);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 2)
            {
                return null;
            }
            return string.Format("{0}, {1}", recvCmd.Val[0], recvCmd.Val[1]);
        }

        public async Task<byte?> ReadReg(byte regAddr)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdReadReg);
            sendCmd.AddVal(regAddr);
            UartCmdModel? recvCmd = await SendCmdSlowRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 1)
            {
                return null;
            }
            return HexUtil.GetU8FromStr(recvCmd.Val[0]);
        }

        public async Task<bool> WriteReg(byte regAddr, byte regVal)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdWriteReg);
            sendCmd.AddVal(regAddr).AddVal(regVal);
            UartCmdModel? recvCmd = await SendCmdSlowRspAsync(sendCmd);
            return recvCmd != null;
        }

        public async Task<(byte pn, byte version, uint uid)?> GetChipInfo()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetChipInfo);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdSlowRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 3)
            {
                return null;
            }
            byte? pn = HexUtil.GetU8FromStr(recvCmd.Val[0]);
            byte? version = HexUtil.GetU8FromStr(recvCmd.Val[1]);
            uint? uid = HexUtil.GetU32FromStr(recvCmd.Val[2]);
            if (pn == null || version == null || uid == null)
            {
                return null;
            }
            else
            {
                return (pn.Value, version.Value, uid.Value);
            }
        }

        public async Task<bool> SetDevAddr(byte devAddr)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetDevAddr);
            sendCmd.AddVal(devAddr);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            return recvCmd != null;
        }

        public async Task<byte?> GetDevAddr()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetDevAddr);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 1)
            {
                return null;
            }
            return HexUtil.GetU8FromStr(recvCmd.Val[0]);
        }

        public async Task<bool> SetChip(uint chip)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetChip);
            sendCmd.AddVal(chip);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            return recvCmd != null;
        }

        public async Task<uint?> GetChip()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetChip);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 1)
            {
                return null;
            }
            return HexUtil.GetU32FromStr(recvCmd.Val[0]);
        }

        public async Task<bool> DetectChip(byte devAddr)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdDetectChip);
            sendCmd.AddVal(devAddr);
            UartCmdModel? recvCmd = await SendCmdSlowRspAsync(sendCmd);
            return recvCmd != null;
        }

        public async Task<bool> FwWriteBuf(uint fwAddr, byte[] fwData)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwWriteBuf);
            sendCmd.AddVal(fwAddr).AddVal(HexUtil.GetChecksum(fwData, fwData.LongLength)).AddVal(fwData);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 1)
            {
                return false;
            }
            return HexUtil.GetU32FromStr(recvCmd.Val[0]) == fwAddr;
        }

        public async Task<byte[]?> FwReadBuf(uint fwAddr)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwReadBuf);
            sendCmd.AddVal(fwAddr);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 3)
            {
                return null;
            }
            uint? fwAddrRead = HexUtil.GetU32FromStr(recvCmd.Val[0]);
            uint? checksum = HexUtil.GetU32FromStr(recvCmd.Val[1]);
            byte[]? fwData = HexUtil.GetBytesFromBase64Str(recvCmd.Val[2]);
            if (fwAddrRead != fwAddr || checksum == null || fwData == null)
            {
                return null;
            }
            uint calcChecksum = HexUtil.GetChecksum(fwData, fwData.LongLength);
            return calcChecksum != checksum ? null : fwData;
        }

        public async Task<bool> FwWriteStart()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwWriteStart);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdMultiRspAsync(sendCmd, byte.MaxValue);
            return recvCmd != null;
        }

        public async Task<bool> FwReadStart()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwReadStart);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdMultiRspAsync(sendCmd, byte.MaxValue);
            return recvCmd != null;
        }

        public async Task<bool> SaveToEsprog()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSaveConfig);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdMultiRspAsync(sendCmd, byte.MaxValue);
            return recvCmd != null;
        }

        public async Task<bool> FormatEsprog()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFlashFormat);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdMultiRspAsync(sendCmd, byte.MaxValue);
            return recvCmd != null;
        }

        public async Task<bool> WriteFwToEsprog(byte zone, byte[] fwData, uint fwSize)
        {
            uint fwAddrOffset;
            uint maxSize;
            string zoneName;
            switch (zone)
            {
                case MtpZoneMask:
                    fwAddrOffset = ChipDict[chip].MTP.Offset;
                    maxSize = ChipDict[chip].MTP.Size;
                    zoneName = "firmware";
                    break;
                case CfgZoneMask:
                    fwAddrOffset = ChipDict[chip].Config.Offset;
                    maxSize = ChipDict[chip].Config.Size;
                    zoneName = "config";
                    break;
                case TrimZoneMask:
                    fwAddrOffset = ChipDict[chip].Trim.Offset;
                    maxSize = ChipDict[chip].Trim.Size;
                    zoneName = "trim";
                    break;
                default:
                    log.Error(string.Format("Wrong zone value ({0})", zone));
                    return false;
            }
            uint esprogFwBlockSize = maxSize >= 512 ? 512 : maxSize;
            byte[] fwBuffer = new byte[esprogFwBlockSize];
            for (uint fwAddr = 0; fwAddr < maxSize; fwAddr += esprogFwBlockSize)
            {
                if (fwAddr + esprogFwBlockSize < fwSize)
                {
                    Array.Copy(fwData, fwAddr, fwBuffer, 0, esprogFwBlockSize);
                }
                else if (fwAddr < fwSize)
                {
                    Array.Clear(fwBuffer);
                    Array.Copy(fwData, fwAddr, fwBuffer, 0, fwSize - fwAddr);
                }
                else
                {
                    Array.Clear(fwBuffer);
                }
                if (!await FwWriteBuf(fwAddr + fwAddrOffset, fwBuffer))
                {
                    log.Error(string.Format("Write {0} zone to ESPROG fail at addr ({1})", zoneName, fwAddr));
                    return false;
                }
            }
            log.Info(string.Format("Write {0} zone to ESPROG succeed", zoneName));
            return true;
        }

        public async Task<bool> ReadFwFromEsprog(byte zone, byte[] fwData)
        {
            Array.Clear(fwData);
            uint fwAddrOffset;
            uint maxSize;
            string zoneName;
            switch (zone)
            {
                case MtpZoneMask:
                    fwAddrOffset = ChipDict[chip].MTP.Offset;
                    maxSize = ChipDict[chip].MTP.Size;
                    zoneName = "firmware";
                    break;
                case CfgZoneMask:
                    fwAddrOffset = ChipDict[chip].Config.Offset;
                    maxSize = ChipDict[chip].Config.Size;
                    zoneName = "config";
                    break;
                case TrimZoneMask:
                    fwAddrOffset = ChipDict[chip].Trim.Offset;
                    maxSize = ChipDict[chip].Trim.Size;
                    zoneName = "trim";
                    break;
                default:
                    log.Error(string.Format("Wrong zone value ({0})", zone));
                    return false;
            }
            uint fwAddr = 0;
            while (true)
            {
                byte[]? fwBuffer = await FwReadBuf((uint)(fwAddr + fwAddrOffset));
                if (fwBuffer == null)
                {
                    log.Error(string.Format("Read {0} zone from ESPROG fail at addr ({1})", zoneName, fwAddr));
                    return false;
                }
                if (fwAddr + fwBuffer.LongLength < maxSize)
                {
                    Array.Copy(fwBuffer, 0, fwData, fwAddr, fwBuffer.LongLength);
                    fwAddr += (uint)fwBuffer.LongLength;
                    continue;
                }
                else if (fwAddr + fwBuffer.LongLength == maxSize)
                {
                    Array.Copy(fwBuffer, 0, fwData, fwAddr, fwBuffer.LongLength);
                    log.Info(string.Format("Read {0} zone from ESPROG succeed", zoneName));
                    return true;
                }
                else
                {
                    log.Error(string.Format("Buffer size ({0}) exceed {1} zone size ({2}) at addr ({3})",
                        fwBuffer.LongLength, zoneName, maxSize, fwAddr));
                    return false;
                }
            }
        }

        public async Task<bool> SetGateCtrl(byte mode)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetGateCtrl);
            sendCmd.AddVal(mode);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            return recvCmd != null;
        }

        public async Task<byte?> GetGateCtrl()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetGateCtrl);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 1)
            {
                return null;
            }
            return HexUtil.GetU8FromStr(recvCmd.Val[0]);
        }

        public async Task<bool> SetChipAndAddr(uint chip, byte devAddr)
        {
            if (!await SetChip(chip))
            {
                log.Error(string.Format("Set chip (0x{0}) fail", Convert.ToString(chip, 16)));
                return false;
            }
            if (!await SetDevAddr(devAddr))
            {
                log.Error(string.Format("Set chip addr ({0}) fail", HexUtil.GetHexStr(devAddr)));
                return false;
            }
            return true;
        }

        public async Task<bool> FwWriteChecksum(byte zone, byte[] fwData, uint size)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwWriteChecksum);
            sendCmd.AddVal(zone).AddVal(HexUtil.GetChecksum(fwData, size));
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            return recvCmd != null;
        }

        public async Task<uint?> FwReadChecksum(byte zone)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwReadChecksum);
            sendCmd.AddVal(zone);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 2)
            {
                return null;
            }
            return HexUtil.GetU32FromStr(recvCmd.Val[1]);
        }

        public async Task<bool> SetProgZone(byte zone)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetZone);
            sendCmd.AddVal(zone);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            return recvCmd != null;
        }

        public async Task<byte?> GetProgZone()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetZone);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = await SendCmdFastRspAsync(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 1)
            {
                return null;
            }
            return HexUtil.GetU8FromStr(recvCmd.Val[0]);
        }
    }
}
