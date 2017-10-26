using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Xml;
using System.Linq;
using Ookii.Dialogs.Wpf;

namespace DOTNET_CHIP_8
{
    public partial class MainWindow : Window
    {
        CHIP_8 CPUCore = null;
        DotRenderer Renderer = new DotRenderer();
        DispatcherTimer gfxClock = null;
        DispatcherTimer cpuClock = null;

        private GlobalKeyboardHook KeyboardHook;
        ObservableCollection<int> Buttons = new ObservableCollection<int>();

        string[] Bindings = new string[16];

        private static int[] KeyCodes;
        private static string[] KeyNames;

        public MainWindow()
        {
            InitializeComponent();

            AllocConsole();
            Thread.Sleep(5);

            CPUCore = new CHIP_8();
            InitializeCPUClock();

            InitializeGFXClock();
            Console.WriteLine($"Pixel buffer size: {Renderer.check}");

            ReadConfiguration();

            InitializeKeyboardHook();
            InitializeInputDriver();
        }

        //Window Stuff

        //DLLs for console window
        [DllImport("Kernel32")]
        private static extern void AllocConsole();
        private void OpenConsole()
        {
            AllocConsole();
            Thread.Sleep(1);
            Console.WriteLine("Booting CHIP-8");
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string gamePath = files[0];
            Console.WriteLine($"Loading game: {gamePath}");

            byte[] game = File.ReadAllBytes(gamePath);
            LoadNewGame(game);

        }

        //Button Region

        private void TestRenderer_Button(object sender, RoutedEventArgs e)
        {
            Renderer.TestFillPixels();
        }

        private void SlowGFXBuffer_Button(object sender, RoutedEventArgs e)
        {
            Renderer.SlowFillPixels();
        }

        private void LoadROM_Button(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog romDialog = new VistaOpenFileDialog();
            romDialog.Title = "Navigate to game file!";
            byte[] gameBuffer = null;

            if(romDialog.ShowDialog().Value == true)
            {
                gameBuffer = File.ReadAllBytes(romDialog.FileName);
                LoadNewGame(gameBuffer);
            }
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

        private void DumpOpCodes_Button(object sender, RoutedEventArgs e)
        {
            List<string> log = CPUCore.OpCodeLog;

            StreamWriter writer = new StreamWriter($"{AppDomain.CurrentDomain.BaseDirectory}/opcodes.log");
            foreach (var entry in log)
            {
                writer.WriteLine(entry);
            }
            writer.Close();
            Console.WriteLine($"Written {log.Count} entries to OPCode log");
        }

        private void SaveState_Button(object sender, RoutedEventArgs e)
        {
            SaveStateManager manager = new SaveStateManager(CPUCore);
            manager.SaveAState("test");
        }

        private void LoadState_Button(object sender, RoutedEventArgs e)
        {

        }

        private void DotRenderer_FrameRendered(object sender, EventArgs args)
        {
            FrameTimerDisplay.Text = $"{Renderer.FrameTime}ms";
        }

        //Initializations

        private void InitializeKeyboardHook()
        {
            KeyboardHook = new GlobalKeyboardHook();
            KeyboardHook.KeyboardPressed += On_KeyPressed;
            Console.WriteLine("Keyboard Hooked");
        }

        private void InitializeGFXClock()
        {
            gfxClock = new DispatcherTimer();
            gfxClock.Interval = TimeSpan.FromMilliseconds(16.67);
            //gfxClock.Interval = TimeSpan.FromMilliseconds(0);
            gfxClock.Tick += GFX_Tick;

            Dispatcher.Invoke(new Action(() => RenderPort.Children.Add(Renderer.RenderPort(5))));

            Renderer.FrameRendered += DotRenderer_FrameRendered;
        }

        private void InitializeCPUClock()
        {
            cpuClock = new DispatcherTimer();
            //cpuClock.Interval = TimeSpan.FromMilliseconds(1.851851851851852);
            cpuClock.Interval = TimeSpan.FromMilliseconds(0.5);
            cpuClock.Tick += CPUCycle;
        }

        private void InitializeInputDriver()
        {
            CreateKeyArrays();
            Buttons.CollectionChanged += Buttons_Changed;
        }

        //--

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
            CPUCore.EmulateCycle();
        }

        //GFX Timer tick
        private void GFX_Tick(object sender, EventArgs e)
        {
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

        private void ReadConfiguration()
        {
            IniFile config = new IniFile("emuconfig.ini");

            double speed = Double.Parse(config.Read("Interval", "CPUCore"));
            Console.WriteLine($"CPU Clock Interval: cycle per {speed}ms ");
            cpuClock.Interval = TimeSpan.FromMilliseconds(speed);

            double gfx_speed = Double.Parse(config.Read("Interval", "GFX"));
            Console.WriteLine($"GFX Buffer will be drawn every {gfx_speed}ms");
            gfxClock.Interval = TimeSpan.FromMilliseconds(gfx_speed);

            int pxsize = Int32.Parse(config.Read("PixelSize", "DOTRenderer"));
            Renderer.size = pxsize;
            Console.WriteLine($"DOTRenderer: Pixel size = {Renderer.size}");

        }

        private void CreateKeyArrays()
        {
            string VirtualKeysXML = AppDomain.CurrentDomain.BaseDirectory + @"\data\VirtualKeys.xml";

            List<int> codes = new List<int>();
            List<string> names = new List<string>();

            using (XmlReader reader = XmlReader.Create(VirtualKeysXML))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "VirtualKeys":
                                break;
                            case "Key":
                                codes.Add(Convert.ToInt32(reader["value"]));
                                names.Add(reader["equivalent"]);
                                break;
                        }
                    }
                }
            }

            KeyCodes = codes.ToArray();
            KeyNames = names.ToArray();
            Console.WriteLine($"Added {KeyCodes.Length} virtual keys!");
        }

        [STAThread]
        private void On_KeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
            {
                if (!Buttons.Contains(e.KeyboardData.VirtualCode))
                {
                    Buttons.Add(e.KeyboardData.VirtualCode);
                }

            }
            else
            {
                Buttons.Remove(e.KeyboardData.VirtualCode);
            }
        }

        private void Buttons_Changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            //= on press
            if (e.NewItems != null)
            {
                foreach (int key in e.NewItems)
                {
                    //listen only to supported keys
                    if (KeyCodes.Contains(key))
                    {
                        int loc = Array.IndexOf(KeyCodes, key);
                        string keyname = KeyNames[loc];
                        string[] _binds = ControlXMLParser.Binds(keyname);

                        if (_binds.Length >= 0)
                        {
                            var i = 0;
                            foreach (string bind in _binds)
                            {
                                ushort _key = (ushort)Convert.ToUInt32(_binds[i]);
                                CPUCore.PressButton(_key);
                                i++;
                            }
                        }
                    }
                }
            }

            //= on depress
            if(e.OldItems != null)
            {
                foreach (int key in e.OldItems)
                {
                    if (KeyCodes.Contains(key))
                    {
                        int loc = Array.IndexOf(KeyCodes, key);
                        string keyname = KeyNames[loc];
                        string[] _binds = ControlXMLParser.Binds(keyname);

                        if (_binds.Length >= 0)
                        {
                            var i = 0;
                            foreach (string bind in _binds)
                            {
                                ushort _key = (ushort)Convert.ToUInt32(_binds[i]);
                                CPUCore.UnpressButton(_key);
                                i++;
                            }
                        }
                    }
                }
            }

        }
    }
}
