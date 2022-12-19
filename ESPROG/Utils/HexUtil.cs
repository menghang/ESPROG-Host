using System;
using System.Diagnostics;
using System.Globalization;

namespace ESPROG.Utils
{
    class HexUtil
    {
        public static string GetHexStr(long val)
        {
            return "0x" + Convert.ToString(val, 16).PadLeft(8, '0');
        }
        public static string GetHexStr(uint val)
        {
            return "0x" + Convert.ToString(val, 16).PadLeft(8, '0');
        }

        public static string GetHexStr(byte val)
        {
            return "0x" + Convert.ToString(val, 16).PadLeft(2, '0');
        }

        public static uint? GetUIntFromStr(string hex)
        {
            uint val;
            if (hex.StartsWith("0x"))
            {
                return uint.TryParse(hex[2..], NumberStyles.HexNumber, null, out val) ? val : null;
            }
            else
            {
                return uint.TryParse(hex, out val) ? val : null;
            }
        }

        public static byte? GetByteFromStr(string hex)
        {
            byte val;
            if (hex.StartsWith("0x"))
            {
                return byte.TryParse(hex[2..], NumberStyles.HexNumber, null, out val) ? val : null;
            }
            else
            {
                return byte.TryParse(hex, out val) ? val : null;
            }
        }

        public static byte[]? GetBytesFromBase64StrWithInvert(string base64)
        {
            try
            {
                byte[] data = Convert.FromBase64String(base64);
                for (int ii = 0; ii < data.Length; ii++)
                {
                    data[ii] = (byte)~data[ii];
                }
                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return null;
            }
        }

        public static byte[]? GetBytesFromBase64Str(string base64)
        {
            try
            {
                byte[] data = Convert.FromBase64String(base64);
                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return null;
            }
        }

        public static string GetBase64StrWithInvert(byte[] data)
        {
            byte[] data2 = new byte[data.Length];
            for (int ii = 0; ii < data.Length; ii++)
            {
                data2[ii] = (byte)~data[ii];
            }
            return Convert.ToBase64String(data2);
        }

        public static string GetBase64Str(byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        public static uint GetChecksum(byte[] data, long size)
        {
            uint checksum = 0;
            for (int ii = 0; ii < size; ii++)
            {
                checksum += data[ii];
            }
            return checksum;
        }
    }
}
