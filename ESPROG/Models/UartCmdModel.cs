using ESPROG.Utils;
using System.Text;

namespace ESPROG.Models
{
    class UartCmdModel
    {
        public const string CmdReadReg = "ReadReg";
        public const string CmdWriteReg = "WriteReg";
        public const string CmdSetDevAddr = "SetDevAddr";
        public const string CmdGetDevAddr = "GetDevAddr";
        public const string CmdSetGateCtrl = "SetGateCtrl";
        public const string CmdGetGateCtrl = "GetGateCtrl";
        public const string CmdFwWriteBuf = "FwWriteBuf";
        public const string CmdFwReadBuf = "FwReadBuf";
        public const string CmdFwWriteStart = "FwWriteStart";
        public const string CmdFwReadStart = "FwReadStart";
        public const string CmdSaveConfig = "SaveConfig";
        public const string CmdFlashFormat = "FlashFormat";
        public const string CmdGetAppVersion = "GetInfo";
        public const string CmdGetChipInfo = "GetChipInfo";
        public const string CmdGetChipUID = "GetChipUID";
        public const string CmdGetAppCompileTime = "GetAppCompileTime";
        public const string CmdFwWriteChecksum = "FwWriteChecksum";
        public const string CmdFwReadChecksum = "FwReadChecksum";
        public const string CmdSetChip = "SetChip";
        public const string CmdGetChip = "GetChip";
        public const string CmdSetZone = "SetZone";
        public const string CmdGetZone = "GetZone";

        public const string CmdError = "Error";

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
            cmd.Rsp = HexUtil.GetU8FromStr(items[1]);
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
