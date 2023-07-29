using ESPROG.Models;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ESPROG.Services
{
    class UartService
    {
        private readonly LogService log;
        private SerialPort? port;
        private string readBuffer;
        private const int bufSize = 8 * 1024;
        private readonly byte[] buf;

        public UartService(LogService logControl)
        {
            log = logControl;
            port = null;
            readBuffer = string.Empty;
            buf = new byte[bufSize];
        }

        public List<string> Scan()
        {
            List<string> ports = new();
            try
            {
                using (ManagementObjectSearcher searcher = new("select * from Win32_PnPEntity where Name like '%(COM%)'"))
                {
                    foreach (ManagementObject hwInfo in searcher.Get().Cast<ManagementObject>())
                    {
                        string? fullName = hwInfo.Properties["Name"].Value.ToString();
                        if (fullName != null)
                        {
                            ports.Add(fullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Debug(ex.ToString());
            }
            return ports;
        }

        public bool Open(string portName)
        {
            Match m = Regex.Match(portName, @"\(COM[0-9]+\)");
            if (!m.Success)
            {
                log.Error(string.Format("Wrong port name ({0})", portName));
            }
            port?.Close();
            port = new()
            {
                PortName = m.Value[1..^1],
                BaudRate = 2000000,
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
                log.Debug(ex.ToString());
                port.Close();
            }
            return false;
        }

        public void SendCmd(UartCmdModel cmd)
        {
            SendCmd(cmd.ToString());
        }

        public async Task<Queue<UartCmdModel>?> ReadCmd(int timeout)
        {
            if (port == null)
            {
                return null;
            }
            while (true)
            {
                using (CancellationTokenSource cts = new())
                {
                    Task<int> readTask = port.BaseStream.ReadAsync(buf, 0, buf.Length, cts.Token);
                    if (await Task.WhenAny(readTask, Task.Delay(timeout)) == readTask)
                    {
                        readBuffer += Encoding.ASCII.GetString(buf, 0, readTask.Result);
                        if (readTask.Result < buf.Length)
                        {
                            break;
                        }
                    }
                    else
                    {
                        cts.Cancel();
                        return null;
                    }
                }
            }

            Queue<UartCmdModel>? cmds = null;
            MatchCollection mc = Regex.Matches(readBuffer, @"\[([a-zA-Z]+[0-9]*,[0-9]+(,[a-zA-Z0-9-:=\s\.\+\/]+)*|Error)]\r\n");
            if (mc.Count > 0)
            {
                int matchIndex = 0;
                int matchLength = 0;
                foreach (Match m in mc.Cast<Match>())
                {
                    LogCmd(false, m.Value);
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

        public void SendCmd(string cmd)
        {
            try
            {
                if (port == null)
                {
                    return;
                }
                port.ReadExisting(); // Clear buffer before write
                port.Write(cmd);
                LogCmd(true, cmd);
            }
            catch (Exception ex)
            {
                log.Debug(ex.ToString());
            }
        }

        public void Close()
        {
            port?.Close();
            port?.Dispose();
        }

        private void LogCmd(bool send, string line)
        {
            string fullLog = string.Format("[{0}] {1}", send ? "S" : "R", line.TrimEnd());
            log.Debug(fullLog);
        }

        public class UartCmdReceivedEventArgs : EventArgs
        {
            public Queue<UartCmdModel> Cmds { private set; get; }

            public UartCmdReceivedEventArgs(Queue<UartCmdModel> cmds)
            {
                Cmds = cmds;
            }
        }

    }
}
