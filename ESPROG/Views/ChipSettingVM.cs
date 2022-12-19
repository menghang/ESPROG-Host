using ESPROG.Models;
using System;
using System.Collections.Generic;

namespace ESPROG.Views
{
    class ChipSettingVM : BaseViewModel
    {
        public static readonly Dictionary<uint, List<ComboBoxModel<string, byte>>> ChipDict = new()
        {
            { 0x1705, new(){ new("0x50", 0x50), new("0x51", 0x51), new("0x52", 0x52), new("0x53", 0x53) } },
            { 0x1708, new(){ new("0x50", 0x50), new("0x51", 0x51), new("0x52", 0x52), new("0x53", 0x53) } },
            { 0x1718, new(){ new("0x70", 0x70), new("0x71", 0x71), new("0x72", 0x72), new("0x73", 0x73) } }
        };

        public static readonly Dictionary<uint, (long mtp, long cfg, long trim)> ChipSizeDict = new()
        {
            { 0x1705, (32 * 1024, 3 * 512, 1 * 512) },
            { 0x1708, (32 * 1024, 3 * 512, 1 * 512) },
            { 0x1718, (64 * 1024, 3 * 512, 1 * 512) }
        };

        public static long MaxFwSize = ChipSizeDict[0x1708].mtp;
        public static long MaxConfigSize = ChipSizeDict[0x1708].cfg;
        public static long MaxTrimSize = ChipSizeDict[0x1708].trim;

        public List<ComboBoxModel<string, uint>> ChipList { get; private set; }

        private uint selectedChip;
        public uint SelectedChip
        {
            get => selectedChip;
            set
            {
                SetProperty(ref selectedChip, value);
                SetProperty(ref chipAddrList, ChipDict[selectedChip], nameof(ChipAddrList));
                SetProperty(ref selectedChipAddr, chipAddrList[1].Value, nameof(SelectedChipAddr));

                MaxFwSize = ChipSizeDict[value].mtp;
                MaxConfigSize = ChipSizeDict[value].cfg;
                MaxTrimSize = ChipSizeDict[value].trim;
                SelectedChipChanged?.Invoke(this, new ChipChangedEventArgs(MaxFwSize, MaxConfigSize, MaxTrimSize));
            }
        }

        public delegate void SelectedChipChangedHandler(object sender, ChipChangedEventArgs e);
        public event SelectedChipChangedHandler? SelectedChipChanged;
        public class ChipChangedEventArgs : EventArgs
        {
            public long FwSize { get; private set; }

            public long CfgSize { get; private set; }

            public long TrimSize { get; private set; }

            public ChipChangedEventArgs(long fw, long cfg, long trim)
            {
                FwSize = fw;
                CfgSize = cfg;
                TrimSize = trim;
            }
        }

        private List<ComboBoxModel<string, byte>> chipAddrList;
        public List<ComboBoxModel<string, byte>> ChipAddrList
        {
            get => chipAddrList;
            set => SetProperty(ref chipAddrList, value);
        }

        private byte selectedChipAddr;
        public byte SelectedChipAddr
        {
            get => selectedChipAddr;
            set => SetProperty(ref selectedChipAddr, value);
        }

        private bool enableAutoChipAddr;
        public bool EnableAutoChipAddr
        {
            get => enableAutoChipAddr;
            set => SetProperty(ref enableAutoChipAddr, value);
        }

        private string chipInfo;
        public string ChipInfo
        {
            get => chipInfo;
            set => SetProperty(ref chipInfo, value);
        }

        private bool isPortConnected;
        public bool IsPortConnected
        {
            get => isPortConnected;
            set => SetProperty(ref isPortConnected, value);
        }

        public ChipSettingVM()
        {
            ChipList = new();
            foreach (uint chip in ChipDict.Keys)
            {
                ChipList.Add(new("NU" + Convert.ToString(chip, 16), chip));
            }
            selectedChip = 0x1708;
            chipAddrList = ChipDict[selectedChip];
            selectedChipAddr = ChipAddrList[1].Value;
            chipInfo = string.Empty;
            isPortConnected = false;
        }
    }
}
