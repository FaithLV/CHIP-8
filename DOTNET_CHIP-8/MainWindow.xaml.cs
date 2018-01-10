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
        DotRenderer Renderer = null;
        SaveStateManager StateManager = null;
        DispatcherTimer gfxClock = null;
        DispatcherTimer cpuClock = null;

        private GlobalKeyboardHook KeyboardHook;
        ObservableCollection<int> Buttons = new ObservableCollection<int>();

        string[] Bindings = new string[16];

        private static int[] KeyCodes;
        private static string[] KeyNames;

        private int[] FlashFlags = new int[] { 0, 0 };

        public MainWindow()
        {
            InitializeComponent();

            AllocConsole();
            Thread.Sleep(5);

            FreshChip();

            ScanROMs();

            InitializeKeyboardHook();
            InitializeInputDriver();

            PopulateSaveStates();
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

        private void ScanROMs()
        {
            foreach (var rom in Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}\\roms\\"))
            {
                MenuItem romItem = new MenuItem();
                romItem.Header = Path.GetFileName(rom);
                romItem.Click += RomItem_Click;
                RomLibraryTab.Items.Add(romItem);
            }
        }

        private void RomItem_Click(object sender, RoutedEventArgs e)
        {
            var obj = (MenuItem)sender;

            byte[] gameBuffer = File.ReadAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\roms\\{obj.Header}");
            LoadNewGame(gameBuffer);
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

        private void SaveNewState_Button(object sender, RoutedEventArgs e)
        {
            uint counter = 1;
            while(File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}\\savestates\\State {counter}.mem"))
            {
                counter++;
            }

            StateManager.SaveAState($"State {counter}");

            MenuItem Sitem = new MenuItem();
            Sitem.Header = $"State {counter}";
            Sitem.Click += SaveState_Click;
            SaveStateTab.Items.Add(Sitem);

            MenuItem Litem = new MenuItem();
            Litem.Header = $"State {counter}";
            Litem.Click += LoadState_Click;
            LoadStateTab.Items.Add(Litem);

        }

        private void Menu_Pause_Click(object sender, RoutedEventArgs e)
        {
            CPUCore.isPaused = !CPUCore.isPaused;
        }

        private void GFX_ResizePixels(object sender, RoutedEventArgs e)
        {
            Renderer.ResizePixels(15);
        }

        private void DotRenderer_FrameRendered(object sender, EventArgs args)
        {
            FrameTimerDisplay.Text = $"{Renderer.FrameTime}ms";
        }

        private void PopulateSaveStates()
        {
            foreach(string file in Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}\\savestates\\"))
            {
                string _file = file.Replace($"{AppDomain.CurrentDomain.BaseDirectory}\\savestates\\", String.Empty);
                _file = _file.Replace(".mem", String.Empty);

                MenuItem Sitem = new MenuItem();
                Sitem.Header = _file;
                Sitem.Click += SaveState_Click;
                SaveStateTab.Items.Add(Sitem);

                MenuItem Litem = new MenuItem();
                Litem.Header = _file;
                Litem.Click += LoadState_Click;
                LoadStateTab.Items.Add(Litem);
            }
        }

        private void LoadState_Click(object sender, RoutedEventArgs e)
        {
            MenuItem obj = (MenuItem)sender;
            StateManager.LoadAState(obj.Header.ToString());
        }

        private void SaveState_Click(object sender, RoutedEventArgs e)
        {
            MenuItem obj = (MenuItem)sender;
            StateManager.SaveAState(obj.Header.ToString());
        }

        //Initializations

        private void FreshChip()
        {
            CPUCore = new CHIP_8();
            InitializeCPUClock();

            InitializeGFXClock();
            Console.WriteLine($"Pixel buffer size: {Renderer.check}");

            StateManager = new SaveStateManager(CPUCore);

            ReadConfiguration();
        }

        private void InitializeKeyboardHook()
        {
            KeyboardHook = new GlobalKeyboardHook();
            KeyboardHook.KeyboardPressed += On_KeyPressed;
            Console.WriteLine("Keyboard Hooked");
        }

        private void InitializeGFXClock()
        {
            Renderer = new DotRenderer(this);

            gfxClock = new DispatcherTimer();
            gfxClock.Interval = TimeSpan.FromMilliseconds(16.67);
            gfxClock.Tick += GFX_Tick;

            Dispatcher.Invoke(new Action(() => RenderPort.Children.Add(Renderer.RenderPort(5))));

            Renderer.FrameRendered += DotRenderer_FrameRendered;
        }

        private void InitializeCPUClock()
        {
            cpuClock = new DispatcherTimer();
            cpuClock.Interval = TimeSpan.FromMilliseconds(0.5);
            cpuClock.Tick += CPUCycle;
            CPUCore.CycleFinished += CPUCore_CycleFinished;
        }

        private void CPUCore_CycleFinished(object sender, EventArgs args)
        {
            CycleTimerDisplay.Text = $"Cycle: {CPUCore.CycleTime}";
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
            FreshChip();

            CPUCore.LoadGame(game);
            cpuClock.Start();
            gfxClock.Start();
        }

        //CPU Timer tick
        private void CPUCycle(object sender, EventArgs e)
        {
            if (FlashFlags[0] == 1) { SwitchCPU(); }
            CPUCore.EmulateCycle();
        }

        //GFX Timer tick
        private void GFX_Tick(object sender, EventArgs e)
        {
            byte[] gfxarray = CPUCore.gfx;
            if (FlashFlags[1] == 1){ SwitchGPU(); }
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

            double gfxspeed = Double.Parse(config.Read("Interval", "GFX_XXX"));
            Console.WriteLine($"GFX Clock Interval: frame per {gfxspeed}ms ");
            gfxClock.Interval = TimeSpan.FromMilliseconds(speed);

            FlashFlags[0] = Int32.Parse(config.Read("FlashCPUCycle", "DebugFlags"));
            FlashFlags[1] = Int32.Parse(config.Read("FlashGPUCycle", "DebugFlags"));
            Console.WriteLine($"FlashCPUCycle = {FlashFlags[0]}");
            Console.WriteLine($"FlashCPUCycle = {FlashFlags[1]}");
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
