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
        int xi = 0;
        public int check = 0;

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
            int yi = 0;
            check++;

            Rectangle px = new Rectangle();
            px.Fill = new SolidColorBrush(Colors.Red);

            px.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            px.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            if( i% 64 == 0)
            {
                yi = i / 64;
            }

            if (xi < 64)
            {
                xi++;
                px.Margin = new System.Windows.Thickness(size * xi, yi*size, 0, 0);
            }
            else
            {
                xi = 0;
            }

            px.Width = size;
            px.Height = size;

            //px.Fill = new SolidColorBrush(Colors.Red);
            return px;
        }
    }
}
