using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DOTNET_CHIP_8
{
    public class DotRenderer
    {
        Rectangle[] Pixels = new Rectangle[2048];
        Stopwatch frametimer = new Stopwatch();

        public delegate void FrameRenderedEventHandler(object sender, EventArgs args);
        public event FrameRenderedEventHandler FrameRendered;

        float width = 64;
        float height = 32;
        public int check = 0;
        public int size = 10;

        public long FrameTime;

        Brush PixelON = new SolidColorBrush(Colors.White);
        Brush PixelOFF = new SolidColorBrush(Colors.Black);
        Brush RedPixel = new SolidColorBrush(Colors.Red);

        public DotRenderer()
        {
            for (int i = 0; i < width * height; i++)
            {
                Pixels[i] = (Rectangle)Pixel(i);
            }
        }

        public void RenderPixels(byte[] buffer)
        {
            frametimer.Start();

            for(int i = 0; i < Pixels.Length; i++)
            {
                if(buffer[i] == 1)
                {
                    Pixels[i].Fill = PixelON;
                }
                else
                {
                    Pixels[i].Fill = PixelOFF;
                }
            }

            frametimer.Stop();
            FrameTime = frametimer.ElapsedMilliseconds;
            frametimer.Reset();
            OnFrameRendered();
        }

        public UIElement RenderPort(int px_size)
        {
            Grid port = new Grid();

            foreach (Rectangle px in Pixels)
            {
                port.Children.Add(px);
            }

            return port;
        }

        float yi = 0;
        float xi = 0;
        private UIElement Pixel(float i)
        {
            check++;

            Rectangle px = new Rectangle
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = size,
                Height = size
            };

            if (i % width == 0 && i != 0)
            {
                yi = i / width;
            }

            px.Margin = new Thickness(size * xi, yi * size, 0, 0);

            if (xi < width-1)
            {
                xi++;
            }
            else
            {
                xi = 0;
            }

            return px;
        }

        public void ResizePixels(int size)
        {
            float _xi = 0;
            float _yi = 0;

            for(int i = 0; i < Pixels.Length; i++)
            {
                Rectangle px = Pixels[i];
                px.Width = size;
                px.Height = size;

                if (i % width == 0 && i != 0)
                {
                    _yi = i / width;
                }

                px.Margin = new Thickness(size * _xi, _yi * size, 0, 0);

                Console.WriteLine($"{i} : {size* _xi} = {_yi*size} ");

                if (_xi < width - 1)
                {
                    _xi++;
                }
                else
                {
                    xi = 0;
                }

            }
        }

        protected virtual void OnFrameRendered()
        {
            FrameRendered?.Invoke(this, EventArgs.Empty);
        }

        public void TestFillPixels()
        {
            for(int i = 0; i < Pixels.Length; i++)
            {
                Pixels[i].Fill = RedPixel;
            }
        }

        int _fillindex = 0;
        Random rand = new Random();
        byte _r;
        byte _g;
        byte _b;

        public void SlowFillPixels()
        {
            DispatcherTimer _t = new DispatcherTimer();
            _t.Interval = TimeSpan.FromMilliseconds(50);
            _t.Tick += FillPixel;
            _t.Start();
        }

        private void FillPixel(object sender, EventArgs e)
        {
            if(_fillindex < Pixels.Length)
            {

                _r = (byte)rand.Next(0, 255);
                _g = (byte)rand.Next(0, 255);
                _b = (byte)rand.Next(0, 255);

                Pixels[_fillindex].Fill = new SolidColorBrush(Color.FromRgb(_r, _g, _b));

                _fillindex++;
            }
            
        }
    }
}
