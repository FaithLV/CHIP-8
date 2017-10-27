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
        private string statefolder = AppDomain.CurrentDomain.BaseDirectory + "savestates";

        public SaveStateManager(CHIP_8 chip)
        {
            CPUCore = chip;
        }

        public void SaveAState(string _name)
        {
            string file = $"{statefolder}\\{_name}.mem";

            Directory.CreateDirectory(statefolder);
            if(File.Exists(file))
            {
                Console.WriteLine($"Overwriting savestate {_name}");
                File.Delete(file);
            }
 
            using(StreamWriter writer = new StreamWriter(file))
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

            Console.WriteLine($"State saved: {_name}.mem");
        }

        public void LoadAState(string _name)
        {
            string file = $"{statefolder}\\{_name}.mem";

            if(!File.Exists(file))
            {
                Console.WriteLine($"Save state {file} not found!");
                return;
            }

            CPUCore.isPaused = true;

            using (StreamReader reader = new StreamReader(file))
            {
                Console.WriteLine(reader.ReadLine());
                Console.WriteLine(reader.ReadLine());
                Console.WriteLine(reader.ReadLine());
            }

            CPUCore.isPaused = false;

        }

        //Raw data
        internal string DataDump<T>(T data)
        {
            string _databuffer = null;

            if(typeof(T) == typeof(byte[]))
            {
                byte[] _d = data as byte[];
                for(int i = 0; i < _d.Length; i++)
                {
                    _databuffer += _d[i];
                    _databuffer += " ";
                }
            }
            else if(typeof(T) == typeof(ushort[]))
            {
                ushort[] _d = data as ushort[];
                for(int i = 0; i < _d.Length; i++)
                {
                    _databuffer += _d[i];
                    _databuffer += " ";
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
