using System.Windows.Controls;
using System.Windows.Media;
using _Size = System.Drawing.Size;

namespace Pic2IcoV3
{
	public static class Misc
	{
		public static System.Windows.Forms.ColorDepth GetColorDepth(int index)
		{
			switch (index)
			{
				case 4: return System.Windows.Forms.ColorDepth.Depth4Bit;
				case 3: return System.Windows.Forms.ColorDepth.Depth8Bit;
				case 2: return System.Windows.Forms.ColorDepth.Depth16Bit;
				case 1: return System.Windows.Forms.ColorDepth.Depth24Bit;
				case 0:
				default: return System.Windows.Forms.ColorDepth.Depth32Bit;
			}
		}

		public static System.Drawing.Color ColorFromBrush(Brush brush)
		{
			var mediaColor = ((SolidColorBrush)brush).Color;
			System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
			return drawingColor;
		}

		public static SolidColorBrush BrushFromColor(System.Drawing.Color color)
		{
			var mediaColor = Color.FromArgb(color.A, color.R, color.G, color.B);
			var brush = new SolidColorBrush(mediaColor);
			return brush;
		}

		public static bool CheckBox(CheckBox checkBox, out _Size size)
		{
			size = new _Size();

			if (int.TryParse(checkBox.Content.ToString(), out int width))
			{
				size = new _Size(width, width);
			}

			return checkBox.IsChecked == true;
		}

	}
}
