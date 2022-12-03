using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESPROG.Views
{
    internal class FwContentVM : BaseViewModel
    {
        private string content;
        public string Content
        {
            get => this.content;
        }

        private uint checksum;
        public string Checksum
        {
            get => "0x" + Convert.ToString(this.checksum, 16).PadLeft(8, '0');
        }

        private int size;
        public string Size
        {
            get => Convert.ToString(this.size);
        }

        private byte[]? fwData;
        public string FwData
        {
            get => string.Empty;
        }

        private const int MaxFwLength = 32 * 1024;

        public bool LoadFwFile(string file)
        {
            bool res = false;
            try
            {
                if (File.Exists(file))
                {
                    using (FileStream fs = new(file, FileMode.Open))
                    {
                        if (fs.Length <= MaxFwLength)
                        {
                            this.size = (int)fs.Length;
                            this.fwData = new byte[this.size];
                            fs.Read(fwData, 0, this.size);

                            res = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            if (res == false)
            {
                this.size = 0;
                this.checksum = 0;
                this.content = string.Empty;
            }
            OnPropertyChanged(nameof(this.Size));
            OnPropertyChanged(nameof(this.Checksum));
            OnPropertyChanged(nameof(this.Content));
            return res;
        }

        public FwContentVM()
        {
            this.content = string.Empty;
            this.checksum = 0;
            this.size = 0;
        }
    }
}
