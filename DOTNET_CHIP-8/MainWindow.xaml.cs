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
using OpenTkControl;
using SharpDX.XInput;
using System.ComponentModel;
using System.Management;

namespace DOTNET_CHIP_8
{
    public partial class MainWindow : Window
    {
        private string SelectedRenderer = "DotRenderer";

        CHIP_8 CPUCore = null;
        SaveStateManager StateManager = null;
        DispatcherTimer gfxClock = null;
        DispatcherTimer cpuClock = null;

        DotRenderer DOTRenderer = null;
        UiOpenTkControl GLControl = null;

        private GlobalKeyboardHook KeyboardHook;
        ObservableCollection<int> Buttons = new ObservableCollection<int>();

        string[] Bindings = new string[16];

        private static int[] KeyCodes;
        private static string[] KeyNames;

        private int[] FlashFlags = new int[] { 0, 0 };
        private bool disableGFXBuffer = false;

        private string CurrentHash = null;

        //Current xInput controller
        public Controller xController;
        //Events for USB device detection
        private ManagementEventWatcher mwe_deletion;
        private ManagementEventWatcher mwe_creation;
        //Controller input listener thread
        private BackgroundWorker xListener = new BackgroundWorker();
        private int PollingRate = 100;
        Dictionary<string, ushort> GamepadBinds;

        public MainWindow()
        {
            InitializeComponent();

            AllocConsole();
            Thread.Sleep(5);

            ReadGraphicsAPI();

            FreshChip();

            ScanROMs();

            InitializeKeyboardHook();
            InitializeInputDriver();

            PopulateSaveStates();

            InitializeXInput();
        }

        private void InitializeXInput()
        {
            Console.WriteLine("XInput Initialization");
            getCurrentController();
            xListener.DoWork += xListener_DoWork;

            //detect new USB device
            WqlEventQuery q_creation = new WqlEventQuery();
            q_creation.EventClassName = "__InstanceCreationEvent";
            q_creation.WithinInterval = new TimeSpan(0, 0, 2);
            q_creation.Condition = @"TargetInstance ISA 'Win32_USBControllerdevice'";
            mwe_creation = new ManagementEventWatcher(q_creation);
            mwe_creation.EventArrived += new EventArrivedEventHandler(USBEventArrived);
            mwe_creation.Start();

            //detect USB device deletion
            WqlEventQuery q_deletion = new WqlEventQuery();
            q_deletion.EventClassName = "__InstanceDeletionEvent";
            q_deletion.WithinInterval = new TimeSpan(0, 0, 2);
            q_deletion.Condition = @"TargetInstance ISA 'Win32_USBControllerdevice'  ";
            mwe_deletion = new ManagementEventWatcher(q_deletion);
            mwe_deletion.EventArrived += new EventArrivedEventHandler(USBEventArrived);
            mwe_deletion.Start();
        }

        private void CreateGamepadBindings(string gameHash)
        {
            GamepadBinds = new Dictionary<string, ushort>();
            GamepadBinds.Add("DPadUp", 1);
            GamepadBinds.Add("DPadDown", 4);

            if (File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}profiles\\{gameHash}"))
            {

            }
        }

        private void USBEventArrived(object sender, EventArrivedEventArgs e)
        {
            //Detect when USB devices change and rescan XInput devices
            getCurrentController();
        }

        private void xListener_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("xListener Started");

            XInputDevice xDevice = new XInputDevice();
            var previousState = xController.GetState();

            string lastKey = "None";

            while (xController.IsConnected)
            {
                var buttons = xController.GetState().Gamepad.Buttons;

                //Check for buttons here!
                if (xDevice.getPressedButton(buttons) != "None")
                {
                    string currentKey = xDevice.getPressedButton(buttons);
 
                    if(currentKey != lastKey)
                    {
                        ControllerUnsetButton(lastKey);
                        ControllerSendButton(currentKey);
                        lastKey = currentKey;
                    }
                    else
                    {
                        lastKey = currentKey;
                    }
                }
                else
                {
                    ControllerUnsetButton(lastKey);
                }

                Thread.Sleep(PollingRate);
            }

            Console.WriteLine("Disposing of xListener thread!");
        }

        private void ControllerSendButton(string key)
        {
            if (GamepadBinds != null && GamepadBinds.ContainsKey(key))
            {
                CPUCore.PressButton(GamepadBinds[key]);
            }
        }

        private void ControllerUnsetButton(string key)
        {
            if (GamepadBinds != null && GamepadBinds.ContainsKey(key))
            {
                CPUCore.UnpressButton(GamepadBinds[key]);
            }
        }

        private void getCurrentController()
        {
            //Checks, if controller is connected before detecting a new controller
            bool wasConnected = false;
            if (xController != null)
            {
                wasConnected = true;
            }

            XInputDevice getDevice = new XInputDevice();
            xController = getDevice.getActiveController();

            if (xController != null)
            {
                if (wasConnected == false)
                {
                    //When new a controller is detected
                    Console.WriteLine("New XInput controller has been detected and has a listener has been attached.");

                    Console.WriteLine("Starting xListener thread!");
                    xListener.RunWorkerAsync();
                }
            }
            else
            {
                Console.WriteLine("No controllers detected");
                if (wasConnected == true)
                {
                    Console.WriteLine("XInput controller was unplugged.");
                }
            }
        }

        private void ReadGraphicsAPI()
        {
            IniFile config = new IniFile("emuconfig.ini");

            //Read G_API from cfg
            string gpuapi = config.Read("API", "Renderer");
            Console.WriteLine($"Loaded GraphicsAPI: {gpuapi}");
            SelectedRenderer = gpuapi;
            foreach (MenuItem item in RendererList.Items)
            {
                if ((string)item.Header == gpuapi)
                {
                    item.IsChecked = true;
                    item.IsEnabled = false;
                }
            }
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
            DOTRenderer.TestFillPixels();
        }

        private void SlowGFXBuffer_Button(object sender, RoutedEventArgs e)
        {
            DOTRenderer.SlowFillPixels();
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
            Console.WriteLine("depreciated");

            //List<string> log = CPUCore.OpCodeLog;

            //StreamWriter writer = new StreamWriter($"{AppDomain.CurrentDomain.BaseDirectory}/opcodes.log");
            //foreach (var entry in log)
            //{
            //    writer.WriteLine(entry);
            //}
            //writer.Close();
            //Console.WriteLine($"Written {log.Count} entries to OPCode log");
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
            DOTRenderer.ResizePixels(15);
        }

        private void DotRenderer_FrameRendered(object sender, EventArgs args)
        {
            FrameTimerDisplay.Text = $"{DOTRenderer.FrameTime}ms";
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
            CPUCore = new CHIP_8(this);
            InitializeCPUClock();

            if (SelectedRenderer == "DotRenderer")
            {
                Console.WriteLine("Starting with DotRenderer");
                InitializeDotRenderer();
                Console.WriteLine($"Pixel buffer size: {DOTRenderer.check}");
            }
            else if (SelectedRenderer == "OpenGL")
            {
                Console.WriteLine("Starting with OpenGL");

            }
            
            StateManager = new SaveStateManager(CPUCore);
            ReadConfiguration();
        }

        private void InitializeKeyboardHook()
        {
            KeyboardHook = new GlobalKeyboardHook();
            KeyboardHook.KeyboardPressed += On_KeyPressed;
            Console.WriteLine("Keyboard Hooked");
        }

        private void InitializeDotRenderer()
        {
            DOTRenderer = new DotRenderer(this);

            gfxClock = new DispatcherTimer();
            gfxClock.Interval = TimeSpan.FromMilliseconds(16.67);
            gfxClock.Tick += GFX_Tick;

            Dispatcher.Invoke(new Action(() => RenderPort.Children.Add(DOTRenderer.RenderPort(5))));

            DOTRenderer.FrameRendered += DotRenderer_FrameRendered;
        }

        private void InitializeOpenGL()
        {
            GLControl = new UiOpenTkControl();
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
            EmuLogo.Visibility = Visibility.Collapsed;

            string hash = HashProvider.GenerateHash(game);
            CreateGamepadBindings(hash);
            Title = $"Chip 8 Emulator | CRC : ({hash})";
            CurrentHash = hash;

            CPUCore.ResetChip();
            CPUCore.DisableAudio = DisableAudioItem.IsChecked;

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
            if(!disableGFXBuffer)
            {
                //byte[] gfxarray = CPUCore.gfx;
                if (FlashFlags[1] == 1) { SwitchGPU(); }
                DrawGFXBuffer(CPUCore.gfx);
            }
        }

        private void DrawGFXBuffer(byte[] buffer)
        {
            if (SelectedRenderer == "DotRenderer")
            {
                DOTRenderer.RenderPixels(buffer);
            }
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
                        string[] _binds = ControlXMLParser.Binds(keyname, CurrentHash);

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
                        string[] _binds = ControlXMLParser.Binds(keyname, CurrentHash);

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

        private void DisableAudio_Click(object sender, RoutedEventArgs e)
        {
            var obj = (MenuItem)sender;
            CPUCore.DisableAudio = obj.IsChecked;
        }

        private void Renderer_Select(object sender, RoutedEventArgs e)
        {
            var obj = (MenuItem)sender;
            Console.WriteLine($"Setting RendererAPI to {obj.Header}");
            obj.IsEnabled = false;
            SelectedRenderer = obj.Header.ToString();

            foreach (MenuItem option in RendererList.Items)
            {
                if (obj != option)
                {
                    option.IsEnabled = true;
                    option.IsChecked = false;
                } 
            }
        }

        private void Randomizer_Select(object sender, RoutedEventArgs e)
        {
            var obj = (MenuItem)sender;

            if (obj.Header.ToString() == ".NET pseudo-random number generator")
            {

            }
            else if (obj.Header.ToString() == "Atmospheric Noise True Randomizer")
            {

            }
        }

        DebuggerWindow dbg;
        private void ShowDebuggerItem_Button(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;

            if (CPUCore.romSize == 0)
            {
                Console.WriteLine("Can't open debugger without loaded rom.");
                item.IsChecked = false;
                return;
            }

            if(item.IsChecked)
            {
                dbg = new DebuggerWindow(CPUCore);
                dbg.Show();

            }
            else
            {
                dbg.Close();
                dbg = null;
            }
        }

        private void GFXCycleItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            if (item.IsChecked)
            {
                FlashFlags[1] = 1;
            }
            else
            {
                FlashFlags[1] = 0;
            }
        }

        private void CPUCycleItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            if (item.IsChecked)
            {
                FlashFlags[0] = 1;
            }
            else
            {
                FlashFlags[0] = 0;
            }
        }

        private void GFXBufferDisable_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            if (item.IsChecked)
            {
                disableGFXBuffer = true;
            }
            else
            {
                disableGFXBuffer = false;
            }
        }
    }
}
