﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NES_Emu {
    class Program {
        static void Main(string[] args) {
            CPU.Init_cpu();
            
            Console.Read();
        }
    }
}
