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
        private readonly ManualResetEvent dataRecvEvent;

        public UartService(LogService logControl)
        {
            log = logControl;
            port = null;
            readBuffer = string.Empty;
            dataRecvEvent = new(false);
        }

        public List<string> Scan()
        {
            List<string> ports = new();
            try
            {
                using (ManagementObjectSearcher searcher = new("select * from Win32_PnPEntity where Name like '%(COM%)'"))
                {
                    foreach (ManagementObject hwInfo in searcher.Get())
                    {
                        string? fullName = hwInfo.GetPropertyValue("Name").ToString();
                        if (fullName == null)
                        {
                            continue;
                        }
                        ManagementBaseObject result = hwInfo.InvokeMethod("GetDeviceProperties",
                            hwInfo.GetMethodParameters("GetDeviceProperties"), new InvokeMethodOptions());

                        ManagementBaseObject[]? deviceProperties = result.GetPropertyValue("deviceProperties") as ManagementBaseObject[];
                        if (deviceProperties == null)
                        {
                            continue;
                        }

                        foreach (ManagementBaseObject deviceProperty in deviceProperties)
                        {
                            if (deviceProperty?.GetPropertyValue("KeyName").ToString() == "DEVPKEY_Device_BusReportedDeviceDesc")
                            {
                                string? deviceDesc = deviceProperty.GetPropertyValue("Data").ToString();
                                ports.Add(string.Format("[{0}] {1}", deviceDesc, fullName));
                                break;
                            }
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
            port.DataReceived += Port_DataReceived;
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

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            dataRecvEvent.Set();
        }

        public void SendCmd(UartCmdModel cmd)
        {
            dataRecvEvent.Reset();
            SendCmd(cmd.ToString());
        }

        public async Task<Queue<UartCmdModel>?> ReadCmd(int timeout)
        {
            if (port == null)
            {
                return null;
            }

            bool isTimeout = false;
            Task readTask = Task.Run(() =>
            {
                dataRecvEvent.WaitOne();
            });
            Task timeoutTask = Task.Run(async () =>
            {
                await Task.Delay(timeout);
                isTimeout = true;
            });
            await Task.WhenAny(readTask, timeoutTask);

            if (isTimeout)
            {
                return null;
            }
            dataRecvEvent.Reset();
            readBuffer += port.ReadExisting();

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

    }
}
