using ESPROGConsole.Models;
using ESPROGConsole.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace ESPROGConsole.Services
{
    internal class NuProgService
    {
        public static readonly Dictionary<ushort, NuChipModel> ChipDict = new()
        {
            { 0x1708, new(0x1708, [ 0x50, 0x51, 0x52, 0x53 ], new(0x00000000, 32 * 1024), new(0x00008000, 1 * 512), new(0x00008200, 3 * 512)) },
            { 0x1718, new(0x1718, [ 0x70, 0x71, 0x72, 0x73 ], new(0x00000000, 64 * 1024), new(0x00010000, 1 * 128), new(0x00010080, 1 * 128)) },
            { 0x1651, new(0x1651, [ 0x60, 0x61, 0x62, 0x63 ], new(0x00000000, 32 * 1024), new(0x00008000, 1 * 512), new(0x00008200, 3 * 512)) },
            { 0x1652, new(0x1652, [ 0x40, 0x41, 0x42, 0x43 ], new(0x00000000, 32 * 1024), new(0x00008000, 1 * 512), new(0x00008200, 3 * 512)) },
            { 0x1628, new(0x1628, [ 0x40, 0x41, 0x42, 0x43 ], new(0x00000000, 32 * 1024), new(0x00008000, 1 * 512), new(0x00008200, 3 * 512)) }
        };

        private readonly string fwFile;
        private readonly ushort chip;
        private readonly byte addr;
        private readonly string port;
        private readonly byte vddCtrlMode;
        private readonly byte vddVol;
        private readonly byte ioVol;
        private readonly byte[] fwData = new byte[256 * 1024];
        private uint fwDataSize = 0;
        private readonly byte fwZone = 0x01;
        private UartService uart;

        public NuProgService(string fwFile, ushort chip, byte addr, string port, byte vddCtrlMode, byte vddVol, byte ioVol)
        {
            this.fwFile = fwFile;
            this.chip = chip;
            this.addr = addr;
            this.port = port;
            this.vddCtrlMode = vddCtrlMode;
            this.vddVol = vddVol;
            this.ioVol = ioVol;
            uart = new();
        }

        public bool Run()
        {
            if (!ReadFw())
            {
                return false;
            }
            uart = new();
            if (!uart.Open(port))
            {
                uart.Close();
                return false;
            }
            bool res = ProgramChip();
            uart.Close();
            return res;
        }

        private bool ReadFw()
        {
            try
            {
                if (!File.Exists(fwFile))
                {
                    Console.WriteLine("Firmware file does not exist");
                }
                using FileStream fs = new(fwFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                using BufferedStream bs = new(fs);
                if (bs.Length % 4 != 0)
                {
                    Console.WriteLine(string.Format("Firmware size ({0}) does not meet requirement", bs.Length));
                    return false;
                }
                if (bs.Length > ChipDict[chip].MTP.Size)
                {
                    Console.WriteLine(string.Format("Firmware size ({0}) does not meet requirement", bs.Length));
                    return false;
                }

                fwDataSize = (uint)bs.Length;
                Array.Clear(fwData);
                const int readBufferSize = 8 * 1024;
                byte[] buf = new byte[readBufferSize];
                int readBytes = 0;
                long pos = 0;
                while ((readBytes = bs.Read(buf, 0, readBufferSize)) > 0)
                {
                    Array.Copy(buf, 0, fwData, pos, readBytes);
                    pos += readBytes;
                }
                Console.WriteLine("Firmware size: " + fwDataSize);
                FileInfo fi = new(fwFile);
                Console.WriteLine("Firmware last write time: " + fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        private bool ProgramChip()
        {
            if (!GetEsprogInfo())
            {
                return false;
            }
            if (!GetEsprogCompileTime())
            {
                return false;
            }
            if (!SetVddCtrl())
            {
                return false;
            }
            if (!SetIoVol())
            {
                return false;
            }
            if (!SetChip())
            {
                return false;
            }
            if (!SetDevAddr())
            {
                return false;
            }
            if (!SetProgZone())
            {
                return false;
            }
            Console.WriteLine("Write firmware to ESPROG start");
            if (!WriteFwToEsprog())
            {
                return false;
            }
            if (!FwWriteChecksum())
            {
                return false;
            }
            Console.WriteLine("Write firmware to chip start");
            if (!FwWriteStart())
            {
                return false;
            }
            return true;
        }

        private bool GetEsprogInfo()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetAppVersion);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = uart.SendCmdFastRsp(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 3)
            {
                Console.WriteLine("Get ESPROG info fail");
                return false;
            }
            Console.WriteLine(string.Format("ESPROG info: {0} ({1} / {2})", recvCmd.Val[0], recvCmd.Val[1], recvCmd.Val[2]));
            return true;
        }

        private bool GetEsprogCompileTime()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdGetAppCompileTime);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = uart.SendCmdFastRsp(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 2)
            {
                Console.WriteLine("Get ESPROG compile time fail");
                return false;
            }
            Console.WriteLine(string.Format("ESPROG compile time: {0}, {1}", recvCmd.Val[0], recvCmd.Val[1]));
            return true;
        }

        private bool SetVddCtrl()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetVddCtrl);
            sendCmd.AddVal(vddVol).AddVal(vddCtrlMode);
            UartCmdModel? recvCmd = uart.SendCmdFastRsp(sendCmd);
            if (recvCmd == null)
            {
                Console.WriteLine("Set VDD voltage & control mode fail");
                return false;
            }
            return true;
        }

        private bool SetIoVol()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetIoVol);
            sendCmd.AddVal(ioVol);
            UartCmdModel? recvCmd = uart.SendCmdFastRsp(sendCmd);
            if (recvCmd == null)
            {
                Console.WriteLine("Set I2C / UART IO voltage fail");
                return false;
            }
            return true;
        }

        private bool SetChip()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetChip);
            sendCmd.AddVal(chip);
            UartCmdModel? recvCmd = uart.SendCmdFastRsp(sendCmd);
            if (recvCmd == null)
            {
                Console.WriteLine("Set chip fail");
                return false;
            }
            return true;
        }

        private bool SetDevAddr()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetDevAddr);
            sendCmd.AddVal(addr);
            UartCmdModel? recvCmd = uart.SendCmdFastRsp(sendCmd);
            if (recvCmd == null)
            {
                Console.WriteLine("Set chip I2C address fail");
                return false;
            }
            return true;
        }

        private bool SetProgZone()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdSetZone);
            sendCmd.AddVal(fwZone);
            UartCmdModel? recvCmd = uart.SendCmdFastRsp(sendCmd);
            if (recvCmd == null)
            {
                Console.WriteLine("Set program zone fail");
                return false;
            }
            return true;
        }

        private bool FwWriteBuf(uint fwAddr, byte[] fwData)
        {
            Console.Write("*");
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwWriteBuf);
            sendCmd.AddVal(fwAddr).AddVal(HexUtil.GetChecksum(fwData, fwData.LongLength)).AddVal(fwData);
            UartCmdModel? recvCmd = uart.SendCmdFastRsp(sendCmd);
            if (recvCmd == null || recvCmd.ValCount != 1)
            {
                return false;
            }
            return HexUtil.GetU32FromStr(recvCmd.Val[0]) == fwAddr;
        }

        private bool WriteFwToEsprog()
        {
            uint fwAddrOffset = ChipDict[chip].MTP.Offset;
            uint maxSize = ChipDict[chip].MTP.Size;
            uint esprogFwBlockSize = maxSize >= 512 ? 512 : maxSize;
            byte[] fwBuffer = new byte[esprogFwBlockSize];
            for (uint fwAddr = 0; fwAddr < maxSize; fwAddr += esprogFwBlockSize)
            {
                if (fwAddr + esprogFwBlockSize < fwDataSize)
                {
                    Array.Copy(fwData, fwAddr, fwBuffer, 0, esprogFwBlockSize);
                }
                else if (fwAddr < fwDataSize)
                {
                    Array.Clear(fwBuffer);
                    Array.Copy(fwData, fwAddr, fwBuffer, 0, fwDataSize - fwAddr);
                }
                else
                {
                    Array.Clear(fwBuffer);
                }
                if (!FwWriteBuf(fwAddr + fwAddrOffset, fwBuffer))
                {
                    Console.WriteLine();
                    Console.WriteLine(string.Format("Write firmware to ESPROG fail at addr ({0})", fwAddr));
                    return false;
                }
            }
            Console.WriteLine();
            Console.WriteLine("Write firmware to ESPROG succeed");
            return true;
        }

        private bool FwWriteChecksum()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwWriteChecksum);
            sendCmd.AddVal(fwZone).AddVal(HexUtil.GetChecksum(fwData, fwDataSize));
            UartCmdModel? recvCmd = uart.SendCmdFastRsp(sendCmd);
            if (recvCmd == null)
            {
                Console.WriteLine("Set firmware checksum fail");
                return false;
            }
            return true;
        }

        private bool FwWriteStart()
        {
            UartCmdModel sendCmd = new(UartCmdModel.CmdFwWriteStart);
            sendCmd.AddVal(true);
            UartCmdModel? recvCmd = uart.SendCmdMultiRsp(sendCmd, byte.MaxValue);
            Console.WriteLine();
            if (recvCmd == null)
            {
                Console.WriteLine("Write firmware to chip fail");
                return false;
            }
            else
            {
                Console.WriteLine("Write firmware to chip succeed");
                return true;
            }
        }
    }
}
