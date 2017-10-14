using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emu {
    class CPU {// 6502 microprocessor 


        static byte SP;//8-bit stack pointer(fixed at RAM address $100, so can address $100 -$1ff)
        public static UInt16 PC;//16 - bit program counter
        static UInt16 opcode;
        static int cycle_num;

        static bool DEBUG = true;
        static string[] dbg = new string[0xF];
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
        /// Bit 7 - S - Sign flag: this is set if the result of an operation is negative, cleared if positive.
        /// </summary>
        static byte S;// 0b SV5B DIZC
        //148 total instructions, (a lot of these are the very similar)
        //Little Endian architecture
        //

        public static void Init_cpu(){
            SP = 0x0;
            PC=CPU_Memory.rom_lowBank;
            opcode = 0x0;
            A = 0x0;
            X = 0x0;
            Y = 0x0;
            S = 0x0;
            Init_stack();
            CPU_Memory.Init_mem();
            CPU_Memory.Load_NES(@"D:\Projects\NES_Emu\ff.nes");
            //CPU_Memory.Print_mem();
            //Console.WriteLine("1 && 1 ==" + (1 && 1));
        }


        public static void Init_stack() {
            for (int i = 0; i < stack.Length; i++) { stack[i] = 0; }
        }

        /// <summary>
        /// Sets a flag value of status register to either 1 or 0;
        /// </summary>
        /// <param name="flag">0-7 --- c=CARRY, z=ZERO, i=INTERRUPT, d=DECIMAL, b=BREAK, (bit5DNE), v=OVERFLOW, s=SIGN</param>
        /// <param name="val">0-1</param>
        public static void SET_FLAG(char flag,byte val) {//BROKEN
            byte f = get_flag(flag);
            if (val == 1) { S = (byte)(S | (val << f)); }// FLAG = 1
            else          { S = (byte)(S & (val << f)); }// FLAG = 0
        }

        /// <summary>
        /// Set sign to the 7th bit of input parameter
        /// </summary>
        /// <param name="tmp">uses 7th bit</param>
        public static void SET_SIGN(UInt16 tmp) {
            if ((0b10000000 & tmp) > 0) { S = (byte)(S | 0b10000000); }// Sets sign to 7th bit of parameter
            else                        { S = (byte)(S & 0b01111111); }
        }


        /// <summary>
        /// Generic get flag
        /// </summary>
        /// <param name="flag">STATUS register bit to return</param>
        /// <returns>Flag Bit</returns>
        public static int GET_FLAG(char flag) {
            byte f = get_flag(flag);
            return ((S & (0b00000001 << f)) >> f);// Returns flag value: 0 or 1
        }

        /// <summary>
        /// sets\resets the zero flag depending on whether the result is zero or not.
        /// </summary>
        /// <param name="condition"></param>
        public static void SET_ZERO(UInt16 condition) {
            if (condition==0) { S = (byte)(S | 0b10); }// ZERO = 1
            else              { S = (byte)(S & 0b01); }// ZERO = 0
        }

        public static void SET_OVERFLOW(bool condition) {
            if (condition) { S = (byte)(S | 0b1000000); }// OVERFLOW = 1
            else           { S = (byte)(S & 0b0111111); }// OVERFLOW = 0
        }


        /// <summary>
        /// if the condition has a non-zero value then the carry flag is set, else it is reset.
        /// </summary>
        /// <param name="condition"></param>
        public static void SET_CARRY(bool condition) {
            if (condition) { S = (byte)(S | 0b1); }// CARRY = 1
            else { S = (byte)(S & 0b0); }// CARRY = 0
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
                case 's': return 7;
                default: Console.WriteLine("ERR: FLAG '"+flag+"' DNE");  return 8;
            }
        }

        /// <summary>
        /// Push Byte onto stack
        /// </summary>
        /// <param name="stackEntry">Byte to push</param>
        public static void PUSH(byte stackEntry) {stack[SP++] = stackEntry;}
        /// <summary>
        /// Pull Byte from stack. Decrement pointer
        /// </summary>
        /// <returns>top element of stack</returns>
        public static byte PULL() {return stack[--SP];}




        public static void Emulation_cycle(byte[] data) {
            cycle_num++;
            opcode = (UInt16)((data[PC] << 8) | (data[PC+1]));
            switch (opcode & 0x00FF) {
                case 0x00:
                    dbg[0] = "00 - BRK - Force Break ";
                    dbg[1] = "Operation:  Forced Interrupt PC + 2 toStk P toStk ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  Implied       |   BRK                 |    00   |    1    |    7     | _ _ _ 1 _ _";
                    dbg[4] = "Note: A BRK command cannot be masked by setting I.";
                    PC+=2;
                    PUSH((byte)((PC >> 8) & 0xFF));//Push return address onto the stack
                    PUSH((byte)(PC & 0xFF));
                    SET_FLAG('b',1);//Set Break Flag
                    PUSH(S);//Push Status register onto stack
                    SET_FLAG('i',1);//Set Interrupt Flag
                    PC = (UInt16)(CPU_Memory.Get_addr(0xFFFE) | (CPU_Memory.Get_addr(0xFFFF) << 8));
                    break;
                case 0x01:
                    dbg[0] = "01 - ORA (Indirect,X) - 'OR' memory with accumulator";
                    dbg[1] = "Operation: A | M -> A ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  (Indirect,X)  |   ORA (Oper,X)        |    01   |    2    |    6     | / / _ _ _ _";
                    dbg[4] = "Note:";
                    //src |= A;
                    //Set_flag('s',src);
                    //Set_flag('z',src);
                    //A = src;
                    break;
                case 0x02: dbg[0] = "02 - Future Expansion"; break;
                case 0x03: dbg[0] = "03 - Future Expansion "; break;
                case 0x04: dbg[0] = "04 - Future Expansion "; break;
                case 0x05: dbg[0] = "05 - ORA - Zero Page"; break;
                case 0x06: dbg[0] = "06 - ASL - Zero Page"; break;
                case 0x07: dbg[0] = "07 - Future Expansion"; break;
                case 0x08: dbg[0] = "08 - PHP"; break;
                case 0x09: dbg[0] = "09 - ORA - Immediate"; break;
                case 0x0A: dbg[0] = "0A - ASL - Accumulator"; break;
                case 0x0B: dbg[0] = "0B - Future Expansion"; break;
                case 0x0C: dbg[0] = "0C - Future Expansion"; break;
                case 0x0D: dbg[0] = "0D - ORA - Absolute"; break;
                case 0x0E: dbg[0] = "0E - ASL - Absolute"; break;
                case 0x0F: dbg[0] = "0F - Future Expansion"; break;
                case 0x10: dbg[0] = "10 - BPL"; break;
                case 0x11: dbg[0] = "11 - ORA - (Indirect),Y"; break;
                case 0x12: dbg[0] = "12 - Future Expansion"; break;
                case 0x13: dbg[0] = "13 - Future Expansion"; break;
                case 0x14: dbg[0] = "14 - Future Expansion"; break;
                case 0x15: dbg[0] = "15 - ORA - Zero Page,X"; break;
                case 0x16: dbg[0] = "16 - ASL - Zero Page,X"; break;
                case 0x17: dbg[0] = "17 - Future Expansion"; break;
                case 0x18: dbg[0] = "18 - CLC"; break;
                case 0x19: dbg[0] = "19 - ORA - Absolute,Y"; break;
                case 0x1A: dbg[0] = "1A - Future Expansion"; break;
                case 0x1B: dbg[0] = "1B - Future Expansion"; break;
                case 0x1C: dbg[0] = "1C - Future Expansion"; break;
                case 0x1D: dbg[0] = "1D - ORA - Absolute,X"; break;
                case 0x1E: dbg[0] = "1E - ASL - Absolute,X"; break;
                case 0x1F: dbg[0] = "1F - Future Expansion"; break;
//---------------------------------------------------------------------------------------------------------------------------------
                case 0x20: dbg[0] = "20 - JSR"; break;
                case 0x21: dbg[0] = "21 - AND - (Indirect,X)"; break;
                case 0x22: dbg[0] = "22 - Future Expansion"; break;
                case 0x23: dbg[0] = "23 - Future Expansion"; break;
                case 0x24: dbg[0] = "24 - BIT - Zero Page"; break;
                case 0x25: dbg[0] = "25 - AND - Zero Pag"; break;
                case 0x26: dbg[0] = "26 - ROL - Zero Page"; break;
                case 0x27: dbg[0] = "27 - Future Expansion"; break;
                case 0x28: dbg[0] = "28 - PLP"; break;
                case 0x29: dbg[0] = "29 - AND - Immediate"; break;
                case 0x2A: dbg[0] = "2A - ROL - Accumulator"; break;
                case 0x2B: dbg[0] = "2B - Future Expansion"; break;
                case 0x2C: dbg[0] = "2C - BIT - Absolute"; break;
                case 0x2D: dbg[0] = "2D - AND - Absolute"; break;
                case 0x2E: dbg[0] = "2E - ROL - Absolute"; break;
                case 0x2F: dbg[0] = "2F - Future Expansion"; break;
                case 0x30: dbg[0] = "30 - BMI"; break;
                case 0x31: dbg[0] = "31 - AND - (Indirect),Y"; break;
                case 0x32: dbg[0] = "32 - Future Expansion"; break;
                case 0x33: dbg[0] = "33 - Future Expansion"; break;
                case 0x34: dbg[0] = "34 - Future Expansion"; break;
                case 0x35: dbg[0] = "35 - AND - Zero Page,X"; break;
                case 0x36: dbg[0] = "36 - ROL - Zero Page,X"; break;
                case 0x37: dbg[0] = "37 - Future Expansion"; break;
                case 0x38: dbg[0] = "38 - SEC"; break;
                case 0x39: dbg[0] = "39 - AND - Absolute,Y"; break;
                case 0x3A: dbg[0] = "3A - Future Expansion"; break;
                case 0x3B: dbg[0] = "3B - Future Expansion"; break;
                case 0x3C: dbg[0] = "3C - Future Expansion"; break;
                case 0x3D: dbg[0] = "3D - AND - Absolute,X"; break;
                case 0x3E: dbg[0] = "3E - ROL - Absolute,X"; break;
                case 0x3F: dbg[0] = "3F - Future Expansion"; break;
//---------------------------------------------------------------------------------------------------------------------------------
                case 0x40: dbg[0] = "40 - RTI"; break;
                case 0x41: dbg[0] = "41 - EOR - (Indirect,X)"; break;
                case 0x42: dbg[0] = "42 - Future Expansion"; break;
                case 0x43: dbg[0] = "43 - Future Expansion"; break;
                case 0x44: dbg[0] = "44 - Future Expansion"; break;
                case 0x45: dbg[0] = "45 - EOR - Zero Page"; break;
                case 0x46: dbg[0] = "46 - LSR - Zero Page "; break;
                case 0x47: dbg[0] = "47 - Future Expansion "; break;
                case 0x48: dbg[0] = "48 - PHA "; break;
                case 0x49: dbg[0] = "49 - EOR - Immediate"; break;
                case 0x4A: dbg[0] = "4A - LSR - Accumulator"; break;
                case 0x4B: dbg[0] = "4B - Future Expansion"; break;
                case 0x4C: dbg[0] = "4C - JMP - Absolute "; break;
                case 0x4D: dbg[0] = "4D - EOR - Absolute"; break;
                case 0x4E: dbg[0] = "4E - LSR - Absolute"; break;
                case 0x4F: dbg[0] = "4F - Future Expansion"; break;
                case 0x50: dbg[0] = "50 - BVC"; break;
                case 0x51: dbg[0] = "51 - EOR - (Indirect),Y"; break;
                case 0x52: dbg[0] = "52 - Future Expansion"; break;
                case 0x53: dbg[0] = "53 - Future Expansion"; break;
                case 0x54: dbg[0] = "54 - Future Expansion"; break;
                case 0x55: dbg[0] = "55 - EOR - Zero Page,X"; break;
                case 0x56: dbg[0] = "56 - LSR - Zero Page,X"; break;
                case 0x57: dbg[0] = "57 - Future Expansion"; break;
                case 0x58: dbg[0] = "58 - CLI"; break;
                case 0x59: dbg[0] = "59 - EOR - Absolute,Y"; break;
                case 0x5A: dbg[0] = "5A - Future Expansion"; break;
                case 0x5B: dbg[0] = "5B - Future Expansion"; break;
                case 0x5C: dbg[0] = "5C - Future Expansion"; break;
                case 0x5D: dbg[0] = "5D - EOR - Absolute,X"; break;
                case 0x5E: dbg[0] = "5E - LSR - Absolute,X"; break;
                case 0x5F: dbg[0] = "5F - Future Expansion"; break;
//---------------------------------------------------------------------------------------------------------------------------------
                case 0x60: dbg[0] = "60 - RTS"; break;
                case 0x61:
                    dbg[0] = "61 - ADC - (Indirect,X) - Add memory to accumulator with carry ";
                    dbg[1] = "Operation: A + M + C -> A, C ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  (Indirect,X)  |   ADC (Oper,X)        |    61   |    2    |    6     | / / / _ _ /";
                    dbg[4] = "Note:";
                    byte src =0;
                    UInt16 tmp = (UInt16)(src + A + (GET_FLAG('c')));
                    SET_ZERO((UInt16)(tmp&0xFF));//  /* This is not valid in decimal mode */
                    if (GET_FLAG('d')==1) {
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
                    break;
                case 0x62: dbg[0] = "62 - Future Expansion"; break;
                case 0x63: dbg[0] = "63 - Future Expansion"; break;
                case 0x64: dbg[0] = "64 - Future Expansion"; break;
                case 0x65:
                    dbg[0] = "65 - ADC - Zero Page - Add memory to accumulator with carry ";
                    dbg[1] = "Operation: A + M + C -> A, C ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  Zero Page     |   ADC Oper            |    65   |    2    |    3     | / / / _ _ /";
                    dbg[4] = "Note:";
                    break;
                case 0x66: dbg[0] = "66 - ROR - Zero Page"; break;
                case 0x67: dbg[0] = "67 - Future Expansion"; break;
                case 0x68: dbg[0] = "68 - PLA"; break;
                case 0x69:
                    dbg[0] = "69 - ADC - Immediate - Add memory to accumulator with carry ";
                    dbg[1] = "Operation: A + M + C -> A, C ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  Immediate     |   ADC #Oper           |    69   |    2    |    2     | / / / _ _ /";
                    dbg[4] = "Note:";
                    break;
                case 0x6A: dbg[0] = "6A - ROR - Accumulator"; break;
                case 0x6B: dbg[0] = "6B - Future Expansion"; break;
                case 0x6C: dbg[0] = "6C - JMP - Indirect"; break;
                case 0x6D:
                    dbg[0] = "6D - ADC - Absolute - Add memory to accumulator with carry ";
                    dbg[1] = "Operation: A + M + C -> A, C ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  Absolute      |   ADC Oper            |    6D   |    3    |    4     | / / / _ _ /";
                    dbg[4] = "Note:";
                    break;
                case 0x6E: dbg[0] = "6E - ROR - Absolute"; break;
                case 0x6F: dbg[0] = "6F - Future Expansion"; break;
                case 0x70: dbg[0] = "70 - BVS"; break;
                case 0x71:
                    dbg[0] = "71 - ADC - (Indirect),Y - Add memory to accumulator with carry ";
                    dbg[1] = "Operation: A + M + C -> A, C ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  (Indirect),Y  |   ADC (Oper),Y        |    71   |    2    |    5*    | / / / _ _ /";
                    dbg[4] = "Note: Add 1 if page boundary is crossed";
                    break;
                case 0x72: dbg[0] = "72 - Future Expansion"; break;
                case 0x73: dbg[0] = "73 - Future Expansion"; break;
                case 0x74: dbg[0] = "74 - Future Expansion"; break;
                case 0x75:
                    dbg[0] = "75 - ADC - Zero Page,X - Add memory to accumulator with carry ";
                    dbg[1] = "Operation: A + M + C -> A, C ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  Zero Page,X   |   ADC Oper,X          |    75   |    2    |    4     | / / / _ _ /";
                    dbg[4] = "Note:";
                    break;
                case 0x76: dbg[0] = "76 - ROR - Zero Page,X"; break;
                case 0x77: dbg[0] = "77 - Future Expansion"; break;
                case 0x78: dbg[0] = "78 - SEI"; break;
                case 0x79:
                    dbg[0] = "79 - ADC - Absolute,Y - Add memory to accumulator with carry ";
                    dbg[1] = "Operation: A + M + C -> A, C ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  Absolute,Y    |   ADC Oper,Y          |    79   |    3    |    4*    | / / / _ _ /";
                    dbg[4] = "Note: Add 1 if page boundary is crossed";
                    break;

                case 0x7A: dbg[0] = "7A - Future Expansion"; break;
                case 0x7B: dbg[0] = "7B - Future Expansion"; break;
                case 0x7C: dbg[0] = "7C - Future Expansion"; break;
                case 0x7D:
                    dbg[0] = "7D - ADC - Absolute,X - Add memory to accumulator with carry ";
                    dbg[1] = "Operation: A + M + C -> A, C ";
                    dbg[2] = "| Addressing Mode| Assembly Language Form| OP CODE |No. Bytes|No. Cycles| N Z C I D V";
                    dbg[3] = "|  Absolute,X    |   ADC Oper,X          |    7D   |    3    |    4*    | / / / _ _ /";
                    dbg[4] = "Note: Add 1 if page boundary is crossed";
                    break;

                case 0x7E: dbg[0] = "7E - ROR - Absolute,X"; break;
                case 0x7F: dbg[0] = "7F - Future Expansion"; break;
//---------------------------------------------------------------------------------------------------------------------------------
                case 0x80: dbg[0] = "80 - Future Expansion"; break;
                case 0x81: dbg[0] = "81 - STA - (Indirect,X)"; break;
                case 0x82: dbg[0] = "82 - Future Expansion"; break;
                case 0x83: dbg[0] = "83 - Future Expansion"; break;
                case 0x84: dbg[0] = "84 - STY - Zero Page"; break;
                case 0x85: dbg[0] = "85 - STA - Zero Page"; break;
                case 0x86: dbg[0] = "86 - STX - Zero Page"; break;
                case 0x87: dbg[0] = "87 - Future Expansion"; break;
                case 0x88: dbg[0] = "88 - DEY"; break;
                case 0x89: dbg[0] = "89 - Future Expansion"; break;
                case 0x8A: dbg[0] = "8A - TXA "; break;
                case 0x8B: dbg[0] = "8B - Future Expansion"; break;
                case 0x8C: dbg[0] = "8C - STY - Absolute"; break;
                case 0x8D: dbg[0] = "8D - STA - Absolute "; break;
                case 0x8E: dbg[0] = "8E - STX - Absolute"; break;
                case 0x8F: dbg[0] = "8F - Future Expansion"; break;
                case 0x90: dbg[0] = "90 - BCC"; break;
                case 0x91: dbg[0] = "91 - STA - (Indirect),Y"; break;
                case 0x92: dbg[0] = "92 - Future Expansion"; break;
                case 0x93: dbg[0] = "93 - Future Expansion"; break;
                case 0x94: dbg[0] = "94 - STY - Zero Page,X"; break;
                case 0x95: dbg[0] = "95 - STA - Zero Page,X"; break;
                case 0x96: dbg[0] = "96 - STX - Zero Page,Y"; break;
                case 0x97: dbg[0] = "97 - Future Expansion"; break;
                case 0x98: dbg[0] = "98 - TYA"; break;
                case 0x99: dbg[0] = "99 - STA - Absolute,Y"; break;
                case 0x9A: dbg[0] = "9A - TXS"; break;
                case 0x9B: dbg[0] = "9B - Future Expansion"; break;
                case 0x9C: dbg[0] = "9C - Future Expansion"; break;
                case 0x9D: dbg[0] = "9D - STA - Absolute,X"; break;
                case 0x9E: dbg[0] = "9E - Future Expansion"; break;
                case 0x9F: dbg[0] = "9F - Future Expansion"; break;
//---------------------------------------------------------------------------------------------------------------------------------
                case 0xA0: dbg[0] = "A0 - LDY - Immediate"; break;
                case 0xA1: dbg[0] = "A1 - LDA - (Indirect,X)"; break;
                case 0xA2: dbg[0] = "A2 - LDX - Immediate"; break;
                case 0xA3: dbg[0] = "A3 - Future Expansion"; break;
                case 0xA4: dbg[0] = "A4 - LDY - Zero Page"; break;
                case 0xA5: dbg[0] = "A5 - LDA - Zero Page"; break;
                case 0xA6: dbg[0] = "A6 - LDX - Zero Page"; break;
                case 0xA7: dbg[0] = "A7 - Future Expansion"; break;
                case 0xA8: dbg[0] = "A8 - TAY"; break;
                case 0xA9: dbg[0] = "A9 - LDA - Immediate"; break;
                case 0xAA: dbg[0] = "AA - TAX"; break;
                case 0xAB: dbg[0] = "AB - Future Expansion"; break;
                case 0xAC: dbg[0] = "AC - LDY - Absolute"; break;
                case 0xAD: dbg[0] = "AD - LDA - Absolute"; break;
                case 0xAE: dbg[0] = "AE - LDX - Absolute"; break;
                case 0xAF: dbg[0] = "AF - Future Expansion"; break;
                case 0xB0: dbg[0] = "B0 - BCS"; break;
                case 0xB1: dbg[0] = "B1 - LDA - (Indirect),Y"; break;
                case 0xB2: dbg[0] = "B2 - Future Expansion"; break;
                case 0xB3: dbg[0] = "B3 - Future Expansion"; break;
                case 0xB4: dbg[0] = "B4 - LDY - Zero Page,X"; break;
                case 0xB5: dbg[0] = "BS - LDA - Zero Page,X"; break;
                case 0xB6: dbg[0] = "B6 - LDX - Zero Page,Y"; break;
                case 0xB7: dbg[0] = "B7 - Future Expansion"; break;
                case 0xB8: dbg[0] = "B8 - CLV"; break;
                case 0xB9: dbg[0] = "B9 - LDA - Absolute,Y"; break;
                case 0xBA: dbg[0] = "BA - TSX"; break;
                case 0xBB: dbg[0] = "BB - Future Expansion"; break;
                case 0xBC: dbg[0] = "BC - LDY - Absolute,X"; break;
                case 0xBD: dbg[0] = "BD - LDA - Absolute,X"; break;
                case 0xBE: dbg[0] = "BE - LDX - Absolute,Y"; break;
                case 0xBF: dbg[0] = "BF - Future Expansion"; break;
//---------------------------------------------------------------------------------------------------------------------------------
                case 0xC0: dbg[0] = "C0 - Cpy - Immediate"; break;
                case 0xC1: dbg[0] = "C1 - CMP - (Indirect,X)"; break;
                case 0xC2: dbg[0] = "C2 - Future Expansion"; break;
                case 0xC3: dbg[0] = "C3 - Future Expansion"; break;
                case 0xC4: dbg[0] = "C4 - CPY - Zero Page"; break;
                case 0xC5: dbg[0] = "C5 - CMP - Zero Page"; break;
                case 0xC6: dbg[0] = "C6 - DEC - Zero Page"; break;
                case 0xC7: dbg[0] = "C7 - Future Expansion"; break;
                case 0xC8: dbg[0] = "C8 - INY"; break;
                case 0xC9: dbg[0] = "C9 - CMP - Immediate"; break;
                case 0xCA: dbg[0] = "CA - DEX"; break;
                case 0xCB: dbg[0] = "CB - Future Expansion"; break;
                case 0xCC: dbg[0] = "CC - CPY - Absolute"; break;
                case 0xCD: dbg[0] = "CD - CMP - Absolute"; break;
                case 0xCE: dbg[0] = "CE - DEC - Absolute"; break;
                case 0xCF: dbg[0] = "CF - Future Expansion"; break;
                case 0xD0: dbg[0] = "D0 - BNE"; break;
                case 0xD1: dbg[0] = "D1 - CMP   (Indirect),Y"; break;
                case 0xD2: dbg[0] = "D2 - Future Expansion"; break;
                case 0xD3: dbg[0] = "D3 - Future Expansion"; break;
                case 0xD4: dbg[0] = "D4 - Future Expansion"; break;
                case 0xD5: dbg[0] = "D5 - CMP - Zero Page,X"; break;
                case 0xD6: dbg[0] = "D6 - DEC - Zero Page,X"; break;
                case 0xD7: dbg[0] = "D7 - Future Expansion"; break;
                case 0xD8: dbg[0] = "D8 - CLD"; break;
                case 0xD9: dbg[0] = "D9 - CMP - Absolute,Y"; break;
                case 0xDA: dbg[0] = "DA - Future Expansion"; break;
                case 0xDB: dbg[0] = "DB - Future Expansion"; break;
                case 0xDC: dbg[0] = "DC - Future Expansion"; break;
                case 0xDD: dbg[0] = "DD - CMP - Absolute,X"; break;
                case 0xDE: dbg[0] = "DE - DEC - Absolute,X"; break;
                case 0xDF: dbg[0] = "DF - Future Expansion"; break;
//---------------------------------------------------------------------------------------------------------------------------------
                case 0xE0: dbg[0] = "E0 - CPX - Immediate"; break;
                case 0xE1: dbg[0] = "E1 - SBC - (Indirect,X)"; break;
                case 0xE2: dbg[0] = "E2 - Future Expansion"; break;
                case 0xE3: dbg[0] = "E3 - Future Expansion"; break;
                case 0xE4: dbg[0] = "E4 - CPX - Zero Page"; break;
                case 0xE5: dbg[0] = "E5 - SBC - Zero Page"; break;
                case 0xE6: dbg[0] = "E6 - INC - Zero Page"; break;
                case 0xE7: dbg[0] = "E7 - Future Expansion"; break;
                case 0xE8: dbg[0] = "E8 - INX"; break;
                case 0xE9: dbg[0] = "E9 - SBC - Immediate"; break;
                case 0xEA: dbg[0] = "EA - NOP"; break;
                case 0xEB: dbg[0] = "EB - Future Expansion"; break;
                case 0xEC: dbg[0] = "EC - CPX - Absolute"; break;
                case 0xED: dbg[0] = "ED - SBC - Absolute"; break;
                case 0xEE: dbg[0] = "EE - INC - Absolute"; break;
                case 0xEF: dbg[0] = "EF - Future Expansion"; break;
                case 0xF0: dbg[0] = "F0 - BEQ"; break;
                case 0xF1: dbg[0] = "F1 - SBC - (Indirect),Y"; break;
                case 0xF2: dbg[0] = "F2 - Future Expansion"; break;
                case 0xF3: dbg[0] = "F3 - Future Expansion"; break;
                case 0xF4: dbg[0] = "F4 - Future Expansion"; break;
                case 0xF5: dbg[0] = "F5 - SBC - Zero Page,X"; break;
                case 0xF6: dbg[0] = "F6 - INC - Zero Page,X"; break;
                case 0xF7: dbg[0] = "F7 - Future Expansion"; break;
                case 0xF8: dbg[0] = "F8 - SED"; break;
                case 0xF9: dbg[0] = "F9 - SBC - Absolute,Y"; break;
                case 0xFA: dbg[0] = "FA - Future Expansion"; break;
                case 0xFB: dbg[0] = "FB - Future Expansion"; break;
                case 0xFC: dbg[0] = "FC - Future Expansion"; break;
                case 0xFD: dbg[0] = "FD - SBC - Absolute,X"; break;
                case 0xFE: dbg[0] = "FE - INC - Absolute,X"; break;
                case 0xFF: dbg[0] = "FF - Future Expansion"; break;
                default:Console.WriteLine("OP CODE: "+opcode+" NOT FOUND"); break;

            }
            Console.WriteLine(
                "\n"+dbg[0]+
                "\n"+dbg[1]+
                "\n"+dbg[2]+
                "\n"+dbg[3]
                );
        }
    }
}
