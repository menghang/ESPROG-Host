using ESPROGConsole.Models;
using ESPROGConsole.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ESPROGConsole.Services
{
    internal class UartService
    {
        private const int shortCheckTimeout = 500;
        private const int midCheckTimeout = 750;
        private const int longCheckTimeout = 1500;
        private const int bufSize = 8 * 1024;

        private SerialPort? port = null;
        private string readBuffer = string.Empty;

        public bool Open(string portName)
        {
            port = new()
            {
                PortName = portName,
                BaudRate = 1000000,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
                Handshake = Handshake.None,
                ReadBufferSize = bufSize,
                WriteBufferSize = bufSize,
                Encoding = Encoding.ASCII,
                ReadTimeout = 100,
                WriteTimeout = 100,
                ReceivedBytesThreshold = 4
            };

            readBuffer = string.Empty;
            try
            {
                port.Open();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Open serial port fail");
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public void Close()
        {
            port?.Close();
            port?.Dispose();
        }

        private UartCmdModel? SendCmdSingleRsp(UartCmdModel cmd, int timeout)
        {
            readBuffer = string.Empty;
            if (!SendCmd(cmd))
            {
                return null;
            }

            DateTime start = DateTime.Now;
            while (true)
            {
                if ((DateTime.Now - start).TotalMilliseconds > timeout)
                {
                    return null;
                }
                Queue<UartCmdModel>? cmds = ReadCmd();
                if (cmds == null)
                {
                    Thread.Sleep(10);
                    continue;
                }
                while (cmds.Count > 0)
                {
                    UartCmdModel recvCmd = cmds.Dequeue();
                    if (recvCmd.Cmd != cmd.Cmd)
                    {
                        continue;
                    }
                    if (recvCmd.Rsp == null || recvCmd.Rsp != 0)
                    {
                        return null;
                    }
                    return recvCmd;
                }
            }
        }

        public UartCmdModel? SendCmdFastRsp(UartCmdModel cmd)
        {
            return SendCmdSingleRsp(cmd, shortCheckTimeout);
        }

        public UartCmdModel? SendCmdSlowRsp(UartCmdModel cmd)
        {
            return SendCmdSingleRsp(cmd, midCheckTimeout);
        }

        public UartCmdModel? SendCmdMultiRsp(UartCmdModel cmd, byte successVal)
        {
            int timeout = longCheckTimeout;
            readBuffer = string.Empty;
            if (!SendCmd(cmd))
            {
                return null;
            }

            Console.Write("*");
            DateTime start = DateTime.Now;
            while (true)
            {
                if ((DateTime.Now - start).TotalMilliseconds > timeout)
                {
                    return null;
                }
                Queue<UartCmdModel>? cmds = ReadCmd();
                if (cmds == null)
                {
                    Thread.Sleep(100);
                    continue;
                }
                while (cmds.Count > 0)
                {
                    UartCmdModel recvCmd = cmds.Dequeue();
                    if (recvCmd.Cmd != cmd.Cmd)
                    {
                        continue;
                    }
                    if (recvCmd.Rsp == null || recvCmd.Rsp != 0 || recvCmd.ValCount != 1)
                    {
                        return null;
                    }
                    if (HexUtil.GetU8FromStr(recvCmd.Val[0]) != successVal) // heart beat cmd
                    {
                        Console.Write("*");
                        start = DateTime.Now;
                        continue;
                    }
                    return recvCmd;
                }
            }
        }

        private bool SendCmd(UartCmdModel cmd)
        {
            try
            {
                if (port == null)
                {
                    return false;
                }
                port.ReadExisting();
                Trace.WriteLine("[s]" + cmd.ToString());
                port.Write(cmd.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return false;
            }
        }

        private Queue<UartCmdModel>? ReadCmd()
        {
            if (port == null)
            {
                return null;
            }
            readBuffer += port.ReadExisting();

            Queue<UartCmdModel>? cmds = null;
            MatchCollection mc = Regex.Matches(readBuffer, @"\[([a-zA-Z]+[0-9]*,[0-9]+(,[a-zA-Z0-9-:=\s\.\+\/]+)*|Error)]\r\n");
            if (mc.Count > 0)
            {
                int matchIndex = 0;
                int matchLength = 0;
                foreach (Match m in mc.Cast<Match>())
                {
                    Trace.WriteLine("[R]" + m.Value);
                    UartCmdModel? cmd = UartCmdModel.ParseRecv(m.Value);
                    if (cmd != null)
                    {
                        cmds ??= new();
                        cmds.Enqueue(cmd);
                        matchIndex = m.Index;
                        matchLength = m.Length;
                    }
                }
                readBuffer = readBuffer[(matchIndex + matchLength)..];
            }
            if (readBuffer.Length > bufSize)
            {
                readBuffer = string.Empty;
            }
            return cmds;
        }
    }
}
