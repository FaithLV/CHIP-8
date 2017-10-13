﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DOTNET_CHIP_8
{
    public class DotRenderer
    {
        Rectangle[] Pixels = new Rectangle[2048];
        Stopwatch frametimer = new Stopwatch();

        public delegate void FrameRenderedEventHandler(object sender, EventArgs args);
        public event FrameRenderedEventHandler FrameRendered;

        int width = 64;
        int height = 32;
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


        int yi = 0;
        int xi = 0;
        private UIElement Pixel(int i)
        {
            check++;

            Rectangle px = new Rectangle
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            if (i % width == 0 && i != 0)
            {
                yi = i / width;
                Console.WriteLine($"Switching to row {yi} with pixel {i}");
            }

            if (xi < width-1)
            {
                xi++;
            }
            else
            {
                Console.WriteLine($"Switching xi with pixel {i}");
                xi = 0;
            }

            px.Margin = new Thickness(size * xi, yi * size, 0, 0);

            px.Width = size;
            px.Height = size;
            return px;
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
    }
}
