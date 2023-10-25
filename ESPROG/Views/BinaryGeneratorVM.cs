using ESPROG.Utils;

namespace ESPROG.Views
{
    class BinaryGeneratorVM : BaseViewModel
    {
        private ulong dataPattern = 0;
        public ulong DataPattern
        {
            get => dataPattern;
            set => SetProperty(ref dataPattern, value, nameof(DataPatternText));
        }
        public string DataPatternText
        {
            get
            {
                if (dataPattern > uint.MaxValue)
                {
                    return "Random";
                }
                else
                {
                    return HexUtil.GetHexStr((uint)dataPattern);
                }
            }
            set
            {
                if (value.ToLower().Equals("random"))
                {
                    SetProperty(ref dataPattern, ulong.MaxValue);
                }
                else
                {
                    uint? tmp = HexUtil.GetU32FromStr(value);
                    SetProperty(ref dataPattern, tmp ?? 0);
                }
            }
        }

        private uint binSize = 0;
        public uint BinSize
        {
            get => binSize;
            set => SetProperty(ref binSize, value, nameof(BinSizeText));
        }
        public string BinSizeText
        {
            get => HexUtil.GetHexStr(binSize);
            set
            {
                uint? tmp = HexUtil.GetU32FromStr(value);
                SetProperty(ref binSize, tmp / 4 * 4 ?? 0);
            }
        }
    }
}
