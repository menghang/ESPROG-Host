using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using ESPROG.Models;
using Microsoft.Win32;

namespace ESPROG.Utils
{
    class UartUtil
    {
        private readonly TextBox ui;
        private readonly LogUtil log;
        private SerialPort? port;
        private string readBuffer;
        private List<UartCmdModel> cmds;

        public UartUtil(LogUtil logControl, TextBox textbox)
        {
            log = logControl;
            ui = textbox;
            port = null;
            readBuffer = string.Empty;
            cmds = new();
        }

        public string[] Scan()
        {
            return SerialPort.GetPortNames();
        }

        public void Open(string portName)
        {
            port = new()
            {
                PortName = portName,
                BaudRate = 2000000,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
                ReadBufferSize = 1024 * 1024,
                WriteBufferSize = 1024 * 1024,
                Encoding = Encoding.ASCII
            };
            port.DataReceived += Port_DataReceived;
            try
            {
                port.Open();
            }
            catch (Exception ex)
            {
                log.Debug(ex.ToString());
                port.Close();
                port.Open();
            }
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port == null)
            {
                return;
            }
            readBuffer += port.ReadExisting();
            Regex regex = new(@"\[[a-zA-z],[a-zA-Z0-9,]+\]\r\n");
            regex.Matches(readBuffer);
        }

        public void Close()
        {

        }

        private void LogCmd()
        {

        }

    }
}
