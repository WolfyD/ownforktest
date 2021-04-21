using System;
using System.Drawing;
using System.IO;
using System.Linq;
using vbAccelerator.Components.Win32;

namespace Pic2IcoV3
{
	public static class ImageConverter
	{
		public static bool ConvertImage(string filePath, Settings settings)
		{
			if (OpenImage(filePath, out Bitmap bmp))
			{
				ConvertToIco(bmp, GetOutputFileName(filePath, settings), settings);
			}

			return false;
		}

		private static void ConvertToIco(Bitmap bmp, string outputFile, Settings settings)
		{
			File.Create(outputFile).Close();

			IntPtr Hicon = bmp.GetHicon();
			Icon newIcon = Icon.FromHandle(Hicon);
			FileStream fs = new FileStream(outputFile, FileMode.OpenOrCreate);
			newIcon.Save(fs);
			fs.Close();

			IconDeviceImageCollection coll = new IconDeviceImageCollection();

			using (IconEx iconex = new IconEx(outputFile))
			{
				
				iconex.Items = new IconDeviceImageCollection(new IconDeviceImage[settings.Sizes.Count()]);

				if (settings.ReplaceColor)
				{
					Color orig = settings.OriginalColor;
					Color repl = settings.ReplacementColor;
					int tolerance = settings.ReplacementTolerance;

					using (Graphics g = Graphics.FromImage(bmp))
					{
						#region RIP nicer solution
						/*
						//Todo: I wish I could use this approach with the remap table,
						//but sadly, I can't implement the tolerance with it
						//and since I don't want to use unsave memory management code
						//I'll have to use the slow as balls pixel by pixel recoloring...
						ColorMap[] colorMap = new ColorMap[1];
						colorMap[0] = new ColorMap();
						colorMap[0].OldColor = orig;
						colorMap[0].NewColor = repl;
						ImageAttributes attr = new ImageAttributes();
						attr.SetRemapTable(colorMap);
						// Draw using the color map
						Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
						g.DrawImage(bmp, rect, 0, 0, rect.Width, rect.Height, GraphicsUnit.Pixel, attr);
						*/
						#endregion

						for (int X = 0; X < bmp.Width; X++)
						{
							for (int Y = 0; Y < bmp.Height; Y++)
							{
								Color gp = bmp.GetPixel(X, Y);

								if ((gp.R >= orig.R - tolerance && gp.R <= orig.R + tolerance) &&
									(gp.G >= orig.G - tolerance && gp.G <= orig.G + tolerance) &&
									(gp.B >= orig.B - tolerance && gp.B <= orig.B + tolerance))
								{
									bmp.SetPixel(X, Y, repl);
								}
							}
						}
					}
				}

				if (settings.Sizes.Count() == 0) { settings.Sizes = new Size[] { new Size(bmp.Width, bmp.Height) }; }

				foreach (Size size in settings.Sizes)
				{
					var icon = new IconDeviceImage(size, settings.ColorDepth);

					Bitmap newBmp = new Bitmap(size.Width, size.Height);

					using(Graphics g = Graphics.FromImage(newBmp))
					{
						using (Graphics gg = Graphics.FromImage(bmp))
						{
							g.DrawImage(bmp, new Rectangle(new Point(0, 0), size));
						}
					}

					icon.IconImage = newBmp;

					coll.Add(icon);

					//iconex.Items.Add(icon);
				}

				iconex.Items.Capacity = coll.Count;
				iconex.Items.Clear();
				foreach (var item in coll)
				{
					iconex.Items.Add((IconDeviceImage)item);
				}

				iconex.Save(outputFile);
			}
		}

		private static string GetOutputFileName(string filePath, Settings settings)
		{
			string outputFolder = settings.UseOriginFolder ? Path.GetDirectoryName(filePath) : settings.OutputFolder;
			string fileName = Path.GetFileNameWithoutExtension(filePath);
			return Path.Combine(outputFolder, fileName + ".ico");
		}

		private static bool OpenImage(string filePath, out Bitmap bmp)
		{
			try
			{
				if (File.Exists(filePath))
				{
					bmp = (Bitmap)Bitmap.FromFile(filePath);
					return true;
				}
			}
			catch { }

			bmp = null;
			return false;
		}
	}
}
