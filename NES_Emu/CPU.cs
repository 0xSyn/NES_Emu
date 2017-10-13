using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emu {
    class CPU {
        // 6502 processor   

        byte sp;//8-bit stack pointer(fixed at RAM address $100, so can address $100 -$1ff)
        UInt16 pc;//16 - bit program counter
        byte A, X, Y;//3 8-bit general purpose registers A, X, and Y

        //148 total instructions, (a lot of these are the very similar)
        //Little Endian architecture
        //



    }
}
