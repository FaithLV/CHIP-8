using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DOTNET_CHIP_8
{
    /// <summary>
    /// Interaction logic for DebuggerWindow.xaml
    /// </summary>
    public partial class DebuggerWindow : Window
    {
        CHIP_8 CPU;
        DispatcherTimer FetcherLoop;

        public DebuggerWindow(CHIP_8 p)
        {
            InitializeComponent();
            CPU = p;
            FetcherLoop = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(1000) };
            FetcherLoop.Tick += FetherLoop_Tick;
            FetcherLoop.Start();
            
        }

        private void FetherLoop_Tick(object sender, EventArgs e)
        {
            TestBuffer();
        }

        private void TestBuffer()
        {
            if(CPU != null)
            {
                test.Text = CPU.romSize.ToString();
            }
            
        }
    }
}
