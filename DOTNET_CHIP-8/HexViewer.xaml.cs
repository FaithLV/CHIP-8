using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace DOTNET_CHIP_8
{
    /// <summary>
    /// Interaction logic for HexViewer.xaml
    /// </summary>
    public partial class HexViewer : UserControl
    {
        private static List<HexEntry> HexEntryList;
        const int lineLenght = 16;

        public HexViewer()
        {
            InitializeComponent();
            HexEntryList = new List<HexEntry>();
        }

        private static string BytesToHexBuffer(ref byte[] buffer)
        {
            StringBuilder hex = new StringBuilder(buffer.Length * 2);
            for (int i = 0; i < buffer.Length; i++)
            {
                hex.AppendFormat("{0:x2}", buffer[i]);
                hex.Append(' ', 1);
            }
            return hex.ToString();
        }

        public void SetBuffer(ref byte[] buffer)
        {
            //skip empty buffers
            if(buffer.Length == 0)
            {
                return;
            }

            string hexDump = BytesToHexBuffer(ref buffer);
            string[] splitHex = hexDump.Split(' ');
            string[] currentBuffer = new string[lineLenght];

            for (int i = 0; i < splitHex.Length - 1; i = (lineLenght - 1) + i + 1)
            {
                for (int j = 0; j < lineLenght; j++)
                {
                    Console.WriteLine($"{i + j} : {splitHex[i+j]}");
                    currentBuffer[j] = (splitHex[i + j].ToString());
                }

                Console.WriteLine("");

                string kk = String.Empty;
                foreach(string st in currentBuffer)
                {
                    kk = kk + st;
                }

                //if(kk == "")
                //{
                //    Console.WriteLine("rip");
                //}

                //Console.WriteLine(kk);

                currentBuffer = new string[lineLenght];
            }

            Console.WriteLine();

        }
    }

    sealed class HexEntry
    {
        public string Offset { get; set; }
        public string Hex { get; set; }
        public string ASCII { get; set; }
    }

}
