using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace DOTNET_CHIP_8
{
    public partial class MainWindow : Window
    {
        CHIP_8 CPUCore = null;
        DotRenderer Renderer = new DotRenderer();
        DispatcherTimer gfxClock = null;
        DispatcherTimer cpuClock = null;

        string[] Bindings = new string[16];

        public MainWindow()
        {
            InitializeComponent();
            AllocConsole();
            Thread.Sleep(5);

            CPUCore = new CHIP_8();
            
            gfxClock = new DispatcherTimer();
            gfxClock.Interval = TimeSpan.FromMilliseconds(16.67);
            gfxClock.Tick += GFX_Tick;

            cpuClock = new DispatcherTimer();
            cpuClock.Interval = TimeSpan.FromMilliseconds(1.851851851851852);
            cpuClock.Tick += CPUCycle;

            Dispatcher.Invoke(new Action(() => EmuGrid.Children.Add(Renderer.RenderPort(5))));
            Console.WriteLine($"Pixel buffer size: {Renderer.check}");

            ReadConfiguration();

        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string gamePath = files[0];
            Console.WriteLine($"Loading game: {gamePath}");

            byte[] game = File.ReadAllBytes(gamePath);
            LoadNewGame(game);

        }

        //Load ROM into memory
        private void LoadNewGame(byte[] game)
        {
            CPUCore.LoadGame(game);
            cpuClock.Start();
            gfxClock.Start();
        }

        //CPU Timer tick
        private void CPUCycle(object sender, EventArgs e)
        {
            SwitchCPU();
            CPUCore.EmulateCycle();
        }

        //GFX Timer tick
        private void GFX_Tick(object sender, EventArgs e)
        {
            SwitchGPU();

            byte[] gfxarray = CPUCore.gfx;
            DrawGFXBuffer(gfxarray);
        }

        private void DrawGFXBuffer(byte[] buffer)
        {
            Renderer.RenderPixels(buffer);
        }


        //DEBUG
        private void SwitchGPU()
        {
            return;

            if (GPU_Marker.Visibility == Visibility.Hidden)
            {
                Dispatcher.Invoke(new Action(() => GPU_Marker.Visibility = Visibility.Visible));
            }
            else
            {
                Dispatcher.Invoke(new Action(() => GPU_Marker.Visibility = Visibility.Hidden));
            }
        }

        private void SwitchCPU()
        {
            return;

            if (CPU_Marker.Visibility == Visibility.Hidden)
            {
                Dispatcher.Invoke(new Action(() => CPU_Marker.Visibility = Visibility.Visible));
            }
            else
            {
                Dispatcher.Invoke(new Action(() => CPU_Marker.Visibility = Visibility.Hidden));
            }
        }

        //DLLs for console window
        [DllImport("Kernel32")]
        private static extern void AllocConsole();
        private void OpenConsole()
        {
            AllocConsole();
            Thread.Sleep(1);
            Console.WriteLine("Booting CHIP-8");
        }

        private void DumpGFXBuffer_Button(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("-----------------------RAW GFX Buffer:");
            byte[] gfxarray = CPUCore.gfx;
            foreach (byte px in gfxarray)
            {
                Console.Write($"{px}, ");
            }
            Console.WriteLine();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
        }

        private void DumpOpCodes_Button(object sender, RoutedEventArgs e)
        {
            List<string> log = CPUCore.OpCodeLog;

            StreamWriter writer = new StreamWriter($"{AppDomain.CurrentDomain.BaseDirectory}/opcodes.log");
            foreach(var entry in log)
            {
                writer.WriteLine(entry);
            }
            writer.Close();
            Console.WriteLine($"Written {log.Count} entries to OPCode log");
        }

        private void ReadConfiguration()
        {
            IniFile config = new IniFile("emuconfig.ini");

            double freq = Double.Parse(config.Read("Frequency", "CPUCore"));
            Console.WriteLine($"CPU Frequency: {freq}Hz");
            cpuClock.Interval = TimeSpan.FromMilliseconds(1000/freq);

            int pxsize = Int32.Parse(config.Read("PixelSize", "DOTRenderer"));
            Renderer.size = pxsize;
            Console.WriteLine($"DOTRenderer: Pixel size = {Renderer.size}");


        }
    }
}
