using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DOTNET_CHIP_8
{

    class CHIP_8
    {
        //VM Specifications
        byte[] memory = new byte[4096];
        byte[] cpu_V = new byte[16];
        byte[] gfx = new byte[2048];
        ushort[] key = new ushort[16];

        byte[] Fontset = {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  //F
        };

        //two ("timers") registers count at 60hz
        //when set over 0, they will count down to 0
        ushort delay_timer = 0;
        ushort sound_timer = 0;

        //CHIP-8 has 35 opcodes and two index registers
        ushort opcode = 0;
        ushort I = 0;
        ushort pc = 0x200; //program counter

        //Stack Interpreter
        ushort[] stack = new ushort[16];
        ushort stackPtr = 0;

        bool keypress = false;
        uint romSize = 0;

        //VM Initialization
        public CHIP_8()
        {
            Console.WriteLine($"System Memory: {memory.Length} bytes");
            Console.WriteLine($"Stack Size: {stack.Length}");
            LoadFonts();
        }

        public void LoadGame(byte[] game)
        {
            Console.WriteLine($"ROM File Size: {game.Length}K");
            romSize = 0;

            //Load game in memory
            for (int i = 0; i < game.Length; i++)
            {
                romSize++;
                memory[512 + i] = game[i];
            }

            Console.WriteLine($"Loaded {romSize}K into memory!");

            while(romSize > 0)
            {
                EmulateCycle();
            }
        }

        private void EmulateCycle()
        {
            opcode = (ushort)(memory[pc] << 8 | memory[pc + 1]);

            Console.WriteLine("0x{0:x}", opcode);
            Console.WriteLine("0x{0:x}", opcode & 0xF000);

            romSize = 0;

            //switch (opcode & 0xF000)
            //{
            //    case 0xA000: // ANNN: Sets I to the address NNN
            //                 // Execute opcode
            //        Console.WriteLine("Running {0:X}", opcode);
            //        I = (ushort)(opcode & 0x0FFF);
            //        pc += 2;
            //        break;
            //    default:
            //        Console.WriteLine("Opcode unknown: {0:X}", opcode);
            //        break;
            //}

            UpdateTimers();
        }

        private void UpdateTimers()
        {
            if (delay_timer > 0)
                delay_timer--;

            if (sound_timer > 0)
                //Console.WriteLine("BEEP");
                sound_timer--;
        }

        private void LoadFonts()
        {
            for(int i = 0; i < 80; i++)
            {
                memory[i] = Fontset[i];
            }

            Console.WriteLine("Fontset loaded into memory.");
        }

        void CPUNull()
        {
            //nothing
        }



    }
}
