using ESPROGConsole.Services;
using System;
using System.Collections.Generic;
using System.CommandLine;

namespace ESPROGConsole
{
    internal class Program
    {

        static void Main(string[] args)
        {
            RootCommand rootCommand = new("ESPROG console application");

            Option<string> optionFw = new("--firmware", "Binary file to program");
            optionFw.IsRequired = true;
            rootCommand.AddOption(optionFw);

            Option<ushort> optionChip = new("--chip", "Chip part number");
            optionChip.IsRequired = true;
            rootCommand.AddOption(optionChip);

            Option<byte> optionChipAddr = new("--addr", "Chip i2c address");
            optionChipAddr.IsRequired = true;
            rootCommand.AddOption(optionChipAddr);

            Option<string> optionPort = new("--port", "ESPROG serial port name");
            optionPort.IsRequired = true;
            rootCommand.AddOption(optionPort);

            Option<byte> optionVddCtrlMode = new("--vddctrlmode", "VDD control mode. 0 - ondemand, 1 - always on, 2 - always off");
            optionVddCtrlMode.IsRequired = true;
            rootCommand.AddOption(optionVddCtrlMode);

            Option<byte> optionVddVol = new("--vddvol", "VDD voltage. 0 - 5.0V, 1 - 3.3V");
            optionVddVol.IsRequired = true;
            rootCommand.AddOption(optionVddVol);

            Option<byte> optionIoVol = new("--iovol", "I2C / UART IO voltage. 0 - 1.8V, 1 - 3.3V, 2 - 5.0V, 3 - 5.0V with high-sink SCL");
            optionIoVol.IsRequired = true;
            rootCommand.AddOption(optionIoVol);

            rootCommand.SetHandler(Run, optionFw, optionChip, optionChipAddr, optionPort, optionVddCtrlMode, optionVddVol, optionIoVol);
            rootCommand.Invoke(args);
        }

        private static void Run(string fw, ushort chip, byte addr, string port, byte vddCtrlMode, byte vddVol, byte IoVol)
        {
            if (!CheckArgs(fw, chip, addr, port, vddCtrlMode, vddVol, IoVol))
            {
                return;
            }
            DispInfo(fw, chip, addr, port, vddCtrlMode, vddVol, IoVol);
            Console.WriteLine("----- ----- ----- ----- ----- ----- ----- ----- ----- -----");

            NuProgService nuProg = new(fw, chip, addr, port, vddCtrlMode, vddVol, IoVol);
            bool res = nuProg.Run();

            Console.WriteLine("----- ----- ----- ----- ----- ----- ----- ----- ----- -----");
            Console.WriteLine("Programming chip " + (res ? "succeed" : "fail"));
        }

        private static void DispInfo(string fw, ushort chip, byte addr, string port, byte vddCtrlMode, byte vddVol, byte ioVol)
        {
            Console.WriteLine("Firmware: " + fw);
            Console.WriteLine("Chip: NU" + Convert.ToString(chip, 16).PadLeft(4, '0'));
            Console.WriteLine("Chip I2C address: 0x" + Convert.ToString(addr, 16).PadLeft(2, '0'));
            Dictionary<byte, string> vddCtrlModeDict = new() { { 0, "ondemand" }, { 1, "always on" }, { 2, "always off" } };
            Console.WriteLine("VDD control mode: " + vddCtrlModeDict[vddCtrlMode]);
            Dictionary<byte, string> vddVolDict = new() { { 0, "5.0V" }, { 1, "3.3V" } };
            Console.WriteLine("VDD voltage: " + vddVolDict[vddVol]);
            Dictionary<byte, string> ioVolDict = new() { { 0, "1.8V" }, { 1, "3.3V" }, { 2, "5.0V" }, { 3, "5.0V with high-sink SCL" } };
            Console.WriteLine("I2C / UART IO voltage: " + ioVolDict[ioVol]);
            Console.WriteLine("ESPROG serial port: " + port);
        }

        private static bool CheckArgs(string fw, ushort chip, byte addr, string port, byte vddCtrlMode, byte vddVol, byte IoVol)
        {
            if (string.IsNullOrEmpty(fw))
            {
                Console.WriteLine("Firmware name is empty.");
                return false;
            }
            if (chip != 0x1708 && chip != 0x1718 && chip != 0x1651 && chip != 0x1652 && chip != 0x1628)
            {
                Console.WriteLine("Chip is invalid - NU" + Convert.ToString(chip, 16).PadLeft(4, '0'));
                return false;
            }
            if (string.IsNullOrEmpty(port))
            {
                Console.WriteLine("ESPROG serial port name is empty.");
                return false;
            }
            if (vddCtrlMode != 0 && vddCtrlMode != 1 && vddCtrlMode != 2)
            {
                Console.WriteLine("VDD control mode is invalid - " + vddCtrlMode);
                return false;
            }
            if (vddVol != 0 && vddVol != 1)
            {
                Console.WriteLine("VDD voltage is invalid - " + vddVol);
                return false;
            }
            if (IoVol != 0 && IoVol != 1 && IoVol != 2 && IoVol != 3)
            {
                Console.WriteLine("I2C / UART IO voltage is invalid - " + IoVol);
                return false;
            }
            return true;
        }
    }
}
