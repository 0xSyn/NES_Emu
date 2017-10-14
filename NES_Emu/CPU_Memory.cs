using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NES_Emu {
    class CPU_Memory {
        public static byte[] memory = new byte[0xFFFFF];//65,536 addresses
        public static UInt16 rom_lowBank = 0xC000;
        /*        CPU Memory Map

        --------------------------------------- $10000
         Upper Bank of Cartridge ROM
        --------------------------------------- $C000  ROM appearing in the CPU address space at $8000-$FFFF
         Lower Bank of Cartridge ROM
        --------------------------------------- $8000
         Cartridge RAM(may be battery-backed)
        --------------------------------------- $6000
         Expansion Modules
        --------------------------------------- $5000
         Input/Output
        --------------------------------------- $2000
         2kB Internal RAM, mirrored 4 times
        --------------------------------------- $0000



        */

        public static void Init_mem() {
            for (int i = 0; i < memory.Length; i++) {
                memory[i] = 0;
            }
        }

        public static FileStream stream;
        public static int Load_NES(String file) {//load program into memory
            if (File.Exists(file)) {
                Console.WriteLine("FILE LOADING...");
                stream = File.Open(file, FileMode.Open);
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                while (CPU.PC < data.Length) {
                    Console.WriteLine("Running");
                    CPU.Emulation_cycle(data);
                }
                //for (int i = 0; i < data.Length; i++) {
                //    memory[i] = data[i];// 4000F bytes
                //}
                Console.Write("LOADED");
            }
            else { Console.WriteLine("FILE NOT FOUND"); }
            return 0;
        }
        public static byte Get_addr(UInt16 addr) {
            return memory[addr];
        }
        public static void Print_mem() {
            for (int i = 0; i < 0x41000; i++) {
                Console.WriteLine("0x"+i.ToString("X")+": "+memory[i].ToString("X"));
            }
        }

    }
}
