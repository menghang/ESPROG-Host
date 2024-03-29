﻿using ESPROG.Utils;
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

        private uint size;
        public uint Size
        {
            get => size;
            set => size = value <= FwData.LongLength ? value : (uint)FwData.LongLength;
        }
        public string SizeText
        {
            get => Convert.ToString(size);
        }

        private string lastModified;
        public string LastModified
        {
            get => lastModified;
        }

        public uint MaxSize { get; set; }

        public byte[] FwData { get; private set; }

        public bool FwAvailable { get; set; }

        public uint FwAddrOffset { get; set; }

        public void UpdateDisplay()
        {
            if (FwAvailable)
            {
                StringBuilder sb = new();
                for (uint line = 0; line < size; line += 16)
                {
                    sb.Append(HexUtil.GetHexStr(line + FwAddrOffset)).Append(' ');
                    for (int ii = 0; (ii < 16) && (line + ii < size); ii++)
                    {
                        sb.Append(' ').Append(Convert.ToString(FwData[line + ii], 16).PadLeft(2, '0'));
                    }
                    sb.Append(Environment.NewLine);
                }
                ContentText = sb.ToString();
                checksum = HexUtil.GetChecksum(FwData, size);
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
            OnPropertyChanged(nameof(LastModified));
        }

        private const int readBufferSize = 8 * 1024;
        public async Task<string> LoadFwFile(string file)
        {
            string log;
            FwAvailable = false;
            try
            {
                if (File.Exists(file))
                {
                    using FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using BufferedStream bs = new(fs);
                    if (bs.Length > MaxSize || bs.Length > FwData.LongLength)
                    {
                        log = string.Format("size ({0}) does not fit limit ({1})", bs.Length, MaxSize);
                    }
                    else if (bs.Length % 4 != 0)
                    {
                        log = string.Format("size ({0}) does not meet requirement", bs.Length);
                    }
                    else
                    {
                        size = (uint)bs.Length;
                        Array.Clear(FwData);
                        byte[] buf = new byte[readBufferSize];
                        int readBytes = 0;
                        long pos = 0;
                        while ((readBytes = await bs.ReadAsync(buf.AsMemory(0, readBufferSize))) > 0)
                        {
                            Array.Copy(buf, 0, FwData, pos, readBytes);
                            pos += readBytes;
                        }
                        log = "file load succeed";
                        FileInfo fi = new(file);
                        lastModified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                        FwAvailable = true;
                    }
                }
                else
                {
                    log = "file do not exist";
                }
            }
            catch (Exception ex)
            {
                log = "file load fail" + Environment.NewLine + ex.ToString();
            }
            UpdateDisplay();
            return log;
        }

        private const int writeBufferSize = 1024;
        public async Task<(bool res, string log)> SaveFwData(string file)
        {
            bool res = false;
            string log;
            try
            {
                if (FwAvailable)
                {
                    using FileStream fs = new(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                    using BufferedStream bs = new(fs);
                    byte[] buf = new byte[writeBufferSize];
                    long pos = 0;
                    while (pos < size)
                    {
                        long bufLength = size - pos < writeBufferSize ? size - pos : writeBufferSize;
                        Array.Copy(FwData, pos, buf, 0, bufLength);
                        await bs.WriteAsync(buf.AsMemory(0, (int)bufLength));
                        pos += bufLength;
                    }
                    bs.Flush();
                    log = "data save succeed";
                    res = true;
                }
                else
                {
                    log = "data does not exist";
                }
            }
            catch (Exception ex)
            {
                log = "data save fail" + Environment.NewLine + ex.ToString();
            }
            return (res, log);
        }

        public FwContentVM(uint fwAddrOffset)
        {
            ContentText = string.Empty;
            checksum = 0;
            size = 0;
            lastModified = string.Empty;
            FwAvailable = false;
            FwData = new byte[128 * 1024];
            FwAddrOffset = fwAddrOffset;
        }
    }
}
