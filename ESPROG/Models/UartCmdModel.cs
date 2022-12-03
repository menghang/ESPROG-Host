using ESPROG.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ESPROG.Models
{
    class UartCmdModel
    {
        public static readonly string CmdReadReg = "ReadReg";
        public static readonly string CmdWriteReg = "WriteReg";
        public static readonly string CmdSetDevAddr = "SetDevAddr";
        public static readonly string CmdGetDevAddr = "GetDevAddr";
        public static readonly string CmdSetGateCtrl = "SetGateCtrl";
        public static readonly string CmdGetGateCtrl = "GetGateCtrl";
        public static readonly string CmdFwWriteBuf = "FwWriteBuf";
        public static readonly string CmdFwReadBuf = "FwReadBuf";
        public static readonly string CmdFwWriteStart = "FwWriteStart";
        public static readonly string CmdFwReadStart = "FwReadStart";
        public static readonly string CmdSaveConfig = "SaveConfig";
        public static readonly string CmdFlashFormat = "FlashFormat";
        public static readonly string CmdGetAppVersion = "GetAppVersion";
        public static readonly string CmdGetChipInfo = "GetChipInfo";
        public static readonly string CmdGetChipUID = "GetChipUID";
        public static readonly string CmdGetAppCompileTime = "GetAppCompileTime";
        public static readonly string CmdFwWriteChecksum = "FwWriteChecksum";
        public static readonly string CmdFwReadChecksum = "FwReadChecksum";
        public static readonly string CmdSetChip = "SetChip";
        public static readonly string CmdGetChip = "GetChip";
        public static readonly string CmdError = "Error";

        public string Cmd { private get; set; }

        public byte? Rsp { private get; set; }

        public List<string> Val { private get; set; }

        public UartCmdModel(string cmd)
        {
            Cmd = cmd;
            Rsp = null;
            Val = new();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append('[').Append(Cmd);
            if (Rsp.HasValue)
            {
                sb.Append(',').Append(HexUtil.GetHexStr(Rsp.Value));
            }
            foreach (string s in Val)
            {
                sb.Append(',').Append(s);
            }
            sb.Append("]\r\n");
            return sb.ToString();
        }

        public static UartCmdModel? ParseRecv(string line)
        {
            if (!line.StartsWith('[') || !line.EndsWith("]\r\n"))
            {
                return null;
            }
            line = line[1..^3];
            string[] items = line.Split(',');
            if (items.Length == 0)
            {
                return null;
            }
            UartCmdModel cmd = new(items[0]);
            if (items.Length == 1)
            {
                return cmd;
            }
            cmd.Rsp = HexUtil.GetUint8(items[1]);
            if (cmd.Rsp == null)
            {
                return null;
            }
            if (items.Length == 2)
            {
                return cmd;
            }
            for (int ii = 2; ii < items.Length; ii++)
            {
                cmd.Val.Add(items[ii]);
            }
            return cmd;
        }
    }
}
