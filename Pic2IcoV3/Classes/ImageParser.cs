using System.IO;
using System.Drawing;
using System;
using System.Linq;

namespace Pic2IcoV3
{
	public static class ImageParser
	{
		private static string[] extensions = { ".jpg", ".jpeg", ".bmp", ".png", ".gif", ".tif", ".tiff" };

		public static ImageData GetImageData(string filePath)
		{
			if (File.Exists(filePath) && IsImage(filePath))
			{
				Bitmap bmp = new Bitmap(filePath);

				ImageData data = new ImageData() {
					Name = Path.GetFileName(filePath),
					Path = filePath,
					Width = bmp.Width,
					Height = bmp.Height
				};

				bmp.Dispose();

				return (data);
			}

			return null;
		}

		private static bool IsImage(string filePath)
		{
			string ext = Path.GetExtension(filePath).ToLower();

			if (extensions.Contains(ext)) { return true; }
			/* TODO: Potentially add check based on file header
			// https://en.wikipedia.org/wiki/List_of_file_signatures */
			return false;
		}
	}
}
