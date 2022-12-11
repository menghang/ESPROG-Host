using ESPROG.Utils;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ESPROG.Views
{
    internal class FwContentVM : BaseViewModel
    {
        public string ContentText { get; private set; }

        private uint checksum;
        public uint Checksum
        {
            get => checksum;
        }
        public string ChecksumText
        {
            get => HexUtil.GetHexStr(checksum);
        }

        private long size;
        public long Size
        {
            get => size;
            set => size = value <= FwData.LongLength ? value : FwData.LongLength;
        }
        public string SizeText
        {
            get => Convert.ToString(size);
        }

        public byte[] FwData { get; private set; }

        public bool FwAvailable { get; set; }

        public void UpdateDisplay()
        {
            if (FwAvailable)
            {
                StringBuilder sb = new();
                for (long line = 0; line < size; line += 16)
                {
                    sb.Append(HexUtil.GetHexStr(line)).Append(' ');
                    for (int ii = 0; (ii < 16) && (line + ii < size); ii++)
                    {
                        sb.Append(' ').Append(Convert.ToString(FwData[line + ii], 16).PadLeft(2, '0'));
                    }
                    sb.Append(Environment.NewLine);
                }
                ContentText = sb.ToString();
                checksum = HexUtil.GetChecksum(FwData);
            }
            else
            {
                ContentText = string.Empty;
                size = 0;
                checksum = 0;
            }
            OnPropertyChanged(nameof(SizeText));
            OnPropertyChanged(nameof(ChecksumText));
            OnPropertyChanged(nameof(ContentText));
        }

        private static readonly int readBufferSize = 1024;
        public async Task<string> LoadFwFile(string file)
        {
            string log;
            FwAvailable = false;
            try
            {
                if (File.Exists(file))
                {
                    using (FileStream fs = new(file, FileMode.Open))
                    {
                        using (BufferedStream bs = new(fs))
                        {
                            if (bs.Length > 0 && bs.Length <= ChipSettingVM.MaxFwSize && bs.Length <= FwData.LongLength)
                            {
                                size = bs.Length;
                                Array.Clear(FwData);
                                byte[] buf = new byte[readBufferSize];
                                int readBytes = 0;
                                long pos = 0;
                                while ((readBytes = await bs.ReadAsync(buf, 0, readBufferSize)) > 0)
                                {
                                    Array.Copy(buf, 0, FwData, pos, readBytes);
                                    pos += readBytes;
                                }
                                log = "Firmware file load succeed";
                                FwAvailable = true;
                            }
                            else
                            {
                                log = string.Format("Firmware size ({0}) does not fit limit ({1})",
                                    bs.Length, ChipSettingVM.MaxFwSize);
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
            UpdateDisplay();
            return log;
        }

        private static readonly int writeBufferSize = 1024;
        public async Task<(bool res, string log)> SaveFwData(string file)
        {
            bool res = false;
            string log;
            try
            {
                if (FwAvailable)
                {
                    using (FileStream fs = new(file, FileMode.Create))
                    {
                        using (BufferedStream bs = new(fs))
                        {
                            byte[] buf = new byte[writeBufferSize];
                            long pos = 0;
                            while (pos < size)
                            {
                                long bufLength = size - pos < writeBufferSize ? size - pos : writeBufferSize;
                                Array.Copy(FwData, pos, buf, 0, bufLength);
                                await bs.WriteAsync(buf, 0, (int)bufLength);
                                pos += bufLength;
                            }
                            bs.Flush();
                            log = "Save firmware succeed";
                            res = true;
                        }
                    }
                }
                else
                {
                    log = "Firmware data does not exist";
                }
            }
            catch (Exception ex)
            {
                log = "Save firmware fail" + Environment.NewLine + ex.ToString();
            }
            return (res, log);
        }

        public FwContentVM()
        {
            ContentText = string.Empty;
            checksum = 0;
            size = 0;
            FwAvailable = false;
            FwData = new byte[128 * 1024];
        }
    }
}
