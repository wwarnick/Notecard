using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NotecardFront
{
	public class Tools
	{
		public static SolidColorBrush colorToSum(byte red, byte green, byte blue, int sum)
		{
			int total = 255 * 3 - red - green - blue;

			double mod = (255d * 3d - (double)sum) / (double)total;

			red = (byte)Math.Round(255d - ((255d - (double)red) * mod));
			green = (byte)Math.Round(255d - ((255d - (double)green) * mod));
			blue = (byte)Math.Round(255d - ((255d - (double)blue) * mod));

			return new SolidColorBrush(Color.FromRgb(red, green, blue));
		}
	}
}
