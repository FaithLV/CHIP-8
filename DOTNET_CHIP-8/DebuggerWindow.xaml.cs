using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DOTNET_CHIP_8
{
    public partial class DebuggerWindow : Window
    {
        CHIP_8 CPU;
        DispatcherTimer FetcherLoop;
        DispatcherTimer MemoryLoop;

        //16 general purpose registers(byte), I, two special registers, pc, sptr = 21 in total
        private uShortRegister[] uShortRegisters = new uShortRegister[6];
        private byteRegister[] byteRegisters = new byteRegister[16];
        private uShortRegister[] stackValues = new uShortRegister[16];

        //Hide window close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private readonly TimeSpan FetcherDelay = TimeSpan.FromMilliseconds(250);
        private readonly TimeSpan MemoryDelay = TimeSpan.FromMilliseconds(1000);

        public DebuggerWindow(CHIP_8 p)
        {
            InitializeComponent();
            Loaded += Debugger_Loaded;
            Closing += Debugger_Closing;

            Console.WriteLine("Opening Debugger");

            CPU = p;
            SetupRegisterEntries();
            PopulateRegisterEntries();

            FetcherLoop = new DispatcherTimer() { Interval = FetcherDelay };
            FetcherLoop.Tick += FetcherLoop_Tick;
            FetcherLoop.Start();

            MemoryLoop = new DispatcherTimer() { Interval = MemoryDelay };
            MemoryLoop.Tick += MemoryLoop_Tick;
            MemoryLoop.Start();
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
        private unsafe void FetcherLoop_Tick(object sender, EventArgs e)
        {
            foreach(var reg in RegisterList.Items)
            {
                if(reg is uShortRegister)
                {
                    uShortRegister item = (uShortRegister)reg;
                    item.Value = *item.Pointer;
                }
                else if(reg is byteRegister)
                {
                    byteRegister item = (byteRegister)reg;
                    item.Value = *item.Pointer;
                }
            }

            foreach (uShortRegister stack in StackList.Items)
            {
                stack.Value = *stack.Pointer;
            }

            Dispatcher.Invoke(new Action(() => RegisterList.Items.Refresh()));
            Dispatcher.Invoke(new Action(() => StackList.Items.Refresh()));
        }

        //Display memory buffer
        private void MemoryLoop_Tick(object sender, EventArgs e)
        {
            HexDump.SetBuffer(ref CPU.memory);
        }

        //Take CPU registers and store them in an array to be displayed later
        private unsafe void SetupRegisterEntries()
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

            //add stack value pointers
            for (int i = 0; i < stackValues.Length; i++)
            {
                fixed(ushort* p = &CPU.stack[i])
                {
                    stackValues[i] = NewRegisterEntry($"S{i}", p);
                }
            }

            Console.WriteLine("Finished setting up register entries.");
            Console.WriteLine($"Total register count: {uShortRegisters.Count() + byteRegisters.Count()}");
        }

        //Add CPU registers to UI
        private void PopulateRegisterEntries()
        {
            for(int i = 0; i < uShortRegisters.Length; i++)
            {
                if(uShortRegisters[i] != null)
                {
                    RegisterList.Items.Add(uShortRegisters[i]);
                }
            }

            for (int i = 0; i < byteRegisters.Length; i++)
            {
                if (byteRegisters[i] != null)
                {
                    RegisterList.Items.Add(byteRegisters[i]);
                }
            }

            for (int i = 0; i < stackValues.Length; i++)
            {
                if(stackValues[i] != null)
                {
                    StackList.Items.Add(stackValues[i]);
                }
            }
        }

        //Create new register entry for ushort types
        private unsafe uShortRegister NewRegisterEntry(string registerName, ushort* reference)
        {
            Console.WriteLine($"Creating register entry for {registerName}");

            uShortRegister entry = new uShortRegister();
            entry.Name = registerName;
            entry.Pointer = reference;
            return entry;
        }

        //Create new register entry for ushort types
        private unsafe byteRegister NewRegisterEntry(string registerName, byte* reference)
        {
            Console.WriteLine($"Creating register entry for {registerName}");

            byteRegister entry = new byteRegister(); ;
            entry.Name = registerName;
            entry.Pointer = reference;
            return entry;
        }

        private void Freeze_Click(object sender, RoutedEventArgs e)
        {
            CPU.isPaused = !CPU.isPaused;
            Console.WriteLine($"CPU pause state set: {CPU.isPaused}");
        }

        private void ASM_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public sealed class uShortRegister
    {
        public string Name { get; set; }
        public unsafe ushort* Pointer { get; set; }
        public ushort Value { get; set; }
    }

    public sealed class byteRegister
    {
        public string Name { get; set; }
        public unsafe byte* Pointer { get; set; }
        public ushort Value { get; set; }
    }
}
