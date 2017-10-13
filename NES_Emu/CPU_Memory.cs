using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emu {
    class CPU_Memory {
        UInt16[] memory = new UInt16[0xFFFF];//65,536 addresses
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


    }
}
