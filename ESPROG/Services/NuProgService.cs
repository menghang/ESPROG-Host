using ESPROG.Models;
using ESPROG.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ESPROG.Services
{
    class NuProgService
    {
        private readonly LogService log;
        private readonly UartService uart;
        private readonly Queue<UartCmdModel> cmdQueue;

        private static readonly int shortCheckInterval = 10;
        private static readonly int shortCheckTimeout = 200;
        private static readonly int longCheckInterval = 100;
        private static readonly int longCheckTimeout = 1200;

        private static readonly uint esprogFwBlockSize = 512;

        public NuProgService(LogService logService, UartService uartService)
        {
            log = logService;
            uart = uartService;
            cmdQueue = new Queue<UartCmdModel>();
            uart.CmdReceived += Uart_CmdReceived;
        }

        private void Uart_CmdReceived(object sender, UartService.UartCmdReceivedEventArgs e)
        {
            while (e.Cmds.Count > 0)
            {
                cmdQueue.Enqueue(e.Cmds.Dequeue());
            }
        }

        private async Task<UartCmdModel?> SendCmdAsync(UartCmdModel cmd)
        {
            int interval = shortCheckInterval;
            int timeout = shortCheckTimeout;
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
                if (recvCmd.Rsp == null || recvCmd.Rsp != 0) // wrong rsp
                {
                    return null;
                }
                return recvCmd;
            }
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
                if (recvCmd.Rsp == null || recvCmd.Rsp != 0 || recvCmd.Val.Count != 1) // wrong rsp or wrong val size
                {
                    return null;
                }
                if (HexUtil.GetByteFromStr(recvCmd.Val[0]) != successVal) // heart beat cmd
                {
                    start = DateTime.Now;
                    continue;
                }
                return recvCmd;
            }
        }

        public async Task<string?> GetEsprogVersionAsync()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetAppVersion);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            if (RecvCmd == null || RecvCmd.Val.Count != 2)
            {
                return null;
            }
            return string.Format("{0}({1})", RecvCmd.Val[0], RecvCmd.Val[1]);
        }

        public async Task<string?> GetEsprogCompileTimeAsync()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetAppCompileTime);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            if (RecvCmd == null || RecvCmd.Val.Count != 2)
            {
                return null;
            }
            return string.Format("{0}, {1}", RecvCmd.Val[0], RecvCmd.Val[1]);
        }

        public async Task<byte?> ReadReg(byte regAddr)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdReadReg);
            sendCmd.AddVal(regAddr);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            if (RecvCmd == null || RecvCmd.Val.Count != 1)
            {
                return null;
            }
            return HexUtil.GetByteFromStr(RecvCmd.Val[0]);
        }

        public async Task<bool> WriteReg(byte regAddr, byte regVal)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetDevAddr);
            sendCmd.AddVal(regAddr).AddVal(regVal);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            return RecvCmd != null;
        }

        public async Task<(byte pn, byte version)?> GetChipInfo()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetChipInfo);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            if (RecvCmd == null || RecvCmd.Val.Count != 2)
            {
                return null;
            }
            byte? pn = HexUtil.GetByteFromStr(RecvCmd.Val[0]);
            byte? version = HexUtil.GetByteFromStr(RecvCmd.Val[1]);
            return pn == null || version == null ? null : ((byte pn, byte version)?)(pn, version);
        }

        public async Task<uint?> GetChipUID()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetChipUID);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            if (RecvCmd == null || RecvCmd.Val.Count != 1)
            {
                return null;
            }
            return HexUtil.GetUIntFromStr(RecvCmd.Val[0]);

        }

        public async Task<bool> SetDevAddr(byte devAddr)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetDevAddr);
            sendCmd.AddVal(devAddr);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            return RecvCmd != null;
        }

        public async Task<byte?> GetDevAddr()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetDevAddr);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            if (RecvCmd == null || RecvCmd.Val.Count != 1)
            {
                return null;
            }
            return HexUtil.GetByteFromStr(RecvCmd.Val[0]);
        }

        public async Task<bool> SetChip(uint chip)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetChip);
            sendCmd.AddVal(chip);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            return RecvCmd != null;
        }

        public async Task<uint?> GetChip()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetChip);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            if (RecvCmd == null || RecvCmd.Val.Count != 1)
            {
                return null;
            }
            return HexUtil.GetUIntFromStr(RecvCmd.Val[0]);
        }

        public async Task<bool> FwWriteBuf(uint fwAddr, byte[] fwData)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetChip);
            sendCmd.AddVal(fwAddr).AddVal(HexUtil.GetChecksum(fwData)).AddVal(fwData);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            if (RecvCmd == null || RecvCmd.Val.Count != 1)
            {
                return false;
            }
            return HexUtil.GetUIntFromStr(RecvCmd.Val[0]) == fwAddr;
        }

        public async Task<byte[]?> FwReadBuf(uint fwAddr)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetChip);
            sendCmd.AddVal(fwAddr);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd);
            if (RecvCmd == null || RecvCmd.Val.Count != 3)
            {
                return null;
            }
            uint? fwAddrRead = HexUtil.GetUIntFromStr(RecvCmd.Val[0]);
            uint? checksum = HexUtil.GetUIntFromStr(RecvCmd.Val[1]);
            byte[]? fwData = HexUtil.GetBytesFromBase64Str(RecvCmd.Val[2]);
            if (fwAddrRead != fwAddr || checksum == null || fwData == null)
            {
                return null;
            }
            uint calcChecksum = HexUtil.GetChecksum(fwData);
            return calcChecksum != checksum ? null : fwData;
        }

        public async Task<bool> FwWriteStart()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwWriteStart);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdMultiRspAsync(sendCmd, byte.MaxValue);
            return RecvCmd != null;
        }

        public async Task<bool> FwReadStart()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwReadStart);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdMultiRspAsync(sendCmd, byte.MaxValue);
            return RecvCmd != null;
        }

        public async Task<bool> SaveToEsprog()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSaveConfig);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdMultiRspAsync(sendCmd, byte.MaxValue);
            return RecvCmd != null;
        }

        public async Task<bool> FormatEsprog()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFlashFormat);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdMultiRspAsync(sendCmd, byte.MaxValue);
            return RecvCmd != null;
        }

        public async Task<bool> WriteFwToEsprog(byte[] fwData, long fwSize)
        {
            for (uint fwAddr = 0; fwAddr < fwSize; fwAddr += esprogFwBlockSize)
            {
                byte[] fwBuffer = new byte[esprogFwBlockSize];
                if (fwAddr + esprogFwBlockSize < fwData.LongLength)
                {
                    Array.Copy(fwData, fwAddr, fwBuffer, 0, esprogFwBlockSize);
                }
                else if (fwAddr < fwData.LongLength)
                {
                    Array.Copy(fwData, fwAddr, fwBuffer, 0, fwData.LongLength - fwAddr);
                }
                if (!await FwWriteBuf(fwAddr, fwBuffer))
                {
                    log.Error(string.Format("Write firmware to ESPROG fail at addr ({0})", fwAddr));
                    return false;
                }
            }
            return true;
        }

        public async Task<byte[]?> ReadFwFromEsprog(long fwSize)
        {
            uint fwAddr = 0;
            byte[] fwData = new byte[fwSize];
            while (true)
            {
                byte[]? fwBuffer = await FwReadBuf(fwAddr);
                if (fwBuffer == null)
                {
                    log.Error(string.Format("Read firmware from ESPROG fail at addr ({0})", fwAddr));
                    return null;
                }
                if (fwAddr + fwBuffer.LongLength < fwSize)
                {
                    Array.Copy(fwBuffer, 0, fwData, fwAddr, fwBuffer.LongLength);
                    continue;
                }
                else if (fwAddr + fwBuffer.LongLength == fwSize)
                {
                    Array.Copy(fwBuffer, 0, fwData, fwAddr, fwBuffer.LongLength);
                    return fwData;
                }
                else
                {
                    log.Error(string.Format("Buffer size ({0}) exceed firmware size ({1}) at addr ({2})",
                        fwBuffer.LongLength, fwSize, fwAddr));
                    return null;
                }
            }
        }
    }
}
