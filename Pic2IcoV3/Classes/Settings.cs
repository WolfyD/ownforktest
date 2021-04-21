using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Pic2IcoV3
{
	public class Settings
	{
		public bool UseOriginFolder { get; set; }
		public string OutputFolder { get; set; }
		public ColorDepth ColorDepth { get; set; }
		public IEnumerable<Size> Sizes { get; set; }
		public bool ReplaceColor { get; set; }
		public Color OriginalColor { get; set; }
		public Color ReplacementColor { get; set; }
		public int ReplacementTolerance { get; set; }
	}
}
