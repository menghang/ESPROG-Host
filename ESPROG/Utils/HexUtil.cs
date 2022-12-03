using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESPROG.Utils
{
    class HexUtil
    {
        public static string GetHexStr(uint val)
        {
            return "0x" + Convert.ToString(val, 16).PadLeft(4, '0');
        }

        public static string GetHexStr(byte val)
        {
            return "0x" + Convert.ToString(val, 16).PadLeft(2, '0');
        }

        public static uint? GetUint32(string hex)
        {
            return uint.TryParse(hex, out uint val) ? val : null;
        }

        public static byte? GetUint8(string hex)
        {
            return byte.TryParse(hex, out byte val) ? val : null;
        }

        public static byte[]? GetBytes(string base64)
        {
            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return null;
            }
        }

        public static string GetBase64Str(byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        public static uint GetChecksum(byte[] data)
        {
            uint checksum = 0;
            for (int ii = 0; ii < data.Length; ii++)
            {
                checksum += data[ii];
            }
            return checksum;
        }
    }
}
