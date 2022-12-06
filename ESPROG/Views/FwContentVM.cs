using ESPROG.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ESPROG.Views
{
    internal class FwContentVM : BaseViewModel
    {
        public long MaxFwSize { get; set; }

        public string Content { get; private set; }

        private uint checksum;
        public string Checksum
        {
            get => HexUtil.GetHexStr(checksum);
        }

        private long size;
        public string Size
        {
            get => Convert.ToString(size);
        }

        public byte[]? FwData { get; private set; }

        private string GetFwContent(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }
            StringBuilder sb = new();
            for (long line = 0; line < data.LongLength; line += 16)
            {
                sb.Append(HexUtil.GetHexStr(line)).Append(' ');
                for (int ii = 0; (ii < 16) && (line + ii < data.Length); ii++)
                {
                    sb.Append(' ').Append(Convert.ToString(data[line + ii], 16).PadLeft(2, '0'));
                }
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        private static readonly int readBufferSize = 1024;
        public bool LoadFwFile(string file, out string log)
        {
            bool res = false;
            try
            {
                if (File.Exists(file))
                {
                    using (FileStream fs = new(file, FileMode.Open))
                    {
                        using (BufferedStream bs = new(fs))
                        {
                            if (bs.Length <= MaxFwSize)
                            {
                                FwData = new byte[bs.Length];
                                byte[] buf = new byte[readBufferSize];
                                int readBytes = 0;
                                long pos = 0;
                                while ((readBytes = bs.Read(buf, 0, readBufferSize)) > 0)
                                {
                                    Array.Copy(buf, 0, FwData, pos, readBytes);
                                    pos += readBytes;
                                }
                                Content = GetFwContent(FwData);
                                checksum = HexUtil.GetChecksum(FwData);
                                size = FwData.LongLength;
                                log = "Firmware file load succeed";
                                res = true;
                            }
                            else
                            {
                                log = string.Format("Firmware size ({0}) exceed limit ({1})", bs.Length, MaxFwSize);
                            }
                        }
                    }
                }
                else
                {
                    log = "Firmware file do not exist";
                }
            }
            catch (Exception ex)
            {
                log = "Firmware file load fail" + Environment.NewLine + ex.ToString();
            }
            if (res == false)
            {
                size = 0;
                checksum = 0;
                Content = string.Empty;
            }
            OnPropertyChanged(nameof(Size));
            OnPropertyChanged(nameof(Checksum));
            OnPropertyChanged(nameof(Content));
            return res;
        }

        public FwContentVM()
        {
            Content = string.Empty;
            checksum = 0;
            size = 0;
        }
    }
}
