using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NES_Emu {
    class CPU_Memory {
        public static byte[] memory = new byte[0x10000];//65,536 addresses
        public static UInt16 rom_lowBank = 0xC000;
/*              -------- CPU Memory Map --------
    
_________________________________________________$10000______________________________
Upper Bank of Cartridge ROM                         |
--------------------------------------- $C000       |     ROM - CPU address space $8000-$FFFF
Lower Bank of Cartridge ROM                         |
_________________________________________________$8000--------------------------
Cartridge RAM(may be battery-backed)                |
_________________________________________________$6000
Expansion Modules                                   |
_________________________________________________$5000_______________________________
                                                    | ?? Cartridge Start or I/O ??
-------------------------------------------- $401F -|--------------------------------
APU & I/O functionality thats normally disabled     |                  
-------------------------------------------- $0417 -|
APU and I/O registers                               |    Input/Output
-------------------------------------------- $3FFF -|
Mirrors $2000-2007 every 8 bytes                    |    
-------------------------------------------- $2007 -|
PPU Registers                                       |
_________________________________________________$2000________________________________
$0-$07FF mirror                                     |
-------------------------------------------- $1FFF -|
$0-$07FF mirror                                     |    $0-$07FF mirrors 
-------------------------------------------- $0FFF -| 
$0-$07FF mirror                                     |
-------------------------------------------- $07FF -|----------------------------
                                                    |
???                                                 |
                                                    |
                                                    |    2kB Internal RAM,
-----------------------------------$1FF             |    mirrored 4 times
Stack (Starts at $1FF)                              |    actual 0x800 bytes ea
-----------------------------------$FF              |
Zero Page                                           |      
__________________________________________________$0000_______________________________
*/

    public static void Init_mem() {
            for (int i = 0; i < memory.Length; i++) {
                memory[i] = 0;
            }
        }

        public static FileStream stream;
        public static int Load_NES(String file) {//load program into memory
            if (File.Exists(file)) {
                Console.Write("\nFILE LOADING...");
                stream = File.Open(file, FileMode.Open);
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
        for (int i = 0; i < data.Length; i++) {
          memory[i+0x8000] = data[i];// 4000F bytes
        }
        Console.Write("\nFile: "+file);
        detectMapping();
        Console.Write("\n\ncommands: " +
          "\n\tP = print Memory" +
          "\n\tS = print Stack" +
          "\n\tS = print Zero Page" +
          "\n\tR = run" +
          "\n\tenter = step" +
          "\n\t..." 
          );
        while (CPU.PC < data.Length) {
                    //Console.WriteLine("Running");
                    CPU.Emulation_cycle(data);
                }
                
            }
            else { Console.Write("FILE NOT FOUND"); }
            return 0;
        }



/*
An iNES file consists of the following sections, in order:
    Header(16 bytes)
    Trainer, if present(0 or 512 bytes)
    PRG ROM data(16384 * x bytes) 0x4000$
    CHR ROM data, if present(8192 * y bytes) 0x2000$
    PlayChoice INST-ROM, if present(0 or 8192 bytes)
    PlayChoice PROM, if present(16 bytes Data, 16 bytes CounterOut) (this is often missing, see PC10 ROM-Images for details)
    Some ROM-Images additionally contain a 128-byte (or sometimes 127-byte) title at the end of the file.

The format of the header is as follows:
    0-3: Constant $4E $45 $53 $1A("NES" followed by MS-DOS end-of-file)
    4: Size of PRG ROM in 16 KB units
    5: Size of CHR ROM in 8 KB units(Value 0 means the board uses CHR RAM)
    6: Flags 6
    7: Flags 7
    8: Size of PRG RAM in 8 KB units(Value 0 infers 8 KB for compatibility; see PRG RAM circuit)
    9: Flags 9
    10: Flags 10 (unofficial)
    11-15: Zero filled
*/







    public static void detectMapping() {
      /*
      If byte 7 AND $0C = $08, and the size taking into account byte 9 does not exceed the actual size of the ROM image, then NES 2.0.
      If byte 7 AND $0C = $00, and bytes 12 - 15 are all 0, then iNES.
      Otherwise, archaic iNES.
      */
      if (memory[7] == memory[0x08] && memory[0x0C] == memory[0x08]) {     
        if ((memory[12] + memory[13] + memory[14] + memory[15]) == 0) {// zero filled
          Console.Write("\nMapping: iNES");
        }
        else if(false){ 
        Console.Write("\nMapping: NES 2.0");
        }
        else {
          Console.Write("\nMapping: Archaic");
        }
      }
      else {
        Console.Write("\nMapping: Archaic");
        
      }
    }
















        public static byte Get_addr(UInt16 addr) {
            return memory[addr];
        }


        public static void Print_mem() {
      Console.WriteLine("\n");
            for (int i = 0; i < memory.Length/8; i++) {
                Console.WriteLine(
                  "$" +        i.ToString("X") + ": " + memory[i].ToString("X").PadRight(6, ' ') + " " +
                  "$" + (i + 0x2000).ToString("X") + ": " + memory[i + 0x2000].ToString("X").PadRight(6, ' ') + " " +
                  "$" + (i + 0x4000).ToString("X") + ": " + memory[i + 0x4000].ToString("X").PadRight(6, ' ') + " " +
                  "$" + (i + 0x6000).ToString("X") + ": " + memory[i + 0x6000].ToString("X").PadRight(6, ' ') + " " +
                  "$" + (i + 0x8000).ToString("X") + ": " + memory[i + 0x8000].ToString("X").PadRight(6, ' ') + " " +
                  "$" + (i + 0xA000).ToString("X") + ": " + memory[i + 0xA000].ToString("X").PadRight(6, ' ') + " " +
                  "$" + (i + 0xC000).ToString("X") + ": " + memory[i + 0xC000].ToString("X").PadRight(6, ' ') + " " +
                  "$" + (i + 0xE000).ToString("X") + ": " + memory[i + 0xE000].ToString("X").PadRight(6, ' ') + " "
                  );
            }
        }


    public static void Print_romlb() {
      Console.WriteLine("\n");
      for (int i = 0x8000; i < 0x8500; i++) {
        Console.WriteLine(
          "$" + i.ToString("X") + ": " + memory[i].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x0500).ToString("X") + ": " + memory[i + 0x0500].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1000).ToString("X") + ": " + memory[i + 0x1000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1500).ToString("X") + ": " + memory[i + 0x1500].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x2000).ToString("X") + ": " + memory[i + 0x2000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x2500).ToString("X") + ": " + memory[i + 0x2500].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3000).ToString("X") + ": " + memory[i + 0x3000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3500).ToString("X") + ": " + memory[i + 0x3500].ToString("X").PadRight(4, ' ') + "|| Low"
          );
      }
    }

    public static void Print_romhb() {
      Console.WriteLine("\n");
      for (int i = 0xC000; i < 0xC500; i++) {
        Console.WriteLine(
          "$" + i.ToString("X") + ": " + memory[i].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x0500).ToString("X") + ": " + memory[i + 0x0500].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1000).ToString("X") + ": " + memory[i + 0x1000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1500).ToString("X") + ": " + memory[i + 0x1500].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x2000).ToString("X") + ": " + memory[i + 0x2000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x2500).ToString("X") + ": " + memory[i + 0x2500].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3000).ToString("X") + ": " + memory[i + 0x3000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3500).ToString("X") + ": " + memory[i + 0x3500].ToString("X").PadRight(4, ' ') + "|| High"
          );
      }
    }












    public static void Print_stack() {
      Console.Write("" +
        "\n                       ____________________                   $100 - $1FF   " +//
        "\n                     /  __________________/                        256 Bytes" +//
        "\n                    /  /                                                    " +//
        "\n                    |  |____________       ====== .^.    ====  || /         " +//
        "\n                     |___________   |        ||  /===|  /      |+ _         " +//
        "\n                                  |  |       ||  |   |  '====  ||  |        " +//
        "\n               ___________________/  /                                      " +//
        "\n              /_____________________/                                       " +//
        "\nSTART ADDR:                                                                 "  //



        );
      for (int i = 0x1FF; i > 0x1DF; i--) {
        Console.Write(
          "\n$" + i.ToString("X") + ": " + memory[i].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i - 0x20).ToString("X") + ": " + memory[i - 0x20].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i - 0x40).ToString("X") + ": " + memory[i - 0x40].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i - 0x60).ToString("X") + ": " + memory[i - 0x60].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i - 0x80).ToString("X") + ": " + memory[i - 0x80].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i - 0xA0).ToString("X") + ": " + memory[i - 0xA0].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i - 0xC0).ToString("X") + ": " + memory[i - 0xC0].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i - 0xE0).ToString("X") + ": " + memory[i - 0xE0].ToString("X").PadRight(6, ' ') + " "
          );
      }
    }




    public static void Print_zeropage() {
      Console.Write("" +
        "\n                   ___________________________                     $0 - $FF        " +//
        "\n                  /______________________  _./                         256 bytes   " +//
        "\n                                    _./ _./                                        " +//
        "\n                                _./ _./       |***  |**;   .^.                     " +//
        "\n                            _./ _./           |---  |**.  |   |   P4G3             " +//
        "\n                       _./ _./                |___  |  |   !./                     " +//
        "\n                   _./ _./__________________________________________ ___ __ _ _ _  " +//
        "\n                  /______________________/______/_____/_____/___/__/__/__/_/////   " +//
        "\nSTART ADDR:                                                                        "  //



        );
      for (int i = 0x0; i < 0x20; i++) {
        Console.Write(
          "\n$" + i.ToString("X") + ": " + memory[i].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x20).ToString("X") + ": " + memory[i + 0x20].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x40).ToString("X") + ": " + memory[i + 0x40].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x60).ToString("X") + ": " + memory[i + 0x60].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x80).ToString("X") + ": " + memory[i + 0x80].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0xA0).ToString("X") + ": " + memory[i + 0xA0].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0xC0).ToString("X") + ": " + memory[i + 0xC0].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0xE0).ToString("X") + ": " + memory[i + 0xE0].ToString("X").PadRight(6, ' ') + " "
          );
      }
    }



  }
}
