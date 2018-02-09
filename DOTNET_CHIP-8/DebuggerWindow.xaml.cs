using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DOTNET_CHIP_8
{
    public unsafe partial class DebuggerWindow : Window
    {
        CHIP_8 CPU;
        DispatcherTimer FetcherLoop;

        //16 general purpose registers(byte), I, two special registers, pc, sptr = 21 in total
        
        private uShortRegister[] uShortRegisters = new uShortRegister[6];
        private byteRegister[] byteRegisters = new byteRegister[16];

        //Hide window close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public DebuggerWindow(CHIP_8 p)
        {
            InitializeComponent();
            Loaded += Debugger_Loaded;
            Closing += Debugger_Closing;

            Console.WriteLine("Opening Debugger");

            CPU = p;
            SetupRegisterEntries();
            PopulateRegisterEntries();

            FetcherLoop = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(16) };
            FetcherLoop.Tick += FetcherLoop_Tick;
            FetcherLoop.Start();
        }

        private void Debugger_Closing(object sender, CancelEventArgs e)
        {
            GC.Collect();
        }

        private void Debugger_Loaded(object sender, RoutedEventArgs e)
        {
            HideCloseButton();
        }

        private void HideCloseButton()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        //Fetch register values and update UI
        private void FetcherLoop_Tick(object sender, EventArgs e)
        {
            //unsigned short registers
            for (int i = 0; i < uShortRegisters.Length; i++)
            {
                if (uShortRegisters[i] != null)
                {
                    Dispatcher.Invoke(new Action(() => uShortRegisters[i].Text = $"{uShortRegisters[i].Name}: {*uShortRegisters[i].Pointer}"));
                }
            }

            //byte registers
            for (int i = 0; i < byteRegisters.Length; i++)
            {
                if (byteRegisters[i] != null)
                {
                    Dispatcher.Invoke(new Action(() => byteRegisters[i].Text = $"{byteRegisters[i].Name}: {*byteRegisters[i].Pointer}"));
                }
            }
        }

        //Take CPU registers and store them in an array to be displayed later
        private void SetupRegisterEntries()
        {
            Console.WriteLine("Setting up Debugger register list");
            fixed(ushort* p = &CPU.opcode)
            {
                uShortRegisters[0] = NewRegisterEntry("opcode", p);
            }
            fixed(ushort * p = &CPU.I)
            {
                uShortRegisters[1] = NewRegisterEntry("I", p);
            }
            fixed (ushort* p = &CPU.pc)
            {
                uShortRegisters[2] = NewRegisterEntry("PC", p);
            }
            fixed (ushort* p = &CPU.stackPtr)
            {
                uShortRegisters[3] = NewRegisterEntry("stackPtr", p);
            }
            fixed (ushort* p = &CPU.delay_timer)
            {
                uShortRegisters[4] = NewRegisterEntry("delay_timer", p);
            }
            fixed (ushort* p = &CPU.sound_timer)
            {
                uShortRegisters[5] = NewRegisterEntry("sound_timer", p);
            }

            for (int i = 0x0; i < CPU.cpu_V.Length; i++)
            {
                fixed(byte* p = &CPU.cpu_V[i])
                {
                    byteRegisters[i] = NewRegisterEntry($"V{i.ToString("X")}", p);
                }
            }
        }

        //Add CPU registers to UI
        private void PopulateRegisterEntries()
        {
            for(int i = 0; i < uShortRegisters.Length; i++)
            {
                if(uShortRegisters[i] != null)
                {
                    RegisterList.Children.Add(uShortRegisters[i]);
                }
            }

            for (int i = 0; i < byteRegisters.Length; i++)
            {
                if (byteRegisters[i] != null)
                {
                    RegisterList.Children.Add(byteRegisters[i]);
                }
            }
        }

        //Create new register entry for ushort types
        private uShortRegister NewRegisterEntry(string registerName, ushort* reference)
        {
            Console.WriteLine($"Creating register entry for {registerName}");

            uShortRegister entry = new uShortRegister() { Text = $"{registerName}" };
            entry.Name = registerName;
            entry.Pointer = reference;
            return entry;
        }

        //Create new register entry for ushort types
        private byteRegister NewRegisterEntry(string registerName, byte* reference)
        {
            Console.WriteLine($"Creating register entry for {registerName}");

            byteRegister entry = new byteRegister() { Text = $"{registerName}" };
            entry.Name = registerName;
            entry.Pointer = reference;
            return entry;
        }
    }

    public unsafe class uShortRegister : TextBlock
    {
        public ushort* Pointer;
    }

    public unsafe class byteRegister : TextBlock
    {
        public byte* Pointer;
    }
}
