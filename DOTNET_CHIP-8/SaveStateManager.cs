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
            using(StreamWriter writer = new StreamWriter($"{AppDomain.CurrentDomain.BaseDirectory}//{_name}.mem"))
            {
                writer.WriteLine(DataDump(CPUCore.memory));
                writer.WriteLine(DataDump(CPUCore.gfx));
                writer.WriteLine(DataDump(CPUCore.cpu_V));
                writer.WriteLine(DataDump(CPUCore.delay_timer));
                writer.WriteLine(DataDump(CPUCore.sound_timer));
                writer.WriteLine(DataDump(CPUCore.opcode));
                writer.WriteLine(DataDump(CPUCore.I));
                writer.WriteLine(DataDump(CPUCore.pc));
                writer.WriteLine(DataDump(CPUCore.stack));
                writer.WriteLine(DataDump(CPUCore.stackPtr));
                writer.WriteLine(DataDump(CPUCore.romSize));
            }
        }

        //Return raw data values from CPU as strings
        internal string DataDump<T>(T data)
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
