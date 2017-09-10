using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DOTNET_CHIP_8
{
    public partial class MainWindow : Window
    {
        CHIP_8 CPUCore = null;
        DispatcherTimer gfxClock = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(16.67) };

        public MainWindow()
        {
            InitializeComponent();
            CPUCore = new CHIP_8();
            gfxClock.Tick += GFX_Tick;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string gamePath = files[0];
            Console.WriteLine($"Loading game: {gamePath}");

            byte[] game = File.ReadAllBytes(gamePath);
            LoadNewGame(game);

        }

        private void LoadNewGame(byte[] game)
        {
            //CPUCore = new CHIP_8();
            CPUCore.LoadGame(game);
            gfxClock.Start();
        }

        private void GFX_Tick(object sender, EventArgs e)
        {
            byte[] gfxarray = CPUCore.gfx;
            MemoryStream ms = new MemoryStream(gfxarray);
            //BitmapImage buffer = BitmapImage.Strea
            //RenderWindow.Source = 
        }
    }
}
