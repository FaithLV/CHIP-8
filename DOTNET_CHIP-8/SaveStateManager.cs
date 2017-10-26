using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOTNET_CHIP_8
{
    class SaveStateManager
    {
        private CHIP_8 CPUCore;

        public SaveStateManager(CHIP_8 chip)
        {
            CPUCore = chip;
        }

        public void SaveAState(string _name)
        {
            //byte[] memoryDump = CPUCore.memory;
            //byte[] graphicsBuffer = CPUCore.gfx;
            //byte[] cpuvDump = CPUCore.cpu_V;
            //ushort delay_timer = CPUCore.delay_timer;
            //ushort sound_timer = CPUCore.sound_timer;
            //ushort OPCode = CPUCore.opcode;
            //ushort CPU_I = CPUCore.I;
            //ushort ProgramCounter = CPUCore.pc;
            //ushort[] stack = CPUCore.stack;
            //ushort stackPtr = CPUCore.stackPtr;
            //uint romsize = CPUCore.romSize;
        }

        internal string DataDump<T>(string name, T data)
        {
            string _databuffer = null;

            if(typeof(T) == typeof(byte[]))
            {
                byte[] _d = data as byte[];
                for(int i = 0; i < _d.Length; i++)
                {
                    _databuffer += _d[i];
                }
            }
            else if(typeof(T) == typeof(ushort[]))
            {
                ushort[] _d = data as ushort[];
                for(int i = 0; i < _d.Length; i++)
                {
                    _databuffer += _d[i];
                }
            }
            else if (typeof(T) == typeof(ushort))
            {
                _databuffer = data.ToString();
            }
            else if (typeof(T) == typeof(uint))
            {
                _databuffer = data.ToString();
            }
            else
            {
                Console.WriteLine("Unhandled datatype.");
            }

            return _databuffer;
        }
    }
}
