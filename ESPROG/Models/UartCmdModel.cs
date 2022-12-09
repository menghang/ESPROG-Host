using ESPROG.Utils;
using System.Collections.Generic;
using System.Text;

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

        public string Cmd { private set; get; }

        public byte? Rsp { private set; get; }

        public string[] Val { private set; get; }

        public int ValCount { private set; get; }

        public UartCmdModel(string cmd)
        {
            Cmd = cmd;
            Rsp = null;
            Val = new string[3];
            ValCount = 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append('[').Append(Cmd);
            if (Rsp.HasValue)
            {
                sb.Append(',').Append(HexUtil.GetHexStr(Rsp.Value));
            }
            for (int ii = 0; ii < ValCount; ii++)
            {
                sb.Append(',').Append(Val[ii]);
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
            if (items.Length == 0 || items.Length > 5)
            {
                return null;
            }
            UartCmdModel cmd = new(items[0]);
            if (items.Length == 1)
            {
                return cmd;
            }
            cmd.Rsp = HexUtil.GetByteFromStr(items[1]);
            if (cmd.Rsp == null)
            {
                return null;
            }
            if (items.Length == 2)
            {
                return cmd;
            }
            cmd.ValCount = items.Length - 2;
            for (int ii = 2; ii < items.Length; ii++)
            {
                cmd.Val[ii - 2] = items[ii];
            }
            return cmd;
        }

        public UartCmdModel AddVal(byte val)
        {
            Val[ValCount] = HexUtil.GetHexStr(val);
            ValCount++;
            return this;
        }

        public UartCmdModel AddVal(uint val)
        {
            Val[ValCount] = HexUtil.GetHexStr(val);
            ValCount++;
            return this;
        }

        public UartCmdModel AddVal(bool val)
        {
            Val[ValCount] = val ? "1" : "0";
            ValCount++;
            return this;
        }

        public UartCmdModel AddVal(byte[] val)
        {
            Val[ValCount] = HexUtil.GetBase64Str(val);
            ValCount++;
            return this;
        }

        public UartCmdModel AddVal(string val)
        {
            Val[ValCount] = val;
            ValCount++;
            return this;
        }
    }
}
