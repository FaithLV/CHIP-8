using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DOTNET_CHIP_8
{
    public class DotRenderer
    {
        public Rectangle[] Pixels = new Rectangle[2048];

        int width = 64;
        int height = 32;

        public DotRenderer()
        {
            for (int i = 0; i < width * height; i++)
            {
                Pixels[i] = Pixel(i);
            }
        }

        public Grid RenderPort(int px_size)
        {
            Grid port = new Grid();

            foreach(Rectangle px in Pixels)
            {
                port.Children.Add(px);
            }

            Console.WriteLine($"Added {width*height} pixels to renderport.");

            return port;
        }

        private Rectangle Pixel(int i)
        {
            int size = 5;

            Rectangle px = new Rectangle();

            px.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            px.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            Console.WriteLine(i);

            if (i < 64)
            {
                px.Fill = new SolidColorBrush(Colors.Red);
                px.Margin = new System.Windows.Thickness(i * size, 0, 0, 0);
            }
            else if(i > 64)
            {
                px.Fill = new SolidColorBrush(Colors.Blue);
                px.Margin = new System.Windows.Thickness((i-64 * size), size, 0, 0);
            }
            

            px.Width = size;
            px.Height = size;

            //px.Fill = new SolidColorBrush(Colors.Red);
            return px;
        }
    }
}
