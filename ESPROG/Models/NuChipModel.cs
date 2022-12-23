using ESPROG.Utils;
using System;
using System.Collections.Generic;

namespace ESPROG.Models
{
    internal class NuChipModel
    {
        public ComboBoxModel<string, uint> Name { get; private set; }

        public List<ComboBoxModel<string, byte>> Addrs { get; private set; }

        public RomModel MTP { get; private set; }

        public RomModel Trim { get; private set; }

        public RomModel Config { get; private set; }

        public class RomModel
        {
            public uint Offset { get; private set; }

            public uint Size { get; private set; }

            public RomModel(uint offset, uint size)
            {
                Offset = offset;
                Size = size;
            }
        }

        public NuChipModel(uint chip, List<byte> addrs, RomModel mtp, RomModel trim, RomModel config)
        {
            Name = new("NU" + Convert.ToString(chip, 16).PadLeft(4, '0'), chip);
            Addrs = new();
            foreach (byte addr in addrs)
            {
                Addrs.Add(new(HexUtil.GetHexStr(addr), addr));
            }
            MTP = mtp;
            Trim = trim;
            Config = config;
        }
    }
}
