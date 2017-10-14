using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emu {
    class PPU {

        
        byte[] sprite = new byte[8];// 8x8 sprites
        byte[] sprite_sheet = new byte[32*30*8];// 32x30 sprites
        public static void Init_ppu() {
            PPU_Memory.Init_memory();
        }

    }
}
