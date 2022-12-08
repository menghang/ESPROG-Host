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

        public static readonly Dictionary<uint, long> ChipSizeDict = new()
        {
            { 0x1705, 32 * 1024 }, { 0x1708, 32 * 1024 }, { 0x1718, 64 * 1024 }
        };

        public List<ComboBoxModel<string, uint>> ChipList { get; private set; }

        public delegate void SelectedChipChangedHandler(object sender, EventArgs e);
        public event SelectedChipChangedHandler? SelectedChipChanged;

        private uint selectedChip;
        public uint SelectedChip
        {
            get => selectedChip;
            set
            {
                if (selectedChip != value)
                {
                    SetProperty(ref selectedChip, value);
                    SetProperty(ref chipAddrList, ChipDict[selectedChip], nameof(ChipAddrList));
                    SetProperty(ref selectedChipAddr, chipAddrList[1].Value, nameof(SelectedChipAddr));
                    SelectedChipChanged?.Invoke(this, EventArgs.Empty);
                }
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
            set => SetProperty(ref this.enableAutoChipAddr, value);
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
            selectedChip = ChipList[1].Value;
            chipAddrList = ChipDict[selectedChip];
            selectedChipAddr = ChipAddrList[1].Value;
            chipInfo = string.Empty;
            isPortConnected = false;
        }
    }
}
