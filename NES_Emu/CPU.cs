using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emu {
  class CPU {// 6502 microprocessor 

    static byte SP;//8-bit stack pointer(fixed at RAM address $100, so can address $100 -$1ff)
    static UInt16 PC;//16 - bit program counter
    static UInt16 opcode;
    static UInt16 addr;
    static byte src;
    static int cycle_num;
    static bool DEBUG = true;
    static string[] dbg = new string[0x10];
    static byte[] stack = new byte[0xFF];

    /// <summary>
    /// ACCUMULATOR
    /// 8bit register
    /// the only register that has instructions for performing math.
    /// </summary>
    static byte A;//3 8-bit general purpose registers A, X, and Y

    /// <summary>
    /// X INDEX
    /// 8bit register
    /// </summary>
    static byte X;

    /// <summary>
    /// Y INDEX
    /// 8bit register
    /// </summary>
    static byte Y;

    /// <summary>
    /// STATUS
    /// 8bit register
    /// 
    /// Bit0 - C - Carry flag: this holds the carry out of the most significant bit in any arithmetic operation.
    ///  In subtraction operations, this flag is cleared - 
    ///  set to 0 if a borrow is required, set to 1 - if no borrow is required.
    ///  The carry flag is also used in shift and rotate logical operations.
    /// Bit1 - Z - Zero flag: set to 1 when any arithmetic or logical operation produces a zero result --- set to 0 if the result is non-zero
    /// Bit2 - I: interrupt enable/disable flag. 1 == interrupts disabled. 0 == interrupts are enabled.
    /// Bit3 - D: this is the decimal mode status flag. When set, and an Add with Carry or Subtract with Carry instruction is executed, the source values are
    ///  treated as valid BCD (Binary Coded Decimal, eg. 0x00-0x99 = 0-99) numbers. The result generated is also a BCD number.
    /// Bit 4 - B: this is set when a software interrupt (BRK instruction) is executed.
    /// Bit 5: not used. Supposed to be logical 1 at all times.
    /// Bit 6 - V - Overflow flag: when an arithmetic operation produces a result too large to be represented in a byte, V is set.
    /// Bit 7 - N - Negative Sign flag: this is set if the result of an operation is negative, cleared if positive.
    /// </summary>
    static byte S;// 0b SV5B DIZC
                  //148 total instructions, (a lot of these are the very similar)
                  //Little Endian architecture
                  //

    public static void Init_cpu() {
      Clear_dbg();
      Clear_stack();
      SP = 0xFF;
      PC = 0;//CPU_Memory.rom_lowBank;
      opcode = 0x0;
      A = 0x0;
      X = 0x0;
      Y = 0x0;
      S = 0x20;// bit 5 = 1
      
      CPU_Memory.Init_mem();
      //CPU_Memory.Load_NES(@"D:\Projects\NES_Emu\ff.nes");
      CPU_Memory.Load_NES(@"D:\Projects\NES_Emu\dk.nes");
      PC = (UInt16)((CPU_Memory.memory[0xFFFC]<<8)| CPU_Memory.memory[0xFFFD]);//Reset Vector
      //Print_status();
      runProgram();
    }

    public static void runProgram() {
      while (true) {
        CPU.Emulation_cycle();
      }
    }



    //___________________________________________________________________________________________________________________________________________
    //                                                                                                                                      DEBUG
    //___________________________________________________________________________________________________________________________________________
    public static void Clear_dbg() { for (int i = 0; i < dbg.Length; i++) { dbg[i] = " "; } }
    public static void Print_dbg() { 
      for (int i = 0; i < dbg.Length; i++) {
        if (dbg[i].Length > 1) { Console.Write("\n"+dbg[i]); } 
      } 
    }
    public static void Print_status() {
      string result = Convert.ToString(S, 2).PadLeft(8, '0') + 
        "   0x" + A.ToString("X").PadRight(5, ' ') + 
        "0x" + X.ToString("X").PadRight(5, ' ') + 
        "0x" + Y.ToString("X").PadRight(5, ' ') + 
        "0x" + PC.ToString("X").PadRight(7, ' ') + 
        "0x" + SP.ToString("X").PadRight(5, ' ');
      Console.Write("\nSTATUS: nv1bdizc   A      X      Y      PC       SP");
      Console.Write("\n        " + result);
      //Console.WriteLine("      " + (0x80 & S) + " " + (0x40 & S) + " " + (0x20 & S) + " " + (0x10 & S) + " " + (0x8 & S) + " " + (0x8 & S) + " " + (0x2 & S) + " " + (0x1 & S));
    }
    public static void Print_select() {
      while (DEBUG) {
        DEBUG = false;
        switch (Console.ReadKey().Key) {
          case ConsoleKey.I:
            CPU_Memory.Print_header();
            DEBUG = true;
            break;
          case ConsoleKey.P:
            CPU_Memory.Print_mem();
            DEBUG = true;
            break;
          case ConsoleKey.S:
            CPU_Memory.Print_stack();
            DEBUG = true;
            break;
          case ConsoleKey.Z:
            CPU_Memory.Print_zeropage();
            DEBUG = true;
            break;
          case ConsoleKey.L:
            CPU_Memory.Print_romlb();
            DEBUG = true;
            break;
          case ConsoleKey.H:
            CPU_Memory.Print_romhb();
            DEBUG = true;
            break;

          case ConsoleKey.R:
            DEBUG = false;
            break;

          default:
            break;

        }        
      }
      DEBUG = true;
    }
 

    //___________________________________________________________________________________________________________________________________________
    //                                                                                                                       MEMORY / STACK STUFF
    //___________________________________________________________________________________________________________________________________________
    public static void Clear_stack() { for (int i = 0xFF; i < 0x20; i++) { CPU_Memory.memory[i] = 0; } }// stack is 0xff to 0x1ff
    //public static void Clear_stack() { for (int i = 0; i < stack.Length; i++) { stack[i] = 0; } }


    /// <summary>
    /// Push Byte onto stack
    /// </summary>
    /// <param name="stackEntry">Byte to push</param>
    public static void PUSH(byte stackEntry) {
      if ((SP - 1) < 0x0) {
        Console.Write("\n\n\n\n\n\n\n                          !!!   STACK OVERFLOW   !!!\n\n\n\n\n\n\n ");
      }
      CPU_Memory.memory[0x100+SP--] = stackEntry;
    }
    

/// <summary>
/// Pull Byte from stack. Decrement pointer
/// </summary>
/// <returns>top element of stack</returns>
    public static byte PULL() {
      if ((SP + 1) > 0xFF) {
        Console.Write("\n\n\n\n\n\n\n                            !!!   STACK UNDERFLOW   !!!\n\n\n\n\n\n\n ");
      }
      return CPU_Memory.memory[++SP+0x100];
    }


    /// <summary>
    /// Returns the value located at the input address
    /// <br>16bit arg ... returns 8bits</br>
    /// </summary>
    /// <param name="arg">UInt16 Address</param>
    /// <returns>Byte memory[address]</returns>
    public static byte PEEK(UInt16 arg) {
      return CPU_Memory.memory[arg];
    }

/// <summary>
/// Store Byte in Memory
/// </summary>
/// <param name="dest"></param>
/// <param name="srcbyte"></param>
    public static void STORE(UInt16 dest, byte srcbyte) {
      CPU_Memory.memory[dest] = srcbyte;
    }
    public static UInt16 LOAD_MEM(UInt16 atAddr) {
      return CPU_Memory.memory[atAddr];
    }











    //___________________________________________________________________________________________________________________________________________
    //                                                                                                                       PROCESSOR STATUS OPs
    //___________________________________________________________________________________________________________________________________________

    /// <summary>
    /// Set sign to the 7th bit of input parameter
    /// </summary>
    /// <param name="tmp">uses 7th bit</param>
    public static void SET_SIGN(UInt16 tmp) {
      if ((0b10000000 & tmp) > 0) { S = (byte)(S | 0b10000000); }// if (7th bit of parameter == 1) negative sign = 1
      else                        { S = (byte)(S & 0b01111111); }// else negative sign = 0
    }
    /// <summary>
    /// if the condition has a non-zero value then the carry flag is set, else it is reset.
    /// </summary>
    /// <param name="condition"></param>
    public static void SET_CARRY(bool condition) {
      if (condition) { S = (byte)(S | 0b1); }// CARRY = 1
      else { S = (byte)(S & 0b0); }// CARRY = 0
    }
    /// <summary>
    /// sets\resets the zero flag depending on whether the result is zero or not.
    /// </summary>
    /// <param name="condition"></param>
    public static void SET_ZERO(UInt16 condition) {
      if (condition == 0) { S = (byte)(S | 0b0000010); }// ZERO = 1
      else                { S = (byte)(S & 0b1111101); }// ZERO = 0
    }

/// <summary>
/// 
/// </summary>
/// <param name="tmp"></param>
    public static void SET_OVERFLOW(UInt16 tmp) {
      if (tmp > 0) { S = (byte)(S | 0b1000000); }// OVERFLOW = 1
      else { S = (byte)(S & 0b0111111); }// OVERFLOW = 0
    }

    //public static void SET_OVERFLOW(bool condition) {
    //    if (condition) { S = (byte)(S | 0b1000000); }// OVERFLOW = 1
    //    else           { S = (byte)(S & 0b0111111); }// OVERFLOW = 0
    //}






    /// <summary>
    /// Generic get flag
    /// </summary>
    /// <param name="flag">STATUS register bit to return</param>
    /// <returns>Flag Bit</returns>
    public static int GET_FLAG(char flag) {
      byte flag_bit = get_flag(flag);
      return ((S & (0b00000001 << flag_bit)) >> flag_bit);// Returns flag value: 0 or 1
    }

    /// <summary>
    /// Sets a flag value of status register to either 1 or 0;
    /// </summary>
    /// <param name="flag">c = CARRY (bit0) --- z = ZERO(bit1) --- i = INTERRUPT ENABLE (bit2) --- d = DECIMAL MODE(bit3) --- b = BREAK (bit4) --- ALWAYS 1 (bit5) --- v=OVERFLOW (bit6) --- n=NEGATIVE SIGN (bit7)</param>
    /// <param name="val">New Flag Value: 0 or 1</param>
    public static void SET_FLAG(char flag, byte val) {//BROKEN
      byte flag_bit = get_flag(flag);
      if (val == 1) { S = (byte)(S | (val << flag_bit)); }// FLAG = 1
      else { S = (byte)(S & (val << flag_bit)); }// FLAG = 0
    }

    public static byte get_flag(char flag) {
      switch (flag) {
        case 'c': return 0;
        case 'z': return 1;
        case 'i': return 2;
        case 'd': return 3;
        case 'b': return 4;
        case ' ': return 5;
        case 'v': return 6;
        case 'n': return 7;
        default: Console.WriteLine("ERR: FLAG '" + flag + "' DNE"); return 8;
      }
    }


    //___________________________________________________________________________________________________________________________________________
    //                                                                                                                            ADRESSING MODES
    //___________________________________________________________________________________________________________________________________________
    // ZERO PAGE == addr & 0x00FF
    // addr == $7F3E --- zero page addr == $003E
    // $7F is the high order byte, aka page
    // $3e is the low order

    /// <summary>
    /// a,x == addr + X index
    /// a,y == addr + Y index
    /// a   == addr
    /// </summary>
    /// <param name="addr">addr == a+x | a+y | a</param>
    public static void Absolute(UInt16 addr) {
      Console.Write("Addressing Mode Absolute() -- Assumed Working");
      src = PEEK(addr);
    }



    /// <summary>
    /// Zero Page Indexed
    /// d,x == addr + X index
    /// d,y == addr + Y index
    /// d   == addr
    /// </summary>
    /// <param name="addr">addr == d+x | d+y | d</param>
    /// <returns></returns>
    public static void ZeroPage(UInt16 addr) {
      Console.Write("Addressing Mode ZeroPage() -- Assumed Working");
      src = PEEK((UInt16)(addr % 256));
      //addr = (UInt16)(addr & 0x00FF);
    }




    public static void Indexed_Indirect_dx() {
      Console.Write("Addressing Mode Indexed Indirect (d,x) -- Assumed Working");
      src = PEEK((UInt16)(PEEK((UInt16)((addr + X) % 256)) + PEEK((UInt16)(((addr + X + 1) % 256) * 256))));
    }
    public static void Indexed_Indirect_dy() {
      Console.Write("Addressing Mode Indexed Indirect (d),y -- BROKEN");
      //src = (UInt16)(PEEK((UInt16)((addr + 1) % 256))* 256 + Y);
      src = PEEK(PEEK(addr));
      src = PEEK((UInt16)(PEEK(addr) + (UInt16)(PEEK((UInt16)((addr + 1) % 256)) * 256 + Y)));
      //src = PEEK(PEEK(addr) + (UInt16)(PEEK((addr + 1) % 256) * 256 + Y));
    }
    public static void Accumulator(UInt16 addr) {
      Console.Write("Addressing Mode Accumulator -- BROKEN");
      src = (byte)(addr & 0x00FF);
    }
    public static void Implied() {
      Console.Write("Addressing Mode Implied -- BROKEN");
      src = (byte)(addr & 0x00FF);
    }

    /// <summary>
    /// Uses the 8-bit operand itself as the value for the operation, rather than fetching a value from a memory address. 
    /// </summary>
    /// <param name="addr"></param>
    public static void Immediate() {
      Console.Write("Addressing Mode Immediate -- Assumed Working");
      src = (byte)((PC>>8) & 0x00FF);
    }





















    //___________________________________________________________________________________________________________________________________________
    //                                                                                                                            EMULATION CYCLE
    //___________________________________________________________________________________________________________________________________________

    public static void Emulation_cycle() {
      Print_select();
      Console.WriteLine("\n_______________\nCycle# " + cycle_num);
      Print_status();
      cycle_num++;
      opcode = (CPU_Memory.memory[PC++]);
      UInt16 addr = (UInt16)((CPU_Memory.memory[PC] << 8) | (CPU_Memory.memory[PC + 1]));

      Console.Write("\n\n$"+ PC.ToString("X") + " contains: "+addr.ToString("X")+"\n");
      

      //Print_status();

      switch (opcode & 0x00FF) {



        /*
        dbg[0x0] = "ADC               Add memory to accumulator with carry                ADC";
        dbg[0x1] = "Operation:  A + M + C -> A, C                         N Z C I D V";
        dbg[0x2] = "(Ref: 2.2.1)                                          / / / _ _ /";
        dbg[0x3] = " | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|";
        dbg[0x4] = " |  Immediate     |   ADC #Oper           |    69   |    2    |    2     |";
        dbg[0x5] = " |  Zero Page     |   ADC Oper            |    65   |    2    |    3     |";
        dbg[0x6] = " |  Zero Page,X   |   ADC Oper,X          |    75   |    2    |    4     |";
        dbg[0x7] = " |  Absolute      |   ADC Oper            |    6D   |    3    |    4     |";
        dbg[0x8] = " |  Absolute,X    |   ADC Oper,X          |    7D   |    3    |    4*    |";
        dbg[0x9] = " |  Absolute,Y    |   ADC Oper,Y          |    79   |    3    |    4*    |";
        dbg[0xA] = " |  (Indirect,X)  |   ADC (Oper,X)        |    61   |    2    |    6     |";
        dbg[0xB] = " |  (Indirect),Y  |   ADC (Oper),Y        |    71   |    2    |    5*    |";
        dbg[0xC] = " * Add 1 if page boundary is crossed.";
        */
        case 0x69:
        case 0x65:
        case 0x75:
        case 0x6D:
        case 0x7D:
        case 0x79:
        case 0x61:
        case 0x71:
          switch (opcode & 0x00FF) {
            case 0x69:
              dbg[0x0] = "69 - ADC - Immediate";
              dbg[0xF] = "BROKEN";
              break;
            case 0x65:
              dbg[0x0] = "65 - ADC - Zero Page";
              dbg[0xF] = "BROKEN";
              ZeroPage(addr);
              break;
            case 0x75:
              dbg[0x0] = "75 - ADC - Zero Page,X";
              dbg[0xF] = "BROKEN";
              ZeroPage((byte)(addr+X));
              break;
            case 0x6D:
              dbg[0x0] = "6D - ADC - Absolute";
              dbg[0xF] = "BROKEN";
              Absolute(addr);
              break;
            case 0x7D:
              dbg[0x0] = "7D - ADC - Absolute,X";
              dbg[0xF] = "BROKEN";
              Absolute((byte)(addr + X));
              break;
            case 0x79:
              dbg[0x0] = "79 - ADC - Absolute,Y";
              dbg[0xF] = "BROKEN";
              Absolute((byte)(addr + Y));
              break;
            case 0x61:
              dbg[0x0] = "61 - ADC - (Indirect,X)";
              dbg[0xF] = "BROKEN";
              Indexed_Indirect_dx();
              break;
            case 0x71:
              dbg[0x0] = "71 - ADC - (Indirect),Y";
              dbg[0xF] = "BROKEN";
              break;
            default:
              break;
          }

          UInt16 tmp = (UInt16)(src + A + (GET_FLAG('c')));
          SET_ZERO((UInt16)(tmp & 0xFF));//  /* This is not valid in decimal mode */
          if (GET_FLAG('d') == 1) {
            if (((A & 0xf) + (src & 0xf) + (GET_FLAG('c'))) > 9) { tmp += 6; }
            SET_SIGN(tmp);
            //SET_OVERFLOW(!((A ^ src) & 0x80) && ((A ^ tmp) & 0x80));
            if (tmp > 0x99) { tmp += 96; }
            SET_CARRY(tmp > 0x99);
          }
          else {
            SET_SIGN(tmp);
            //SET_OVERFLOW(!((A ^ src) & 0x80) && ((A ^ tmp) & 0x80));
            SET_CARRY(tmp > 0xff);
          }
          A = ((byte)tmp);
          PC += 2;
          break;



        /*
        dbg[0x0] = " AND - 'AND' memory with accumulator                           N Z C I D V";
        dbg[0x1] = " Operation:  A & M -> A                                        / / _ _ _ _";
        dbg[0x3] = " | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|";
        dbg[0x4] = " |  Immediate     |   AND #Oper           |    29   |    2    |    2     |";
        dbg[0x5] = " |  Zero Page     |   AND Oper            |    25   |    2    |    3     |";
        dbg[0x6] = " |  Zero Page,X   |   AND Oper,X          |    35   |    2    |    4     |";
        dbg[0x7] = " |  Absolute      |   AND Oper            |    2D   |    3    |    4     |";
        dbg[0x8] = " |  Absolute,X    |   AND Oper,X          |    3D   |    3    |    4*    |";
        dbg[0x9] = " |  Absolute,Y    |   AND Oper,Y          |    39   |    3    |    4*    |";
        dbg[0xA] = " |  (Indirect,X)  |   AND (Oper,X)        |    21   |    2    |    6     |";
        dbg[0xB] = " |  (Indirect,Y)  |   AND (Oper),Y        |    31   |    2    |    5     |";
        dbg[0xC] = " * Add 1 if page boundary is crossed.                       (Ref: 2.2.3.0)";
        */
        case 0x29:
        case 0x25:
        case 0x35:
        case 0x2D:
        case 0x3D:
        case 0x39:
        case 0x21:
        case 0x31:
          switch (opcode & 0x00FF) {// Set individual OP data
            case 0x29:
              dbg[0x0] = "29 - AND - Immediate";
              dbg[0xF] = "BROKEN";
              break;
            case 0x25:
              dbg[0x0] = "25 - AND - Zero Page";
              dbg[0xF] = "BROKEN";
              break;
            case 0x35:
              dbg[0x0] = "35 - AND - Zero Page,X";
              dbg[0xF] = "BROKEN";
              break;
            case 0x2D:
              dbg[0x0] = "2D - AND - Absolute";
              dbg[0xF] = "BROKEN";
              break;
            case 0x3D:
              dbg[0x0] = "3D - AND - Absolute,X";
              dbg[0xF] = "BROKEN";
              break;
            case 0x39:
              dbg[0x0] = "39 - AND - Absolute,Y";
              dbg[0xF] = "BROKEN";
              break;
            case 0x21:
              dbg[0x0] = "21 - AND - (Indirect,X)";
              dbg[0xF] = "BROKEN";
              break;
            case 0x31:
              dbg[0x0] = "31 - AND - (Indirect),Y";
              dbg[0xF] = "BROKEN";
              break;
          }
          //PRIMARY OPERATION
          
          src &= A;
          SET_SIGN(src);
          SET_ZERO(src);
          A = src;
          PC += 2;
          break;







        /*
        ASL - Shift Left One Bit (Memory or Accumulator)          N Z C I D V
                         +-+-+-+-+-+-+-+-+                        / / / _ _ _
        Operation:  C <- |7|6|5|4|3|2|1|0| <- 0
                         +-+-+-+-+-+-+-+-+                                          
        +----------------+-----------------------+---------+---------+----------+
        | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
        +----------------+-----------------------+---------+---------+----------+
        |  Accumulator   |   ASL A               |    0A   |    1    |    2     |
        |  Zero Page     |   ASL Oper            |    06   |    2    |    5     |
        |  Zero Page,X   |   ASL Oper,X          |    16   |    2    |    6     |
        |  Absolute      |   ASL Oper            |    0E   |    3    |    6     |
        |  Absolute, X   |   ASL Oper,X          |    1E   |    3    |    7     |
        +----------------+-----------------------+---------+---------+----------+ (Ref: 10.2)         
        */
        case 0x0A:
        case 0x06:
        case 0x16:
        case 0x0E:
        case 0x1E:
          switch (opcode & 0x00FF) {// Set individual OP data
            case 0x0A:
              dbg[0x0] = "0A - ASL - Accumulator";
              break;
            case 0x06:
              dbg[0x0] = "06 - ASL - Zero Page ";
              break;
            case 0x16:
              dbg[0x0] = "16 - ASL - Zero Page,X";
              break;
            case 0x0E:
              dbg[0x0] = "0E - ASL - Absolute";
              break;
            case 0x1E:
              dbg[0x0] = "1E - ASL - Absolute,X";
              break;

          }
          //PRIMARY OPERATION
          dbg[0xF] = "BROKEN";
          //SET_CARRY(src & 0x80);
          src <<= 1;
          src &= 0xff;
          SET_SIGN(src);
          SET_ZERO(src);
          //STORE src in memory or accumulator depending on addressing mode.
          //A=src;
          //STORE(addr, src)
          PC += 2;
          break;



        /*
        BCC - Branch on Carry Clear                                  N Z C I D V                                           
        Operation:  Branch on C = 0                                  _ _ _ _ _ _                 
        +----------------+-----------------------+---------+---------+----------+
        | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
        +----------------+-----------------------+---------+---------+----------+
        |  Relative      |   BCC Oper            |    90   |    2    |    2*    |
        +----------------+-----------------------+---------+---------+----------+
        * Add 1 if branch occurs to same page.                     (Ref: 4.1.1.3)
        * Add 2 if branch occurs to different page.
       */
        case 0x90:
          dbg[0x0] = "90 - BCC";
          //if (!IF_CARRY()) {
          //    clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
          //    PC = REL_ADDR(PC, src);
          //}
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;




        /*
          BCS - Branch on carry set                                     N Z C I D V
          Operation:  Branch on C = 1                                   _ _ _ _ _ _                                                 
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Relative      |   BCS Oper            |    B0   |    2    |    2*    |
          +----------------+-----------------------+---------+---------+----------+
          * Add 1 if branch occurs to same  page.                    (Ref: 4.1.1.4)
          * Add 2 if branch occurs to next  page.
         */
        case 0xB0:
          dbg[0x0] = "B0 - BCS";
          dbg[0xF] = "BROKEN";
          //if (IF_CARRY()) {
          //    clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
          //    PC = REL_ADDR(PC, src);
          //}
          PC += 2;
          break;




        /*
        BEQ - Branch on result zero                                  N Z C I D V                                                                     
        Operation:  Branch on Z = 1                                  _ _ _ _ _ _ 
        +----------------+-----------------------+---------+---------+----------+
        | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
        +----------------+-----------------------+---------+---------+----------+
        |  Relative      |   BEQ Oper            |    F0   |    2    |    2*    |
        +----------------+-----------------------+---------+---------+----------+
        * Add 1 if branch occurs to same  page.                    (Ref: 4.1.1.5)
        * Add 2 if branch occurs to next  page.
        */
        case 0xF0:
          dbg[0x0] = "F0 - BEQ";
          dbg[0xF] = "BROKEN";
          //if (IF_ZERO()) {
          //    clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
          //    PC = REL_ADDR(PC, src);
          //}
          PC += 2;
          break;



        /*
         BIT - Test bits in memory with accumulator                   N Z C I D V      
         Operation:  A /\ M, M7 -> N, M6 -> V                         M7/ _ _ _ M6
         Bit 6 and 7 are transferred to the status register.   
         If the result of A /\ M is zero then Z = 1, otherwise Z = 0
         +----------------+-----------------------+---------+---------+----------+
         | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
         +----------------+-----------------------+---------+---------+----------+
         |  Zero Page     |   BIT Oper            |    24   |    2    |    3     |
         |  Absolute      |   BIT Oper            |    2C   |    3    |    4     |
         +----------------+-----------------------+---------+---------+----------+ (Ref: 4.2.1.1)
        */
        case 0x24:
        case 0x2C:
          switch (opcode & 0x00FF) {
            case 0x24:
              dbg[0x0] = "24 - BIT - Zero Page";
              src = (byte)(addr & 0xFF);
              break;
            case 0x2C:
              dbg[0x0] = "2C - BIT - Absolute";
              src = (byte)(addr);
              break;
              //default: src = 0;break;
          }
          dbg[0x1] = "src: 0x" + src.ToString("X");
          dbg[0xF] = "BROKEN";
          SET_SIGN(src);
          SET_OVERFLOW((byte)(0x40 & src));   /* Copy bit 6 to OVERFLOW flag. */
          SET_ZERO((byte)(src & A));
          PC += 2;
          break;





        /*
        BMI - Branch on result minus                                 N Z C I D V
        Operation:  Branch on N = 1                                  _ _ _ _ _ _  
        (Ref: 4.1.1.1)
        +----------------+-----------------------+---------+---------+----------+
        | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
        +----------------+-----------------------+---------+---------+----------+
        |  Relative      |   BMI Oper            |    30   |    2    |    2*    |
        +----------------+-----------------------+---------+---------+----------+
        * Add 1 if branch occurs to same page.
        * Add 1 if branch occurs to different page.
        */
        case 0x30:
          dbg[0x0] = "30 - BMI";
          dbg[0xF] = "BROKEN";
          //if (IF_SIGN()) {
          //    clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
          //    PC = REL_ADDR(PC, src);
          //}
          PC += 2;
          break;








        /*
        BNE - Branch on result not zero                              N Z C I D V
        Operation:  Branch on Z = 0                                  _ _ _ _ _ _                                                                                                          
        +----------------+-----------------------+---------+---------+----------+
        | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
        +----------------+-----------------------+---------+---------+----------+
        |  Relative      |   BMI Oper            |    D0   |    2    |    2*    |
        +----------------+-----------------------+---------+---------+----------+
        * Add 1 if branch occurs to same page.                     (Ref: 4.1.1.6)
        * Add 2 if branch occurs to different page.
        */
        case 0xD0:
          dbg[0x0] = "D0 - BNE";
          dbg[0xF] = "BROKEN";

          //if (!IF_ZERO()) {
          //    clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
          //    PC = REL_ADDR(PC, src);
          //}
          PC += 2;
          break;





        /* 
         BPL - Branch on result plus                                  N Z C I D V
         Operation:  Branch on N = 0                                  _ _ _ _ _ _                     
         +----------------+-----------------------+---------+---------+----------+
         | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
         +----------------+-----------------------+---------+---------+----------+
         |  Relative      |   BPL Oper            |    10   |    2    |    2*    |
         +----------------+-----------------------+---------+---------+----------+
         * Add 1 if branch occurs to same page.                     (Ref: 4.1.1.2)
         * Add 2 if branch occurs to different page.
        */
        case 0x10:
          dbg[0x0] = "10 - BPL - Branch on result plus";
          dbg[0xF] = "BROKEN";
          //if (!IF_SIGN()) {
          //    clk += ((PC & 0xFF00) != (REL_ADDR(PC, src) & 0xFF00) ? 2 : 1);
          //    PC = REL_ADDR(PC, src);
          //}
          PC += 2;
          break;





        /*                   
         BRK - Force Break                                            N Z C I D V
         Operation:  Forced Interrupt PC + 2 toS P toS                _ _ _ 1 _ _              
         +----------------+-----------------------+---------+---------+----------+
         | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
         +----------------+-----------------------+---------+---------+----------+
         |  Implied       |   BRK                 |    00   |    1    |    7     |
         +----------------+-----------------------+---------+---------+----------+
         1. A BRK command cannot be masked by setting I.               (Ref: 9.11)
        */
        case 0x00:
          dbg[0] = "00 - BRK - Force Break ";
          dbg[0xF] = "BROKEN";

          //PC++;///????
          PUSH((byte)((PC >> 8) & 0xFF));//Push return address onto the stack
          PUSH((byte)(PC & 0xFF));
          SET_FLAG('b', 1);//Set Break Flag
          PUSH(S);//Push Status register onto stack
          SET_FLAG('i', 1);//Set Interrupt Flag
          PC = (UInt16)(CPU_Memory.Get_addr(0xFFFE) | (CPU_Memory.Get_addr(0xFFFF) << 8));
          
          break;



        /*   
          BVC - Branch on overflow clear                             N Z C I D V
          Operation:  Branch on V = 0                                  _ _ _ _ _ _                                        
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Relative      |   BVC Oper            |    50   |    2    |    2*    |
          +----------------+-----------------------+---------+---------+----------+
          * Add 1 if branch occurs to same page.                     (Ref: 4.1.1.8)
          * Add 2 if branch occurs to different page.
         */
        case 0x50:
          dbg[0x0] = "50 - BVC - Branch on overflow clear";
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;


        /*                   
         BVS - Branch on overflow set                                 N Z C I D V
         Operation:  Branch on V = 1                                  _ _ _ _ _ _                                                                                                                                       
         +----------------+-----------------------+---------+---------+----------+
         | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
         +----------------+-----------------------+---------+---------+----------+
         |  Relative      |   BVS Oper            |    70   |    2    |    2*    |
         +----------------+-----------------------+---------+---------+----------+
         * Add 1 if branch occurs to same page.                     (Ref: 4.1.1.7)
         * Add 2 if branch occurs to different page.
        */
        case 0x70:
          dbg[0x0] = "70 - BVS - Branch on overflow set ";
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;


        /*                
         CLC - Clear carry flag                                      N Z C I D V
         Operation:  0 -> C                                          _ _ 0 _ _ _                                         
        +----------------+-----------------------+---------+---------+----------+
        | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
        +----------------+-----------------------+---------+---------+----------+
        |  Implied       |   CLC                 |    18   |    1    |    2     |
        +----------------+-----------------------+---------+---------+----------+
        (Ref: 3.0.2)
       */
        case 0x18:
          dbg[0] = "18 - CLC - Clear carry flag ";
          dbg[0xF] = "OK";
          SET_FLAG('c', 0);
          PC += 2;
          break;


        /*
          CLD - Clear decimal mode                                       N A C I D V
          Operation:  0 -> D                                           _ _ _ _ 0 _                     
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   CLD                 |    D8   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+
                                                                       (Ref: 3.3.2)
         */
        case 0xD8:
          dbg[0x0] = "D8 - CLD - Clear decimal mode ";
          SET_FLAG('d', 0);
          dbg[0xF] = "OK";
          PC += 2;
          break;




        /*
          CLI - Clear interrupt disable bit                             N Z C I D V
          Operation: 0 -> I                                             _ _ _ 0 _ _                                                        
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   CLI                 |    58   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+
          (Ref: 3.2.2)
        */
        case 0x58:
          dbg[0x0] = "58 - CLI - Clear interrupt disable bit ";
          SET_FLAG('i', 0);
          dbg[0xF] = "OK";
          PC += 2;
          break;




        /*
          CLV - Clear overflow flag                                    N Z C I D V
          Operation: 0 -> V                                            _ _ _ _ _ 0                  
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   CLV                 |    B8   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+
          (Ref: 3.6.1)
         */
        case 0xB8:
          dbg[0x0] = "B8 - CLV - Clear overflow flag";
          dbg[0xF] = "ok";
          SET_FLAG('v', 0);
          PC += 2;
          break;



        /*
          CMP - Compare memory and accumulator                         N Z C I D V
          Operation:  A - M                                            / / / _ _ _          
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Immediate     |   CMP #Oper           |    C9   |    2    |    2     |
          |  Zero Page     |   CMP Oper            |    C5   |    2    |    3     |
          |  Zero Page,X   |   CMP Oper,X          |    D5   |    2    |    4     |
          |  Absolute      |   CMP Oper            |    CD   |    3    |    4     |
          |  Absolute,X    |   CMP Oper,X          |    DD   |    3    |    4*    |
          |  Absolute,Y    |   CMP Oper,Y          |    D9   |    3    |    4*    |
          |  (Indirect,X)  |   CMP (Oper,X)        |    C1   |    2    |    6     |
          |  (Indirect),Y  |   CMP (Oper),Y        |    D1   |    2    |    5*    |
          +----------------+-----------------------+---------+---------+----------+
          * Add 1 if page boundary is crossed.                         (Ref: 4.2.1)
         */
        case 0xC9:
        case 0xC5:
        case 0xD5:
        case 0xCD:
        case 0xDD:
        case 0xD9:
        case 0xC1:
        case 0xD1:
          switch (opcode & 0x00FF) {
            case 0xC9:
              dbg[0x0] = "C9 - CMP - Immediate ";
              break;
            case 0xC5:
              dbg[0x0] = "C5 - CMP - Zero Page";
              ZeroPage(addr);
              break;
            case 0xD5:
              dbg[0x0] = "D5 - CMP - Zero Page,X";
              break;
            case 0xCD:
              dbg[0x0] = "CD - CMP - Absolute";
              break;
            case 0xDD:
              dbg[0x0] = "DD - CMP - Absolute,X";
              break;
            case 0xD9:
              dbg[0x0] = "D9 - CMP - Absolute,Y";
              break;
            case 0xC1:
              dbg[0x0] = "C1 - CMP - (Indirect, X)";
              break;
            case 0xD1:
              dbg[0x0] = "D1 - CMP - (Indirect),Y";
              break;
          }
          dbg[0xF] = "BROKEN";
          CPU.src = (byte)(A - CPU.src);
          SET_CARRY(CPU.src < 0x100);
          SET_SIGN(CPU.src);
          SET_ZERO(CPU.src &= 0xff);
          PC += 2;
          break;



        /*
          CPX - Compare Memory and Index X                             N Z C I D V                                                
          Operation:  X - M                                            / / / _ _ _                                                  
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Immediate     |   CPX *Oper           |    E0   |    2    |    2     |
          |  Zero Page     |   CPX Oper            |    E4   |    2    |    3     |
          |  Absolute      |   CPX Oper            |    EC   |    3    |    4     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.8)
         */
        case 0xE0:
        case 0xE4:
        case 0xEC:
          switch (opcode & 0x00FF) {
            case 0xE0:
              dbg[0x0] = "E0 - CPX - Immediate";
              break;
            case 0xE4:
              dbg[0x0] = "E4 - CPX - Zero Page";
              break;
            case 0xEC:
              dbg[0x0] = "EC - CPX - Absolute";
              break;
          }
          dbg[0xF] = "BROKEN";
          PC += 2;

          break;





        /*
          CPY - Compare memory and index Y                             N Z C I D V                                                    
          Operation:  Y - M                                            / / / _ _ _                                                  
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Immediate     |   CPY *Oper           |    C0   |    2    |    2     |
          |  Zero Page     |   CPY Oper            |    C4   |    2    |    3     |
          |  Absolute      |   CPY Oper            |    CC   |    3    |    4     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.9)
         */
        case 0xC0:
        case 0xC4:
        case 0xCC:
          switch (opcode & 0x00FF) {
            case 0xC0:
              dbg[0x0] = "C0 - Cpy - Immediate ";
              break;
            case 0xC4:
              dbg[0x0] = "C4 - CPY - Zero Page";
              break;
            case 0xCC:
              dbg[0x0] = "CC - CPY - Absolute";
              break;
          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;




        /*
          DEC - Decrement memory by one                                N Z C I D V
          Operation:  M - 1 -> M                                       / / _ _ _ _ 
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Zero Page     |   DEC Oper            |    C6   |    2    |    5     |
          |  Zero Page,X   |   DEC Oper,X          |    D6   |    2    |    6     |
          |  Absolute      |   DEC Oper            |    CE   |    3    |    6     |
          |  Absolute,X    |   DEC Oper,X          |    DE   |    3    |    7     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 10.7)                 */
        case 0xC6:
        case 0xD6:
        case 0xCE:
        case 0xDE:
          switch (opcode & 0x00FF) {
            case 0xC6:
              dbg[0x0] = "C6 - DEC - Zero Page";
              break;
            case 0xD6:
              dbg[0x0] = "D6 - DEC - Zero Page,X";
              break;
            case 0xCE:
              dbg[0x0] = "CE - DEC - Absolute";
              break;
            case 0xDE:
              dbg[0x0] = "DE - DEC - Absolute,X";
              break;
          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;





        /*
          DEX - Decrement index X by one                                N Z C I D V
          Operation:  X - 1 -> X                                        / / _ _ _ _                                                    
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   DEX                 |    CA   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.6)
         */
        case 0xCA:
          dbg[0x0] = "CA - DEX - Decrement index X by one  ";
          dbg[0xF] = "ok?";
          src = X;
          src = (byte)((src - 1) & 0xFF); //seems pretty redundant
          SET_SIGN(src);
          SET_ZERO(src);
          X = (src);
          PC += 2;
          break;





        /*-------------------------------------------------------------------------
          DEY - Decrement index Y by one                                N Z C I D V
          Operation:  X - 1 -> Y                                        / / _ _ _ _                            
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   DEY                 |    88   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.7)
         */
        case 0x88:
          dbg[0x0] = "88 - DEY - Decrement index Y by one ";
          dbg[0xF] = "ok?";
          src = Y;
          src = (byte)((src - 1) & 0xFF); //seems pretty redundant
          SET_SIGN(src);
          SET_ZERO(src);
          Y = (src);
          PC += 2;
          break;






        /*
          EOR - "Exclusive-Or" memory with accumulator                  N Z C I D V
          Operation:  A EOR M -> A                                      / / _ _ _ _    
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Immediate     |   EOR #Oper           |    49   |    2    |    2     |
          |  Zero Page     |   EOR Oper            |    45   |    2    |    3     |
          |  Zero Page,X   |   EOR Oper,X          |    55   |    2    |    4     |
          |  Absolute      |   EOR Oper            |    4D   |    3    |    4     |
          |  Absolute,X    |   EOR Oper,X          |    5D   |    3    |    4*    |
          |  Absolute,Y    |   EOR Oper,Y          |    59   |    3    |    4*    |
          |  (Indirect,X)  |   EOR (Oper,X)        |    41   |    2    |    6     |
          |  (Indirect),Y  |   EOR (Oper),Y        |    51   |    2    |    5*    |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 2.2.3.2)
          * Add 1 if page boundary is crossed.
         */
        case 0x49:
        case 0x45:
        case 0x55:
        case 0x4D:
        case 0x5D:
        case 0x59:
        case 0x41:
        case 0x51:
          switch (opcode & 0x00FF) {
            case 0x49:
              dbg[0x0] = "49 - EOR - Immediate";
              dbg[0xF] = "BROKEN";
              break;
            case 0x45:
              dbg[0x0] = "45 - EOR - Zero Page";
              dbg[0xF] = "BROKEN";
              src = (byte)(addr & 0xFF);
              dbg[0x1] = "src: " + src.ToString("X");
              break;
            case 0x55:
              dbg[0x0] = "55 - EOR - Zero Page,X";
              dbg[0xF] = "BROKEN";
              break;
            case 0x4D:
              dbg[0x0] = "4D - EOR - Absolute";
              dbg[0xF] = "BROKEN";
              break;
            case 0x5D:
              dbg[0x0] = "5D - EOR - Absolute,X";
              dbg[0xF] = "BROKEN";
              break;
            case 0x59:
              dbg[0x0] = "59 - EOR - Absolute,Y";
              dbg[0xF] = "BROKEN";
              break;
            case 0x41:
              dbg[0x0] = "41 - EOR - (Indirect,X)";
              dbg[0xF] = "BROKEN";
              break;
            case 0x51:
              dbg[0x0] = "51 - EOR - (Indirect),Y";
              dbg[0xF] = "BROKEN";
              break;
          }
          
          src ^= A;
          SET_SIGN(src);
          SET_ZERO(src);
          A = src;
          PC += 2;
          break;





        /*
          INC - Increment memory by one                                N Z C I D V                                                                         
          Operation:  M + 1 -> M                                       / / _ _ _ _                                                  
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Zero Page     |   INC Oper            |    E6   |    2    |    5     |
          |  Zero Page,X   |   INC Oper,X          |    F6   |    2    |    6     |
          |  Absolute      |   INC Oper            |    EE   |    3    |    6     |
          |  Absolute,X    |   INC Oper,X          |    FE   |    3    |    7     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 10.6)
         */
        case 0xE6:
        case 0xF6:
        case 0xEE:
        case 0xFE:
          switch (opcode & 0x00FF) {
            case 0xE6:
              dbg[0x0] = "E6 - INC - Zero Page";
              break;
            case 0xF6:
              dbg[0x0] = "F6 - INC - Zero Page,X";
              break;
            case 0xEE:
              dbg[0x0] = "EE - INC - Absolute";
              break;
            case 0xFE:
              dbg[0x0] = "FE - INC - Absolute,X";
              break;
          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;




        /*
          INX - Increment Index X by one                               N Z C I D V                                                  
          Operation:  X + 1 -> X                                       / / _ _ _ _                                                   
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   INX                 |    E8   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.4)
         */
        case 0xE8:
          dbg[0x0] = "E8 - INX - Increment Index X by one ";
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;





        /*
          INY - Increment Index Y by one                               N Z C I D V
          Operation:  X + 1 -> X                                       / / _ _ _ _       
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   INY                 |    C8   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.5)
         */
        case 0xC8:
          dbg[0x0] = "C8 - INY - Increment Index Y by one ";
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;





        /*
          JMP Jump to new location                                     N Z C I D V
          Operation:  (PC + 1) -> PCL                                  _ _ _ _ _ _               
                      (PC + 2) -> PCH                                         
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Absolute      |   JMP Oper            |    4C   |    3    |    3     |
          |  Indirect      |   JMP (Oper)          |    6C   |    3    |    5     | (Ref: 4.0.2) 
          +----------------+-----------------------+---------+---------+----------+ (Ref: 9.8.1)
         */
        case 0x4C:
        case 0x6C:
          switch (opcode & 0x00FF) {
            case 0x4C:
              dbg[0x0] = "4C - JMP - Absolute";
              PC = (UInt16)(((LOAD_MEM(addr) & 0xFF) << 8) | ((LOAD_MEM(addr) & 0xFF00)>>8));
              break;
            case 0x6C:
              dbg[0x0] = "6C - JMP - Indirect";
              PC = (UInt16)(((addr & 0xFF) << 8) | ((addr & 0xFF00) >> 8));
              break;
          }
          dbg[0xF] = "BROKEN";
          break;





        /*
          JSR - Jump to new location saving return address               N Z C I D V
          Operation:  PC + 2 toS, (PC + 1) -> PCL                      _ _ _ _ _ _
                                  (PC + 2) -> PCH                                     
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Absolute      |   JSR Oper            |    20   |    3    |    6     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 8.1)
         */
        case 0x20:
          dbg[0x0] = "20 - JSR - Jump to new location saving return address";
          dbg[0xF] = "BROKEN";
          break;











        /*
          LDA Load accumulator with memory                             N Z C I D V
          Operation:  M -> A                                           / / _ _ _ _                                              
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Immediate     |   LDA #Oper           |    A9   |    2    |    2     |
          |  Zero Page     |   LDA Oper            |    A5   |    2    |    3     |
          |  Zero Page,X   |   LDA Oper,X          |    B5   |    2    |    4     |
          |  Absolute      |   LDA Oper            |    AD   |    3    |    4     |
          |  Absolute,X    |   LDA Oper,X          |    BD   |    3    |    4*    |
          |  Absolute,Y    |   LDA Oper,Y          |    B9   |    3    |    4*    |
          |  (Indirect,X)  |   LDA (Oper,X)        |    A1   |    2    |    6     |
          |  (Indirect),Y  |   LDA (Oper),Y        |    B1   |    2    |    5*    |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 2.1.1)
          * Add 1 if page boundary is crossed.
         */
        case 0xA9:
        case 0xA5:
        case 0xB5:
        case 0xAD:
        case 0xBD:
        case 0xB9:
        case 0xA1:
        case 0xB1:
          switch (opcode & 0x00FF) {
            case 0xA9:
              dbg[0x0] = "A9 - LDA - Immediate";
              break;
            case 0xA5:
              dbg[0x0] = "A5 - LDA - Zero Page";
              break;
            case 0xB5:
              dbg[0x0] = "B5 - LDA - Zero Page,X";
              break;
            case 0xAD:
              dbg[0x0] = "AD - LDA - Absolute";
              break;
            case 0xBD:
              dbg[0x0] = "BD - LDA - Absolute,X";
              break;
            case 0xB9:
              dbg[0x0] = "B9 - LDA - Absolute,Y";
              break;
            case 0xA1:
              dbg[0x0] = "A1 - LDA - (Indirect,X)";
              break;
            case 0xB1:
              dbg[0x0] = "B1 - LDA - (Indirect),Y";
              break;
          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;





        /*
          LDX - Load index X with memory                               N Z C I D V
          Operation:  M -> X                                           / / _ _ _ _                                                  
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Immediate     |   LDX #Oper           |    A2   |    2    |    2     |
          |  Zero Page     |   LDX Oper            |    A6   |    2    |    3     |
          |  Zero Page,Y   |   LDX Oper,Y          |    B6   |    2    |    4     |
          |  Absolute      |   LDX Oper            |    AE   |    3    |    4     |
          |  Absolute,Y    |   LDX Oper,Y          |    BE   |    3    |    4*    |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.0)
          * Add 1 when page boundary is crossed.
         */
        case 0xA2:
        case 0xA6:
        case 0xB6:
        case 0xAE:
        case 0xBE:
          switch (opcode & 0x00FF) {
            case 0xA2:
              dbg[0x0] = "A2 - LDX - Immediate";
              break;
            case 0xA6:
              dbg[0x0] = "A6 - LDX - Zero Page";
              break;
            case 0xB6:
              dbg[0x0] = "B6 - LDX - Zero Page,Y";
              break;
            case 0xAE:
              dbg[0x0] = "AE - LDX - Absolute";
              break;
            case 0xBE:
              dbg[0x0] = "BE - LDX - Absolute,Y";
              break;

          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;




        /*
          LDY LDY Load index Y with memory                             N Z C I D V                                                                            
          Operation:  M -> Y                                           / / _ _ _ _                                                   
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Immediate     |   LDY #Oper           |    A0   |    2    |    2     |
          |  Zero Page     |   LDY Oper            |    A4   |    2    |    3     |
          |  Zero Page,X   |   LDY Oper,X          |    B4   |    2    |    4     |
          |  Absolute      |   LDY Oper            |    AC   |    3    |    4     |
          |  Absolute,X    |   LDY Oper,X          |    BC   |    3    |    4*    | 
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.1)
          * Add 1 when page boundary is crossed.
         */
        case 0xA0:
        case 0xA4:
        case 0xB4:
        case 0xAC:
        case 0xBC:
          switch (opcode & 0x00FF) {
            case 0xA0:
              dbg[0x0] = "A0 - LDY - Immediate";
              break;
            case 0xA4:
              dbg[0x0] = "A4 - LDY - Zero Page";
              break;
            case 0xB4:
              dbg[0x0] = "B4 - LDY - Zero Page,X";
              break;
            case 0xAC:
              dbg[0x0] = "AC - LDY - Absolute";
              break;
            case 0xBC:
              dbg[0x0] = "BC - LDY - Absolute,X";
              break;

          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;








        /*
          LSR - Shift right one bit (memory or accumulator)          N Z C I D V
          Operation:  0 -> |7|6|5|4|3|2|1|0| -> C                    0 / / _ _ _                                    
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Accumulator   |   LSR A               |    4A   |    1    |    2     |
          |  Zero Page     |   LSR Oper            |    46   |    2    |    5     |
          |  Zero Page,X   |   LSR Oper,X          |    56   |    2    |    6     |
          |  Absolute      |   LSR Oper            |    4E   |    3    |    6     |
          |  Absolute,X    |   LSR Oper,X          |    5E   |    3    |    7     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 10.1)
         */
        case 0x4A:
        case 0x46:
        case 0x56:
        case 0x4E:
        case 0x5E:
          switch (opcode & 0x00FF) {
            case 0x4A:
              dbg[0x0] = "4A - LSR - Accumulator";
              break;
            case 0x46:
              dbg[0x0] = "46 - LSR - Zero Page";
              break;
            case 0x56:
              dbg[0x0] = "56 - LSR - Zero Page,X";
              break;
            case 0x4E:
              dbg[0x0] = "4E - LSR - Absolute";
              break;
            case 0x5E:
              dbg[0x0] = "5E - LSR - Absolute,X";
              break;

          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;








        /*
          NOP - No operation                                            N Z C I D V
          Operation:  No Operation (2 cycles)                           _ _ _ _ _ _
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   NOP                 |    EA   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+
         */
        case 0xEA:
          dbg[0x0] = "EA - NOP - No operation ";
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;







        /*
          ORA - "OR" memory with accumulator                         N Z C I D V
          Operation: A V M -> A                                        / / _ _ _ _                          
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Immediate     |   ORA #Oper           |    09   |    2    |    2     |
          |  Zero Page     |   ORA Oper            |    05   |    2    |    3     |
          |  Zero Page,X   |   ORA Oper,X          |    15   |    2    |    4     |
          |  Absolute      |   ORA Oper            |    0D   |    3    |    4     |
          |  Absolute,X    |   ORA Oper,X          |    1D   |    3    |    4*    |
          |  Absolute,Y    |   ORA Oper,Y          |    19   |    3    |    4*    |
          |  (Indirect,X)  |   ORA (Oper,X)        |    01   |    2    |    6     |
          |  (Indirect),Y  |   ORA (Oper),Y        |    11   |    2    |    5     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 2.2.3.1)
          * Add 1 on page crossing
         */
        case 0x09:
        case 0x05:
        case 0x15:
        case 0x0D:
        case 0x1D:
        case 0x19:
        case 0x01:
        case 0x11:
          switch (opcode & 0x00FF) {
            case 0x09:
              dbg[0x0] = "09 - ORA - Immediate";
              break;
            case 0x05:
              dbg[0x0] = "05 - ORA - Zero Page";
              break;
            case 0x15:
              dbg[0x0] = "15 - ORA - Zero Page,X";
              break;
            case 0x0D:
              dbg[0x0] = "0D - ORA - Absolute";
              break;
            case 0x1D:
              dbg[0x0] = "1D - ORA - Absolute,X";
              break;
            case 0x19:
              dbg[0x0] = "19 - ORA - Absolute,Y";
              break;
            case 0x01:
              dbg[0x0] = "01 - ORA - (Indirect,X)";
              break;
            case 0x11:
              dbg[0x0] = "11 - ORA - (Indirect),Y";
              break;
          }
          dbg[0xF] = "BROKEN";
          src = X;
          src |= A;
          SET_FLAG('n', src);
          SET_FLAG('z', src);
          A = src;
          PC += 2;
          break;











        /*
          PHA - Push accumulator on stack                               N Z C I D V
          Operation:  A to S                                             _ _ _ _ _ _       
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   PHA                 |    48   |    1    |    3     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 8.5)
         */
        case 0x48:
          dbg[0x0] = "48 - PHA - Push accumulator on stack";
          dbg[0xF] = "---OK---";
          PUSH(A);
          PC += 2;
          break;






        /*
          PHP - Push processor status on stack                          N Z C I D V
          Operation:  P toS                                             _ _ _ _ _ _
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   PHP                 |    08   |    1    |    3     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 8.11)
         */
        case 0x08:
          dbg[0x0] = "08 - PHP - Push processor status on stack";
          dbg[0xF] = "---OK---";
          PUSH(S);
          PC += 2;
          break;







        /*
          PLA - Pull accumulator from stack                             N Z C I D V
          Operation:  A from S                                           _ _ _ _ _ _ 
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   PLA                 |    68   |    1    |    4     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 8.6)
         */
        case 0x68:
          dbg[0x0] = "68 - PLA - Pull accumulator from stack ";
          dbg[0xF] = "---OK---";
          A = PULL();
          PC += 2;
          break;




        /*
          PLP - Pull processor status from stack                       N Z C I D V
          Operation:  P fromS                                          From Stack                                                                             
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   PLP                 |    28   |    1    |    4     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 8.12)
         */
        case 0x28:
          dbg[0x0] = "28 - PLP - Pull processor status from stack";
          dbg[0xF] = "---OK---";
          S = PULL();
          PC += 2;
          break;





        /*
          ROL - Rotate one bit left (memory or accumulator)         N Z C I D V
                       +------------------------------+             / / / _ _ _
                       |         M or A               |
                       |   +-+-+-+-+-+-+-+-+    +-+   |
          Operation:   +-< |7|6|5|4|3|2|1|0| <- |C| <-+         
                           +-+-+-+-+-+-+-+-+    +-+                                                                 
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Accumulator   |   ROL A               |    2A   |    1    |    2     |
          |  Zero Page     |   ROL Oper            |    26   |    2    |    5     |
          |  Zero Page,X   |   ROL Oper,X          |    36   |    2    |    6     |
          |  Absolute      |   ROL Oper            |    2E   |    3    |    6     |
          |  Absolute,X    |   ROL Oper,X          |    3E   |    3    |    7     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 10.3)
         */
        case 0x2A:
        case 0x26:
        case 0x36:
        case 0x2E:
        case 0x3E:
          switch (opcode & 0x00FF) {
            case 0x2A:
              dbg[0x0] = "2A - ROL - Accumulator";
              break;
            case 0x26:
              dbg[0x0] = "26 - ROL - Zero Page";
              break;
            case 0x36:
              dbg[0x0] = "36 - ROL - Zero Page,X";
              break;
            case 0x2E:
              dbg[0x0] = "2E - ROL - Absolute";
              break;
            case 0x3E:
              dbg[0x0] = "3E - ROL - Absolute,X";
              break;
          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;





        /*
         ROR - Rotate one bit right (memory or accumulator)        
                       +------------------------------+
                       |                              |
                       |   +-+    +-+-+-+-+-+-+-+-+   |
          Operation:   +-> |C| -> |7|6|5|4|3|2|1|0| >-+         N Z C I D V
                           +-+    +-+-+-+-+-+-+-+-+             / / / _ _ _
                                         (Ref: 10.4)
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Accumulator   |   ROR A               |    6A   |    1    |    2     |
          |  Zero Page     |   ROR Oper            |    66   |    2    |    5     |
          |  Zero Page,X   |   ROR Oper,X          |    76   |    2    |    6     |
          |  Absolute      |   ROR Oper            |    6E   |    3    |    6     |
          |  Absolute,X    |   ROR Oper,X          |    7E   |    3    |    7     |
          +----------------+-----------------------+---------+---------+----------+
          Note: ROR instruction is available on MCS650X microprocessors after June, 1976.
         */
        case 0x6A:
        case 0x66:
        case 0x76:
        case 0x6E:
        case 0x7E:
          switch (opcode & 0x00FF) {
            case 0x6A:
              dbg[0x0] = "6A - ROR - Accumulator";
              break;
            case 0x66:
              dbg[0x0] = "66 - ROR - Zero Page";
              break;
            case 0x76:
              dbg[0x0] = "76 - ROR - Zero Page,X";
              break;
            case 0x6E:
              dbg[0x0] = "6E - ROR - Absolute";
              break;
            case 0x7E:
              dbg[0x0] = "7E - ROR - Absolute,X";
              break;
          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;







        /*
          RTI - Return from interrupt                                  N Z C I D V
          Operation:  P fromS PC fromS                                 From StacK                                                  
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   RTI                 |    40   |    1    |    6     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 9.6)
         */
        case 0x40:
          dbg[0x0] = "40 - RTI - Return from interrupt";
          dbg[0xF] = "...BROKEN";
          src = PULL();
          S = src;//SET_SR(src);
          src = PULL();
          src |= (byte)(PULL() << 8);   /* Load return address from stack. */
          PC = (src);
          break;





        /*
          RTS - Return from subroutine                                 N Z C I D V
          Operation:  PC from S, PC + 1 -> PC                           _ _ _ _ _ _     
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   RTS                 |    60   |    1    |    6     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 8.2)
         */
        case 0x60:
          dbg[0x0] = "60 - RTS - Return from subroutine";
          dbg[0xF] = "ok? idk i dont think its ok";
          src = PULL();//src is only a byte.... FUCK
          src += (byte)((PULL() << 8) + 1); /* Load return address from stack and add 1. */
          PC = (src);
          break;





        /*
          SBC - Subtract memory from accumulator with borrow            N Z C I D V
          Operation:  A - M - C -> A      Note:C = Borrow               / / / _ _ /      
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Immediate     |   SBC #Oper           |    E9   |    2    |    2     |
          |  Zero Page     |   SBC Oper            |    E5   |    2    |    3     |
          |  Zero Page,X   |   SBC Oper,X          |    F5   |    2    |    4     |
          |  Absolute      |   SBC Oper            |    ED   |    3    |    4     |
          |  Absolute,X    |   SBC Oper,X          |    FD   |    3    |    4*    |
          |  Absolute,Y    |   SBC Oper,Y          |    F9   |    3    |    4*    |
          |  (Indirect,X)  |   SBC (Oper,X)        |    E1   |    2    |    6     |
          |  (Indirect),Y  |   SBC (Oper),Y        |    F1   |    2    |    5     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 2.2.2)
          * Add 1 when page boundary is crossed.
         */
        case 0xE9:
        case 0xE5:
        case 0xF5:
        case 0xED:
        case 0xFD:
        case 0xF9:
        case 0xE1:
        case 0xF1:
          switch (opcode & 0x00FF) {
            case 0xE9:
              dbg[0x0] = "E9 - SBC - Immediate";
              break;
            case 0xE5:
              dbg[0x0] = "E5 - SBC - Zero Page";
              break;
            case 0xF5:
              dbg[0x0] = "F5 - SBC - Zero Page,X";
              break;
            case 0xED:
              dbg[0x0] = "ED - SBC - Absolute";
              break;
            case 0xFD:
              dbg[0x0] = "FD - SBC - Absolute,X";
              break;
            case 0xF9:
              dbg[0x0] = "F9 - SBC - Absolute,Y";
              break;
            case 0xE1:
              dbg[0x0] = "E1 - SBC - (Indirect,X)";
              break;
            case 0xF1:
              dbg[0x0] = "F1 - SBC - (Indirect),Y";
              break;
          }
          dbg[0xF] = "BROKEN";
          PC += 2;
          break;





        /*
          SEC - Set carry flag                                          N Z C I D V
          Operation:  1 -> C                                            _ _ 1 _ _ _      
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   SEC                 |    38   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 3.0.1)
         */
        case 0x38:
          dbg[0x0] = "38 - SEC - Set carry flag ";
          dbg[0xF] = "---OK---";
          SET_FLAG('c', 1);
          PC += 2;
          break;



        /*
          SED - Set decimal mode                                        N Z C I D V
          Operation:  1 -> D                                            _ _ _ _ 1 _
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   SED                 |    F8   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 3.3.1)
         */
        case 0xF8:
          dbg[0x0] = "F8 - SED - Set decimal mode";
          dbg[0xF] = "---OK---";
          SET_FLAG('d', 1);
          PC += 2;
          break;


        /*
          SEI - Set interrupt disable status                           N Z C I D V                                                                     
          Operation:  1 -> I                                           _ _ _ 1 _ _
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   SEI                 |    78   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 3.2.1)
         */
        case 0x78:
          dbg[0x0] = "78 - SEI - Set interrupt disable status ";
          dbg[0xF] = "---OK---";
          SET_FLAG('i', 1);
          PC += 2;
          break;






        /*
          STA - Store accumulator in memory                            N Z C I D V
          Operation:  A -> M                                           _ _ _ _ _ _                                                  
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Zero Page     |   STA Oper            |    85   |    2    |    3     |
          |  Zero Page,X   |   STA Oper,X          |    95   |    2    |    4     |
          |  Absolute      |   STA Oper            |    8D   |    3    |    4     |
          |  Absolute,X    |   STA Oper,X          |    9D   |    3    |    5     |
          |  Absolute,Y    |   STA Oper, Y         |    99   |    3    |    5     |
          |  (Indirect,X)  |   STA (Oper,X)        |    81   |    2    |    6     |
          |  (Indirect),Y  |   STA (Oper),Y        |    91   |    2    |    6     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 2.1.2)
         */
        case 0x85:
        case 0x95:
        case 0x8D:
        case 0x9D:
        case 0x99:
        case 0x81:
        case 0x91:

          switch (opcode & 0x00FF) {
            case 0x85:
              dbg[0x0] = "85 - STA - Zero Page";
              dbg[0xF] = "BROKEN";
              break;
            case 0x95:
              dbg[0x0] = "95 - STA - Zero Page,X";
              dbg[0xF] = "BROKEN";
              break;
            case 0x8D:
              dbg[0x0] = "8D - STA - Absolute";
              dbg[0xF] = "BROKEN";
              break;
            case 0x9D:
              dbg[0x0] = "9D - STA - Absolute,X";
              dbg[0xF] = "???";
              src = A;
              addr += X;// given $address (absolute) + X index
              break;
            case 0x99:
              dbg[0x0] = "99 - STA - Absolute,Y";
              dbg[0xF] = "???";
              src = A;
              addr += Y;// given $address (absolute) + X index
              break;
            case 0x81:
              dbg[0x0] = "81 - STA - (Indirect,X)";
              dbg[0xF] = "BROKEN";
              break;
            case 0x91:
              dbg[0x0] = "91 - STA - (Indirect),Y";
              dbg[0xF] = "BROKEN";
              break;
          }

          STORE(addr, (src));
          PC += 2;
          break;




        /*
          STX - Store index X in memory                                N Z C I D V
          Operation: X -> M                                            _ _ _ _ _ _
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Zero Page     |   STX Oper            |    86   |    2    |    3     |
          |  Zero Page,Y   |   STX Oper,Y          |    96   |    2    |    4     |
          |  Absolute      |   STX Oper            |    8E   |    3    |    4     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.2)
         */
        case 0x86:
        case 0x96:
        case 0x8E:

          switch (opcode & 0x00FF) {
            case 0x86:
              dbg[0x0] = "86 - STX - Zero Page";
              break;
            case 0x96:
              dbg[0x0] = "96 - STX - Zero Page,Y";
              break;
            case 0x8E:
              dbg[0x0] = "8E - STX - Absolute";
              dbg[0x1] = "Store X("+X.ToString("X")+") Into mem addr:" + addr.ToString("X");
              dbg[0x2] = "8E - STX - Absolute";
              src = X;
              break;
          }
          dbg[0xF] = "BROKEN";
          STORE(addr, (src));
          PC += 2;
          break;



        /*
          STY - Store index Y in memory                                N Z C I D V
          Operation: Y -> M                                            _ _ _ _ _ _                 
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Zero Page     |   STY Oper            |    84   |    2    |    3     |
          |  Zero Page,X   |   STY Oper,X          |    94   |    2    |    4     |
          |  Absolute      |   STY Oper            |    8C   |    3    |    4     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.3)
         */
        case 0x84:
        case 0x94:
        case 0x8C:
          switch (opcode & 0x00FF) {
            case 0x84:
              dbg[0x0] = "84 - STY - Zero Page";
              break;
            case 0x94:
              dbg[0x0] = "94 - STY - Zero Page,X";
              break;
            case 0x8C:
              dbg[0x0] = "8C - STY - Absolute";
              break;

          }
          dbg[0xF] = "BROKEN";
          STORE(addr, (src));
          PC += 2;
          break;






        /*
          TAX - Transfer accumulator to index X                        N Z C I D V
          Operation:  A -> X                                           / / _ _ _ _
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   TAX                 |    AA   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.11)
         */
        case 0xAA:
          dbg[0x0] = "AA - TAX - Transfer accumulator to index X";
          dbg[0xF] = "ok";
          src = A;
          SET_SIGN(src);
          SET_ZERO(src);
          X = (src);
          PC += 2;
          break;







        /*
          TAY - Transfer accumulator to index Y                        N Z C I D V
          Operation:  A -> Y                                           / / _ _ _ _
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   TAY                 |    A8   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.13)
         */
        case 0xA8:
          dbg[0x0] = "A8 - TAY - Transfer accumulator to index Y";
          dbg[0xF] = "ok";
          src = A;
          SET_SIGN(src);
          SET_ZERO(src);
          Y = (src);
          PC += 2;
          break;



        /*
          TSX - Transfer stack pointer to index X                       N Z C I D V
          Operation:  S -> X                                            / / _ _ _ _                                                                                                                             
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   TSX                 |    BA   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 8.9)
         */
        case 0xBA:
          dbg[0x0] = "BA - TSX - Transfer stack pointer to index X";
          dbg[0xF] = "ok";
          src = SP;
          SET_SIGN(src);
          SET_ZERO(src);
          X = (src);
          PC += 2;
          break;



        /*
          TXA - Transfer index X to accumulator                         N Z C I D V
          Operation:  X -> A                                            / / _ _ _ _                     
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   TXA                 |    8A   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.12)
         */
        case 0x8A:
          dbg[0x0] = "8A - TXA - Transfer index X to accumulator ";
          dbg[0xF] = "ok";
          src = X;
          SET_SIGN(src);
          SET_ZERO(src);
          A = (src);
          PC += 2;
          break;



        /*
          TXS - Transfer index X to stack pointer                       N Z C I D V
          Operation:  X -> S                                            _ _ _ _ _ _                             
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   TXS                 |    9A   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 8.8)                   
         */
        case 0x9A:
          dbg[0x0] = "9A - TXS - Transfer index X to stack pointer";
          dbg[0xF] = "ok";
          //src = X;
          SP = X;
          PC += 2;
          break;



        /*
          TYA - Transfer index Y to accumulator                        N Z C I D V
          Operation:  Y -> A                                           / / _ _ _ _
          +----------------+-----------------------+---------+---------+----------+
          | Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles|
          +----------------+-----------------------+---------+---------+----------+
          |  Implied       |   TYA                 |    98   |    1    |    2     |
          +----------------+-----------------------+---------+---------+----------+ (Ref: 7.14)
         */
        case 0x98:
          dbg[0x0] = "98 - TYA - Transfer index Y to accumulator";
          dbg[0xF] = "ok";
          src = Y;
          SET_SIGN(src);
          SET_ZERO(src);
          A = (src);
          PC += 2;
          break;












        case 0x02: PC += 2; dbg[0] = "02 - Future Expansion"; break;
        case 0x03: PC += 2; dbg[0] = "03 - Future Expansion"; break;
        case 0x04: PC += 2; dbg[0] = "04 - Future Expansion"; break;
        case 0x07: PC += 2; dbg[0] = "07 - Future Expansion"; break;
        case 0x0B: PC += 2; dbg[0] = "0B - Future Expansion"; break;
        case 0x0C: PC += 2; dbg[0] = "0C - Future Expansion"; break;
        case 0x0F: PC += 2; dbg[0] = "0F - Future Expansion"; break;
        case 0x12: PC += 2; dbg[0] = "12 - Future Expansion"; break;
        case 0x13: PC += 2; dbg[0] = "13 - Future Expansion"; break;
        case 0x14: PC += 2; dbg[0] = "14 - Future Expansion"; break;
        case 0x17: PC += 2; dbg[0] = "17 - Future Expansion"; break;
        case 0x1A: PC += 2; dbg[0] = "1A - Future Expansion"; break;
        case 0x1B: PC += 2; dbg[0] = "1B - Future Expansion"; break;
        case 0x1C: PC += 2; dbg[0] = "1C - Future Expansion"; break;
        case 0x1F: PC += 2; dbg[0] = "1F - Future Expansion"; break;
        case 0x22: PC += 2; dbg[0] = "22 - Future Expansion"; break;
        case 0x23: PC += 2; dbg[0] = "23 - Future Expansion"; break;
        case 0x27: PC += 2; dbg[0] = "27 - Future Expansion"; break;
        case 0x2B: PC += 2; dbg[0] = "2B - Future Expansion"; break;
        case 0x2F: PC += 2; dbg[0] = "2F - Future Expansion"; break;
        case 0x32: PC += 2; dbg[0] = "32 - Future Expansion"; break;
        case 0x33: PC += 2; dbg[0] = "33 - Future Expansion"; break;
        case 0x34: PC += 2; dbg[0] = "34 - Future Expansion"; break;
        case 0x37: PC += 2; dbg[0] = "37 - Future Expansion"; break;
        case 0x3A: PC += 2; dbg[0] = "3A - Future Expansion"; break;
        case 0x3B: PC += 2; dbg[0] = "3B - Future Expansion"; break;
        case 0x3C: PC += 2; dbg[0] = "3C - Future Expansion"; break;
        case 0x3F: PC += 2; dbg[0] = "3F - Future Expansion"; break;
        case 0x42: PC += 2; dbg[0] = "42 - Future Expansion"; break;
        case 0x43: PC += 2; dbg[0] = "43 - Future Expansion"; break;
        case 0x44: PC += 2; dbg[0] = "44 - Future Expansion"; break;
        case 0x47: PC += 2; dbg[0] = "47 - Future Expansion"; break;
        case 0x4B: PC += 2; dbg[0] = "4B - Future Expansion"; break;
        case 0x4F: PC += 2; dbg[0] = "4F - Future Expansion"; break;
        case 0x52: PC += 2; dbg[0] = "52 - Future Expansion"; break;
        case 0x53: PC += 2; dbg[0] = "53 - Future Expansion"; break;
        case 0x54: PC += 2; dbg[0] = "54 - Future Expansion"; break;
        case 0x57: PC += 2; dbg[0] = "57 - Future Expansion"; break;
        case 0x5A: PC += 2; dbg[0] = "5A - Future Expansion"; break;
        case 0x5B: PC += 2; dbg[0] = "5B - Future Expansion"; break;
        case 0x5C: PC += 2; dbg[0] = "5C - Future Expansion"; break;
        case 0x5F: PC += 2; dbg[0] = "5F - Future Expansion"; break;
        case 0x62: PC += 2; dbg[0] = "62 - Future Expansion"; break;
        case 0x63: PC += 2; dbg[0] = "63 - Future Expansion"; break;
        case 0x64: PC += 2; dbg[0] = "64 - Future Expansion"; break;
        case 0x67: PC += 2; dbg[0] = "67 - Future Expansion"; break;
        case 0x6B: PC += 2; dbg[0] = "6B - Future Expansion"; break;
        case 0x6F: PC += 2; dbg[0] = "6F - Future Expansion"; break;
        case 0x72: PC += 2; dbg[0] = "72 - Future Expansion"; break;
        case 0x73: PC += 2; dbg[0] = "73 - Future Expansion"; break;
        case 0x74: PC += 2; dbg[0] = "74 - Future Expansion"; break;
        case 0x77: PC += 2; dbg[0] = "77 - Future Expansion"; break;
        case 0x7A: PC += 2; dbg[0] = "7A - Future Expansion"; break;
        case 0x7B: PC += 2; dbg[0] = "7B - Future Expansion"; break;
        case 0x7C: PC += 2; dbg[0] = "7C - Future Expansion"; break;
        case 0x7F: PC += 2; dbg[0] = "7F - Future Expansion"; break;
        case 0x80: PC += 2; dbg[0] = "80 - Future Expansion"; break;
        case 0x82: PC += 2; dbg[0] = "82 - Future Expansion"; break;
        case 0x83: PC += 2; dbg[0] = "83 - Future Expansion"; break;
        case 0x87: PC += 2; dbg[0] = "87 - Future Expansion"; break;
        case 0x89: PC += 2; dbg[0] = "89 - Future Expansion"; break;
        case 0x8B: PC += 2; dbg[0] = "8B - Future Expansion"; break;
        case 0x8F: PC += 2; dbg[0] = "8F - Future Expansion"; break;
        case 0x92: PC += 2; dbg[0] = "92 - Future Expansion"; break;
        case 0x93: PC += 2; dbg[0] = "93 - Future Expansion"; break;
        case 0x97: PC += 2; dbg[0] = "97 - Future Expansion"; break;
        case 0x9B: PC += 2; dbg[0] = "9B - Future Expansion"; break;
        case 0x9C: PC += 2; dbg[0] = "9C - Future Expansion"; break;
        case 0x9E: PC += 2; dbg[0] = "9E - Future Expansion"; break;
        case 0x9F: PC += 2; dbg[0] = "9F - Future Expansion"; break;
        case 0xA3: PC += 2; dbg[0] = "A3 - Future Expansion"; break;
        case 0xA7: PC += 2; dbg[0] = "A7 - Future Expansion"; break;
        case 0xAB: PC += 2; dbg[0] = "AB - Future Expansion"; break;
        case 0xAF: PC += 2; dbg[0] = "AF - Future Expansion"; break;
        case 0xB2: PC += 2; dbg[0] = "B2 - Future Expansion"; break;
        case 0xB3: PC += 2; dbg[0] = "B3 - Future Expansion"; break;
        case 0xB7: PC += 2; dbg[0] = "B7 - Future Expansion"; break;
        case 0xBB: PC += 2; dbg[0] = "BB - Future Expansion"; break;
        case 0xBF: PC += 2; dbg[0] = "BF - Future Expansion"; break;
        case 0xC2: PC += 2; dbg[0] = "C2 - Future Expansion"; break;
        case 0xC3: PC += 2; dbg[0] = "C3 - Future Expansion"; break;
        case 0xC7: PC += 2; dbg[0] = "C7 - Future Expansion"; break;
        case 0xCB: PC += 2; dbg[0] = "CB - Future Expansion"; break;
        case 0xCF: PC += 2; dbg[0] = "CF - Future Expansion"; break;
        case 0xD2: PC += 2; dbg[0] = "D2 - Future Expansion"; break;
        case 0xD3: PC += 2; dbg[0] = "D3 - Future Expansion"; break;
        case 0xD4: PC += 2; dbg[0] = "D4 - Future Expansion"; break;
        case 0xD7: PC += 2; dbg[0] = "D7 - Future Expansion"; break;
        case 0xDA: PC += 2; dbg[0] = "DA - Future Expansion"; break;
        case 0xDB: PC += 2; dbg[0] = "DB - Future Expansion"; break;
        case 0xDC: PC += 2; dbg[0] = "DC - Future Expansion"; break;
        case 0xDF: PC += 2; dbg[0] = "DF - Future Expansion"; break;
        case 0xE2: PC += 2; dbg[0] = "E2 - Future Expansion"; break;
        case 0xE3: PC += 2; dbg[0] = "E3 - Future Expansion"; break;
        case 0xE7: PC += 2; dbg[0] = "E7 - Future Expansion"; break;
        case 0xEB: PC += 2; dbg[0] = "EB - Future Expansion"; break;
        case 0xEF: PC += 2; dbg[0] = "EF - Future Expansion"; break;
        case 0xF2: PC += 2; dbg[0] = "F2 - Future Expansion"; break;
        case 0xF3: PC += 2; dbg[0] = "F3 - Future Expansion"; break;
        case 0xF4: PC += 2; dbg[0] = "F4 - Future Expansion"; break;
        case 0xF7: PC += 2; dbg[0] = "F7 - Future Expansion"; break;
        case 0xFA: PC += 2; dbg[0] = "FA - Future Expansion"; break;
        case 0xFB: PC += 2; dbg[0] = "FB - Future Expansion"; break;
        case 0xFC: PC += 2; dbg[0] = "FC - Future Expansion"; break;
        case 0xFF: PC += 2; dbg[0] = "FF - Future Expansion"; break;





















        default:
          dbg[0] = "OP 0x" + (opcode & 0xFF).ToString("X") + " DNE yet";
          Console.ReadKey();
          break;


      }
      Print_dbg();
      Clear_dbg();
      
      //Print_status();
      
    }
  }
}
