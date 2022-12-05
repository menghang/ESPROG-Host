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

        private static readonly int checkInterval = 10;
        private static readonly int shortCheckTimeout = 200;
        private static readonly int longCheckTimeout = 1200;

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

        private async Task<UartCmdModel?> SendCmdAsync(UartCmdModel cmd, int interval, int timeout)
        {
            cmdQueue.Clear();
            uart.SendCmd(cmd);
            DateTime start = DateTime.Now;
            while (true)
            {
                if ((DateTime.Now - start).TotalMilliseconds > timeout)
                {
                    return null;
                }
                if (cmdQueue.Count == 0)
                {
                    await Task.Delay(interval);
                    continue;
                }
                UartCmdModel recvCmd = cmdQueue.Dequeue();
                if (recvCmd.Cmd != cmd.Cmd)
                {
                    continue;
                }
                if (recvCmd.Rsp == null || recvCmd.Rsp == 1)
                {
                    continue;
                }
                return recvCmd;
            }
        }

        public async Task<string?> GetEsprogVersionAsync()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetAppVersion);
            sendCmd.AddVal(true);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd, checkInterval, shortCheckTimeout);
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
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd, checkInterval, shortCheckTimeout);
            if (RecvCmd == null || RecvCmd.Val.Count != 2)
            {
                return null;
            }
            return string.Format("{0} {1}", RecvCmd.Val[0], RecvCmd.Val[1]);
        }

        public async Task<byte?> ReadReg(byte regAddr)
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdReadReg);
            sendCmd.AddVal(regAddr);
            UartCmdModel? RecvCmd = await SendCmdAsync(sendCmd, checkInterval, shortCheckTimeout);
            if (RecvCmd == null || RecvCmd.Val.Count != 1)
            {
                return null;
            }
            return HexUtil.GetUint8(RecvCmd.Val[0]);
        }
    }
}
