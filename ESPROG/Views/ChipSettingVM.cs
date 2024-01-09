using ESPROG.Models;
using ESPROG.Services;
using System;
using System.Collections.Generic;

namespace ESPROG.Views
{
    class ChipSettingVM : BaseViewModel
    {
        public List<ComboBoxModel<string, ushort>> ChipList { get; private set; }

        private ushort selectedChip;
        public ushort SelectedChip
        {
            get => selectedChip;
            set
            {
                SetProperty(ref selectedChip, value);
                ChipAddrList = NuProgService.ChipDict[selectedChip].Addrs;
                SelectedChipAddr = chipAddrList[1].Value;
                SelectedChipChanged?.Invoke(this, new ChipChangedEventArgs(value));
            }
        }

        public delegate void SelectedChipChangedHandler(object sender, ChipChangedEventArgs e);
        public event SelectedChipChangedHandler? SelectedChipChanged;
        public class ChipChangedEventArgs : EventArgs
        {
            public ushort Chip { get; private set; }

            public ChipChangedEventArgs(ushort chip)
            {
                Chip = chip;
            }
        }

        private List<ComboBoxModel<string, byte>> chipAddrList;
        public List<ComboBoxModel<string, byte>> ChipAddrList
        {
            get => chipAddrList;
            set
            {
                if (value != chipAddrList)
                {
                    chipAddrList = value;
                    OnPropertyChanged(nameof(ChipAddrList));
                    OnPropertyChanged(nameof(SelectedChipAddr));
                }
            }
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
            foreach (ushort chip in NuProgService.ChipDict.Keys)
            {
                ChipList.Add(NuProgService.ChipDict[chip].Name);
            }
            selectedChip = 0x1708;
            chipAddrList = NuProgService.ChipDict[selectedChip].Addrs;
            selectedChipAddr = ChipAddrList[1].Value;
            chipInfo = string.Empty;
            isPortConnected = false;
        }
    }
}
