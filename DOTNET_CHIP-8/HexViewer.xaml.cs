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
            //Release all items
            Dump.Items.Clear();
            //HexEntryList = new List<HexEntry>();

            //skip empty buffers
            if (buffer.Length == 0)
            {
                return;
            }

            string hexDump = BytesToHexBuffer(ref buffer);
            string[] splitHex = hexDump.Split(' ');
            string[] currentBuffer = new string[lineLenght];

            //Loop through each line based on lineLenght variable
            for (int i = 0; i < splitHex.Length - 1; i = (lineLenght - 1) + i + 1)
            {
                //set current buffer to size of lineLenght
                for (int j = 0; j < lineLenght; j++)
                {
                    currentBuffer[j] = (splitHex[i + j].ToString());
                }

                //current HEX buffer as string
                string hexbuffer = String.Join(String.Empty, currentBuffer);

                //Create a hex entry
                HexEntry currentEntry = new HexEntry();
                currentEntry.Hex = String.Join(" ", currentBuffer);
                currentEntry.Offset = i.ToString("X");

                //Encode as ASCII
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < hexbuffer.Length; j += 2)
                {
                    string hs = hexbuffer.Substring(j, 2);

                    if(Convert.ToChar(Convert.ToUInt32(hs, 16)) == 0xA)
                    {
                        sb.Append("· ");
                    }
                    else if(hs == "00")
                    {
                        sb.Append("· ");
                    }
                    else
                    {
                        sb.Append(Convert.ToChar(Convert.ToUInt32(hs, 16)));
                        sb.Append(" ");
                    }
                }

                currentEntry.ASCII = sb.ToString();

                //Console.WriteLine($"{currentEntry.Offset} {currentEntry.Hex} {currentEntry.ASCII}");
                //HexEntryList.Add(currentEntry);
                Dump.Items.Add(currentEntry);

                currentBuffer = new string[lineLenght];
            }
        }
    }

    sealed class HexEntry
    {
        public string Offset { get; set; }
        public string Hex { get; set; }
        public string ASCII { get; set; }
    }

}
