using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace DOTNET_CHIP_8
{
    public partial class MainWindow : Window
    {
        CHIP_8 CPUCore = null;
        DotRenderer Renderer = new DotRenderer();
        DispatcherTimer gfxClock = null;
        DispatcherTimer cpuClock = null;

        public MainWindow()
        {
            InitializeComponent();
            AllocConsole(); 

            CPUCore = new CHIP_8();

            gfxClock = new DispatcherTimer();
            gfxClock.Interval = TimeSpan.FromMilliseconds(16.67);
            gfxClock.Tick += GFX_Tick;

            cpuClock = new DispatcherTimer();
            cpuClock.Tick += CPUCycle;

            Dispatcher.Invoke(new Action(() => EmuGrid.Children.Add(Renderer.RenderPort(5))));
            Console.WriteLine($"Pixel buffer size: {Renderer.check}");

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
            //draw

        }


        //DEBUG
        private void SwitchGPU()
        {
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
    }
}
