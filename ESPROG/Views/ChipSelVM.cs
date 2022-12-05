using System.Collections.Generic;

namespace ESPROG.Views
{
    class ChipSelVM : BaseViewModel
    {
        private static readonly Dictionary<string, List<string>> chips = new()
        {
            { "NU1705",new(){"0x50","0x51","0x52","0x53"} },
            { "NU1708",new(){"0x50","0x51","0x52","0x53"} },
            { "NU1718",new(){"0x70","0x71","0x72","0x73"} }
        };

        public List<string> ChipList { get; private set; }

        private string selectedChip;
        public string SelectedChip
        {
            get => selectedChip;
            set
            {
                SetProperty(ref selectedChip, value);
                SetProperty(ref chipAddrList, chips[selectedChip], nameof(ChipAddrList));
                SetProperty(ref selectedChipAddr, chipAddrList[1], nameof(SelectedChipAddr));
            }
        }

        private List<string> chipAddrList;
        public List<string> ChipAddrList
        {
            get => chipAddrList;
            set => SetProperty(ref chipAddrList, value);
        }

        private string selectedChipAddr;
        public string SelectedChipAddr
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

        private bool portConnected;
        public bool PortConnected
        {
            get => portConnected;
            set => SetProperty(ref portConnected, value);
        }

        public ChipSelVM()
        {
            ChipList = new();
            foreach (string chip in chips.Keys)
            {
                ChipList.Add(chip);
            }
            selectedChip = ChipList[1];
            chipAddrList = chips[selectedChip];
            selectedChipAddr = ChipAddrList[1];
            chipInfo = string.Empty;
            portConnected = false;
        }
    }
}
