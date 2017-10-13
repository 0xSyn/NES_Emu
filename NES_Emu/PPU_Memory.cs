using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emu {
    class PPU_Memory {

        /*
         PPU Memory MapVROM or VRAM appearing in the PPU address space at $0000-$1FFF. 

--------------------------------------- $4000
 Empty
--------------------------------------- $3F20
 Sprite Palette
--------------------------------------- $3F10
 Image Palette
--------------------------------------- $3F00
 Empty
--------------------------------------- $3000
 Attribute Table 3
--------------------------------------- $2FC0
 Name Table 3 (32x30 tiles)
--------------------------------------- $2C00
 Attribute Table 2
--------------------------------------- $2BC0
 Name Table 2 (32x30 tiles)
--------------------------------------- $2800
 Attribute Table 1
--------------------------------------- $27C0
 Name Table 1 (32x30 tiles)
--------------------------------------- $2400
 Attribute Table 0
--------------------------------------- $23C0
 Name Table 0 (32x30 tiles)
--------------------------------------- $2000 ------
 Pattern Table 1 (256x2x8, may be VROM)
--------------------------------------- $1000V  ROM or VRAM appearing in the PPU address space at $0000-$1FFF. 
 Pattern Table 0 (256x2x8, may be VROM)
--------------------------------------- $0000 ------


*/
        UInt16[] memory = new UInt16[0x4000];
    }
}
