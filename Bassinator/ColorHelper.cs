using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Text;

namespace Bassinator
{
    class ColorHelper
    {
        public static List<Color> GetGradient(Color[] colors, int gradientSize)
        {
            var gradient = new List<Color>();

            for (int i = 0; i < gradientSize; i++)
            {
                int rAverage = colors[0].R + (int)((colors[1].R - colors[0].R) * i / gradientSize);
                int gAverage = colors[0].G + (int)((colors[1].G - colors[0].G) * i / gradientSize);
                int bAverage = colors[0].B + (int)((colors[1].B - colors[0].B) * i / gradientSize);
                int aAverage = colors[0].A + (int)((colors[1].A - colors[0].A) * i / gradientSize);

                gradient.Add(Color.FromArgb((byte)aAverage, (byte)rAverage, (byte)gAverage, (byte)bAverage));
            }

            return gradient;
        }
    }
}
