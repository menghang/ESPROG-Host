namespace ESPROGConsole.Models
{
    internal class NuChipModel
    {
        public ushort Chip { get; private set; }

        public byte[] Addr { get; private set; }

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

        public NuChipModel(ushort chip, byte[] addr, RomModel mtp, RomModel trim, RomModel config)
        {
            Chip = chip;
            Addr = addr;
            MTP = mtp;
            Trim = trim;
            Config = config;
        }
    }
}
