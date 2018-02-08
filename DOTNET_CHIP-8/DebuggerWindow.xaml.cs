using System;
using System.Collections.Generic;
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
    public partial class DebuggerWindow : Window
    {
        CHIP_8 CPU;
        DispatcherTimer FetcherLoop;

        //16 general purpose registers, I, VF, two special registers, pc, sptr = 22 in total
        private Register[] AddedRegisters = new Register[22];

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

            Console.WriteLine("Opening Debugger");

            CPU = p;
            SetupRegisterEntries();
            PopulateRegisterEntries();


            FetcherLoop = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000) };
            FetcherLoop.Tick += FetcherLoop_Tick;
            FetcherLoop.Start();
            
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

        private void FetcherLoop_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < AddedRegisters.Length; i++)
            {
                if (AddedRegisters[i] != null)
                {
                    //Console.WriteLine(AddedRegisters[i].Pointer);
                    AddedRegisters[i].Text = $"{AddedRegisters[i].Name}: {AddedRegisters[i].Pointer}";
                }
            }
        }

        private void SetupRegisterEntries()
        {
            Console.WriteLine("Setting up Debugger register list");

            AddedRegisters[0] = NewRegisterEntry("pc", ref CPU.pc);
            AddedRegisters[1] = NewRegisterEntry("romsize", ref CPU.romSize);
        }

        private void PopulateRegisterEntries()
        {
            for(int i = 0; i < AddedRegisters.Length; i++)
            {
                if(AddedRegisters[i] != null)
                {
                    RegisterList.Children.Add(AddedRegisters[i]);
                }
            }
        }

        //Create new register entry
        private Register NewRegisterEntry<Ptr>(string registerName, ref Ptr registerPtr)
        {
            Console.WriteLine($"Creating register entry for {registerName}");

            Register entry = new Register() { Text = $"{registerName}: {registerPtr}" };
            entry.Name = registerName;
            entry.Pointer = registerPtr;
            return entry;
        }
    }

    public class Register : TextBlock
    {
        public object Pointer;
    }
}
