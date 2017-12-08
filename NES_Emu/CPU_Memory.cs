using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NES_Emu {
    class CPU_Memory {
    private static byte[] cartridge;
    public static byte[] memory = new byte[0x10000];//65,536 addresses
    public static UInt16 rom_lowBank = 0x8000;
    public static UInt16 rom_highBank = 0xC000;
    public static UInt16 RESET_VECTOR;//=0xFFFC;
    /*     
     *                            -------- CPU Memory Map --------
     *                            ________________________________

    _________________________________________________$10000______________________________
    Upper Bank of Cartridge ROM       $C000-$FFFF       |     $FFFA-$FFFB = NMI vector --- $FFFC-$FFFD = Reset vector --- $FFFE-$FFFF = IRQ/BRK vector
    -------------------------------------------- $C000 -|     ROM - CPU address space $8000-$FFFF
    Lower Bank of Cartridge ROM       $8000-$BFFF       |
    _________________________________________________$8000--------------------------
    Cartridge RAM(may be battery-backed)                |     WRAM
    _________________________________________________$6000--------------------------
    Expansion Modules  rarely used   $5000-$5FFF        | often used as bank switching registers, some cartridges put RAM here
    _________________________________________________$5000_______________________________
    rarely used                      $4020-$4FFF        | ?? Cartridge Start or I/O ??
    -------------------------------------------- $4020 -|--------------------------------
    APU and I/O registers            $4000-$401F        |    Input/Output
    -------------------------------------------- $4000 -|
    $2000-$2007 mirror               every 8 bytes      |    
    -------------------------------------------- $2008 -|
    PPU Registers                    $2000-$2007        |
    _________________________________________________$2000________________________________
    $0-$07FF mirror                  $1800-$1FFF        |
    -------------------------------------------- $1800 -|      $0-$07FF mirrors
    $0-$07FF mirror                  $1000-$17FF        |        $800-$1FFF
    -------------------------------------------- $1000 -| 
    $0-$07FF mirror                  $800-$FFF          |
    -------------------------------------------- $800---|----------------------------
                                                        |    2kB Internal RAM,
    ------------------------- ---------$1FF             |    mirrored 4 times
    Stack (Starts at $1FF)                              |    actual 0x800 bytes ea
    -----------------------------------$FF              |
    Zero Page                                           |    AND address w/ 07FF to get the effective address
    __________________________________________________$0000_______________________________






      So you load a Mapper 0 (NROM) cartridge into memory, and the first two PRG banks appear in NES memory at 8000-FFFF. 
      If there is only one 16k bank, then it is mirrored at 8000-BFFF and C000-FFFF.

      When the CPU boots up, it reads the Reset vector, located at FFFC. 
      That contains a 16-bit value which tells the CPU where to jump to.
      The first thing a game will do when it starts up is repeatedly read PPU register 2002 
      to wait for the NES to warm up, so you won't see a game doing anything until 
      you throw in some rudimentary PPU emulation.
      
      Then the game clears the RAM, and waits for the NES to warm up some more. 
      Then the system is ready, and the game will start running.







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
                cartridge = new byte[stream.Length];
                stream.Read(cartridge, 0, cartridge.Length);
                //for (int i = 0; i < data.Length; i++) {
                  //memory[i+0x8000] = data[i];// 4000F bytes
                //}
                Console.Write("\nFile: "+file);
                detectMapping();
                RESET_VECTOR = (UInt16)((UInt16)(memory[0xFFFD] << 8) | memory[0xFFFC]);
                Print_dbgcmds();
                
        
                //while (CPU.PC < data.Length) {
                  //Console.WriteLine("Running");
                  //CPU.Emulation_cycle(data);
                //}
                
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
        byte mapperNum = (byte)((cartridge[6] >> 4) & (cartridge[7] & 0xF0));
        UInt16 cartridgePointer = 0x10;//skip 16 byte header
        if ((memory[12] + memory[13] + memory[14] + memory[15]) == 0) {// zero filled
          if (((cartridge[7] >> 2) & 0b11) == 0b10) {//If equal to 2, flags 8-15 are in NES 2.0 format
            Console.Write("iNES 2.0 "+ mapperNum);
          }
          else {
            Console.Write("\nMapping: iNES -- Mapper No." + mapperNum);
            

            





            if (((cartridge[6] >> 2) & 1) == 1) {// 1: 512-byte trainer detected, store at $7000-$71FF (stored before PRG data)
              Console.Write("512-byte trainer at $7000-$71FF");
              for (int i = 0; i < cartridgePointer+0x200 ; i++) {
                memory[0x7000 + i] = cartridge[i + cartridgePointer];
              }
              cartridgePointer += 0x200;// 0x20B == 512bytes + 0xB header
            }
            
            Console.Write("\nPRG ROM: " + (cartridge[4] * 0x4000) +" bytes");
            Console.Write("\n   Writing PRG ROM to S8000+.");
            if (cartridge[4] == 1) {
              for (int i = 0; i < 2; i++) {
                for (int j = 0; j < 0x4000; j++) {// PRG ROM
                  memory[0x8000 + j + (i * 0x4000)] = cartridge[j + cartridgePointer];
                }
              }
            }
            else {
              for (int i= 0; i < cartridge[4]*0x4000; i++) {// PRG ROM
                memory[0x8000 + i] = cartridge[i + cartridgePointer];
              }
            }
            cartridgePointer += (UInt16)(cartridge[4] * 0x4000);


            
            if (cartridge[5] > 0) {//CHR ROM
              Console.Write("\nCHR ROM" + (cartridge[5] * 0x2000) + " bytes");
              Console.Write("\n   Loading CHR ROM into Memory $C000+");
              for (int i = 0; i < (cartridge[5] * 0x2000); i++) {// 8kb*data[5]
                PPU_Memory.memory[i] = cartridge[i + cartridgePointer];
              }
              cartridgePointer += (UInt16)(cartridge[5] * 0x2000);
            }
            

            // Make sure all of the cartridge has been used
            Console.Write("\nCartridge Bytes: " + cartridge.Length.ToString("X"));
            Console.Write("\nPointing at: " + cartridgePointer.ToString("X"));
            Console.Write("\nUnused Bytes: " + (cartridge.Length-cartridgePointer).ToString("X"));
            for (int i = cartridgePointer; i < (cartridge.Length); i++) {// 8kb*data[5]
              Console.Write("\n   Unused Byte #"+(i-cartridgePointer)+": "+cartridge[i]);
            }

  
            // Byte 8 -- Size of PRG RAM in 8 KB units (Value 0 infers 8 KB for compatibility; see PRG RAM circuit)





            // FLAG 6
            if ((cartridge[6] & 1) == 0) { }//Mirroring: 0: horizontal (vertical arrangement) (CIRAM A10 = PPU A11)
            else { }//1: vertical (horizontal arrangement) (CIRAM A10 = PPU A10)
            if (((cartridge[6] >> 1) & 1) == 1) { }//1: Cartridge contains battery-backed PRG RAM ($6000-7FFF) or other persistent memory
            if (((cartridge[6] >> 3) & 1) == 1) { }//1: Ignore mirroring control or above mirroring bit; instead provide four-screen VRAM
            
            // FLAG 7
            if ((cartridge[7] & 1) == 0) { }//VS Unisystem --Vs. games have a coin slot and different palettes. The detection of which palette a particular game uses is left unspecified. 
            else { }
            if (((cartridge[7] >> 1) & 1) == 0) { }// PlayChoice-10 (8KB of Hint Screen data stored after CHR data)
            else { }            


            // FLAG 9 --TV System --Though in the official specification, very few emulators honor this bit as virtually no ROM images in circulation make use of it. 
            if ((cartridge[9] & 1) == 0) { }//0: NTSC
            else { }// 1: PAL


            // FLAG 10 -- not part of the official specification, and relatively few emulators honor it.
            switch (cartridge[10] & 0b11) {// TV System
              case 0b0:// 0: NTSC

                break;
              case 0b10:// 2: PAL

                break;
              case 0b11:// 3: Dual Compatible

                break;
              case 0b01:// 1: Dual Compatible

                break;
            }
            //PRG RAM $6000-$7FFF
            if (((cartridge[10] >> 4) & 1) == 0) { }//0: present
            else { }//1: not present

            if (((cartridge[6] >> 5) & 1) == 0) { }//0: Board has no bus conflicts
            else { }//1: Board has bus conflicts



          }
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






    public static void Print_dbgcmds() {
      Console.Write("\n\ncommands: " +
                  "\n\tI = print ROM Header" +
                  "\n\tP = print Memory    $0-$FFFF" +
                  "\n\tS = print Stack     $100-$1FF" +
                  "\n\tS = print Zero Page $0-$FF" +
                  "\n\tL = print Low Bank  $8000-$BFFF" +
                  "\n\tS = print High Bank $C000-$FFFF" +
                  "\n\tR = run" +
                  "\n\tenter = step" +
                  "\n\t..."
                );
    }



    public static void Print_header() {
      Console.Write("\n\nHEADER\n---------\n");
      Console.Write("Byte 0:  " + cartridge[0].ToString("X") + "\n");
      Console.Write("byte 1:  " + cartridge[1].ToString("X") + "\n");
      Console.Write("byte 2:  " + cartridge[2].ToString("X") + "\n");
      Console.Write("Byte 3:  " + cartridge[3].ToString("X") + "\n");
      Console.Write("Byte 4:  " + cartridge[4].ToString("X") + "\n");
      Console.Write("Byte 5:  " + cartridge[5].ToString("X") + "\n");
      Console.Write("flag 6:  " + Convert.ToString(cartridge[6], 2).PadLeft(8, '0') + "\n");
      Console.Write("flag 7:  " + Convert.ToString(cartridge[7], 2).PadLeft(8, '0') + "\n");
      Console.Write("flag 8:  " + Convert.ToString(cartridge[8], 2).PadLeft(8, '0') + "\n");
      Console.Write("flag 9:  " + Convert.ToString(cartridge[9], 2).PadLeft(8, '0') + "\n");
      Console.Write("flag 10: " + Convert.ToString(cartridge[10], 2).PadLeft(8, '0') + "\n");

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
      for (int i = 0x8000; i < 0x8400; i++) {
        Console.WriteLine(
          "$" + i.ToString("X") + ": " + memory[i].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x400).ToString("X") + ": " + memory[i + 0x400].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x800).ToString("X") + ": " + memory[i + 0x800].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0xC00).ToString("X") + ": " + memory[i + 0xC00].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1000).ToString("X") + ": " + memory[i + 0x1000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1400).ToString("X") + ": " + memory[i + 0x1400].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1800).ToString("X") + ": " + memory[i + 0x1800].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1C00).ToString("X") + ": " + memory[i + 0x1C00].ToString("X").PadRight(4, ' ') + " " +
          "$" + (i + 0x2000).ToString("X") + ": " + memory[i + 0x2000].ToString("X").PadRight(4, ' ') + " " +
          "$" + (i + 0x2400).ToString("X") + ": " + memory[i + 0x2400].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x2800).ToString("X") + ": " + memory[i + 0x2800].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x2C00).ToString("X") + ": " + memory[i + 0x2C00].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3000).ToString("X") + ": " + memory[i + 0x3000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3400).ToString("X") + ": " + memory[i + 0x3400].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3800).ToString("X") + ": " + memory[i + 0x3800].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3C00).ToString("X") + ": " + memory[i + 0x3C00].ToString("X").PadRight(4, ' ')
          );
      }
      Console.Write("" +
        "|____________$8000-$8FFF___________________________|  " +
        "|____________$9000-$9FFF___________________________|  " +
        "|____________$A000-$AFFF___________________________|  " +
        "|____________$B000-$BFFF___________________________|  END OF MEMORY" +
        "\n                     __                               _______                  " +//
        "\n                    |  |                             |        |                 $C000 - $FFFF " +//
        "\n                    |  |                             |        |                      4kb      " +//
        "\n                    |  |      .^.  |   |   |         |_______/      .^.    /|  |  || /        " +//
        "\n                    |  |     |   |  | | | |          |        |    /===|   | | |  |+ _        " +//
        "\n                    |  |      !./    |   |           |      |  |   |   |   |  |/  ||  |       " +//
        "\n                    |  |_____________________________|  |___/  |                              " +//
        "\n                    |_________________________________________/                               "
      );
    }

    public static void Print_romhb() {
      Console.WriteLine("\n");
      for (int i = 0xC000; i < 0xC400; i++) {
        Console.WriteLine(
          "$" + i.ToString("X") + ": " + memory[i].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x400).ToString("X") + ": " + memory[i + 0x400].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x800).ToString("X") + ": " + memory[i + 0x800].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0xC00).ToString("X") + ": " + memory[i + 0xC00].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1000).ToString("X") + ": " + memory[i + 0x1000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1400).ToString("X") + ": " + memory[i + 0x1400].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1800).ToString("X") + ": " + memory[i + 0x1800].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x1C00).ToString("X") + ": " + memory[i + 0x1C00].ToString("X").PadRight(4, ' ') + " " +
          "$" + (i + 0x2000).ToString("X") + ": " + memory[i + 0x2000].ToString("X").PadRight(4, ' ') + " " +
          "$" + (i + 0x2400).ToString("X") + ": " + memory[i + 0x2400].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x2800).ToString("X") + ": " + memory[i + 0x2800].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x2C00).ToString("X") + ": " + memory[i + 0x2C00].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3000).ToString("X") + ": " + memory[i + 0x3000].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3400).ToString("X") + ": " + memory[i + 0x3400].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3800).ToString("X") + ": " + memory[i + 0x3800].ToString("X").PadRight(6, ' ') + " " +
          "$" + (i + 0x3C00).ToString("X") + ": " + memory[i + 0x3C00].ToString("X").PadRight(4, ' ')
          );
      }
      Console.Write("" +
        "|____________$C000-$CFFF___________________________|  "+
        "|____________$D000-$DFFF___________________________|  "+
        "|____________$E000-$EFFF___________________________|  "+
        "|____________$F000-$FFFF___________________________|  END OF MEMORY"+
        "\n                     __       __                      _______                  " +//
        "\n                    |  |     |  |                    |        |                 $C000 - $FFFF " +//
        "\n                    |  |     |  |                    |        |                      4kb      " +//
        "\n                    |  |_____|  |  ___   ___  ,  ,   |_______/      .^.    /|  |  || /        " +//
        "\n                    |   _____   |   |   / __  |__|   |        |    /===|   | | |  |+ _        " +//
        "\n                    |  |     |  |  _|_  |__|  |  |   |         |   |   |   |  |/  ||  |       " +//
        "\n                    |  |     |  |                    |         |                              " +//
        "\n                    |__|     |__|                    |________/                               "
      );
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
