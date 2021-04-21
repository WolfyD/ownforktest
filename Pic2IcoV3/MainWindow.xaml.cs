using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using _Size = System.Drawing.Size;

namespace Pic2IcoV3
{
	public partial class MainWindow : Window
	{
		#region Variables
		private bool isMouseDown = false;
		private Point mousePosition = new Point();
		private List<string> images = new List<string>();
		private List<object> items = new List<object>();
		private bool isColorOriginal = true;
		private bool isConverting = false;
		private bool stopWork = true;
		private Settings settings = new Settings();
		private Properties.Settings def = Properties.Settings.Default;
		Thread workerThread = null;
		private System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
		{
			Filter = "All files|*.*|Image files|*.jpg;*.jpeg;*.bmp;*.png;*.gif;*.tif;*.tiff",
			FilterIndex = 1,
			Multiselect = true,
			Title = "Open images",
			CheckFileExists = true,
			CheckPathExists = true
		};
		#endregion

		public MainWindow()
		{
			InitializeComponent();
			Loaded += MainWindow_Loaded;
			canvas_Color_ReplaceThis.MouseUp += Canvas_Color_ReplaceThis_MouseUp;
			canvas_Color_ReplaceWithThis.MouseUp += Canvas_Color_ReplaceWithThis_MouseUp;
			cp_ColorPicker.LostFocus += Cp_ColorPicker_LostFocus;
			cp_ColorPicker.Closed += Cp_ColorPicker_Closed;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			cb_ColorTemplate.SelectedIndex = Properties.Settings.Default.s_ColorTemplate;

			LoadSettings();
		}

		private void LoadSettings()
		{
			cb_ColorDepth.SelectedIndex = def.s_ColorDepth;
			cb_UseOriginFolder.IsChecked = def.s_UseOriginalFolder;
			if (!def.s_UseOriginalFolder)
			{
				tb_OutputFolder.Text = def.s_FolderPath;
			}

			canvas_Color_ReplaceThis.Background = Misc.BrushFromColor(def.s_OriginalColor);
			canvas_Color_ReplaceWithThis.Background = Misc.BrushFromColor(def.s_ReplacementColor);

			rb_OriginalSize.IsChecked = def.s_OriginalSize;
			rb_Scale.IsChecked = !def.s_OriginalSize;

			cb_ReplaceColor.IsChecked = def.s_ReplaceColor;
			slider_ColorTolerance.Value = def.s_ReplacementTolerance;

			SetSizes(def.s_Sizes);

			cb_UseOriginFolder.IsChecked = def.s_UseOriginalFolder;
		}

		private void Cp_ColorPicker_Closed(object sender, RoutedEventArgs e)
		{
			HandleColorPicked();
		}

		private void Canvas_Color_ReplaceThis_MouseUp(object sender, MouseButtonEventArgs e)
		{
			isColorOriginal = true;
			var b = canvas_Color_ReplaceThis.Background;
			cp_ColorPicker.SelectedColor = (b is SolidColorBrush) ? ((SolidColorBrush)b).Color : Color.FromArgb(0, 0, 0, 0);
			cp_ColorPicker.Visibility = Visibility.Visible;
		}

		private void Canvas_Color_ReplaceWithThis_MouseUp(object sender, MouseButtonEventArgs e)
		{
			isColorOriginal = false;
			var b = canvas_Color_ReplaceWithThis.Background;
			cp_ColorPicker.SelectedColor = (b is SolidColorBrush) ? ((SolidColorBrush)b).Color : Color.FromArgb(0, 0, 0, 0);
			cp_ColorPicker.Visibility = Visibility.Visible;
		}

		private void Cp_ColorPicker_LostFocus(object sender, RoutedEventArgs e)
		{
			HandleColorPicked();
		}

		private void Header_MouseDown(object sender, MouseButtonEventArgs e)
		{
			mousePosition = e.GetPosition(this);
			isMouseDown = true;
		}

		private void Header_MouseUp(object sender, MouseButtonEventArgs e)
		{
			isMouseDown = false;
			Header.ReleaseMouseCapture();
		}

		private void Header_MouseMove(object sender, MouseEventArgs e)
		{
			if (isMouseDown)
			{
				var left = e.GetPosition(null).X - mousePosition.X;
				var top = e.GetPosition(null).Y - mousePosition.Y;

				if (Math.Abs(left) > 3 || Math.Abs(top) > 3)
				{
					Mouse.Capture(Header);
					var pos = PointToScreen(Mouse.GetPosition(null));
					MainForm.Left = pos.X - mousePosition.X;
					MainForm.Top = pos.Y - mousePosition.Y;
				}
			}
		}

		private void btn_Close_MouseEnter(object sender, MouseEventArgs e)
		{
			btn_CloseShadow.Visibility = Visibility.Visible;
		}

		private void btn_Close_MouseLeave(object sender, MouseEventArgs e)
		{
			btn_CloseShadow.Visibility = Visibility.Hidden;
		}

		private void btn_Close_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			Environment.Exit(0);
		}

		private void btn_Add_Click(object sender, RoutedEventArgs e)
		{
			if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				AddFilesToList(ofd.FileNames);
			}
		}

		private void btn_Remove_Click(object sender, RoutedEventArgs e)
		{
			if (lv_List.SelectedItems.Count > 0)
			{
				List<object> selectedItems = new List<object>();
				foreach (var item in lv_List.SelectedItems)
				{
					selectedItems.Add(item);
				}

				foreach (var item in selectedItems)
				{
					lv_List.Items.Remove(item);
				}
			}
		}

		private void btn_Clear_Click(object sender, RoutedEventArgs e)
		{
			if (System.Windows.MessageBox.Show("Do you want to clear the list of images?", "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				images.Clear();
				UpdateList();
			}
		}

		private void lv_List_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (lv_List.SelectedItems.Count > 0)
			{
				btn_Remove.IsEnabled = true;
			}
			else
			{
				btn_Remove.IsEnabled = false;
			}
		}

		private void lv_List_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
			}
		}

		private void lv_List_DragOver(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Handled = true;
				e.Effects = DragDropEffects.Copy;
			}
		}

		private void lv_List_Drop(object sender, DragEventArgs e)
		{

			var files = (IEnumerable<string>)e.Data.GetData(DataFormats.FileDrop);
			AddFilesToList(files);
		}

		private void lv_List_DragLeave(object sender, DragEventArgs e)
		{
			e.Handled = true;
			e.Effects = DragDropEffects.None;
		}

		private void rb_OriginalSize_Checked(object sender, RoutedEventArgs e)
		{
			if (IsLoaded)
			{
				grid_TypicalScale.Visibility = Visibility.Hidden;
				grid_TypicalScale.IsEnabled = false;
			}
		}

		private void rb_Scale_Checked(object sender, RoutedEventArgs e)
		{
			if (IsLoaded)
			{
				grid_TypicalScale.Visibility = Visibility.Visible;
				grid_TypicalScale.IsEnabled = true;
			}
		}

		private void slider_ColorTolerance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (IsLoaded)
			{
				lbl_ColorTolerance.Text = slider_ColorTolerance.Value + "px";
			}
		}

		private void btn_SelectOutputFolder_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog()
			{
				ShowNewFolderButton = true,
				Description = "Select folder for output"
			};

			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				tb_OutputFolder.Text = fbd.SelectedPath;
			}
		}

		private void tb_OutputFolder_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (Directory.Exists(tb_OutputFolder.Text))
			{
				tb_OutputFolder.Foreground = Brushes.Black;
			}
			else
			{
				tb_OutputFolder.Foreground = Brushes.Red;
			}

			SetButton();
		}

		private void cb_ReplaceColor_Checked(object sender, RoutedEventArgs e)
		{
			if (IsLoaded)
			{
				grid_ReplaceColorTolerance.Visibility = Visibility.Visible;
				sp_ReplaceColorPanel.Visibility = Visibility.Visible;
			}
		}

		private void cb_ReplaceColor_Unchecked(object sender, RoutedEventArgs e)
		{
			if (IsLoaded)
			{
				grid_ReplaceColorTolerance.Visibility = Visibility.Hidden;
				sp_ReplaceColorPanel.Visibility = Visibility.Hidden;
			}
		}

		private void cb_UseOriginFolder_Checked(object sender, RoutedEventArgs e)
		{
			sp_Output.IsEnabled = false;

			SetButton();
		}

		private void cb_UseOriginFolder_Unchecked(object sender, RoutedEventArgs e)
		{
			sp_Output.IsEnabled = true;

			SetButton();
		}

		private void cb_ColorTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (IsLoaded)
			{
				Properties.Settings.Default.s_ColorTemplate = cb_ColorTemplate.SelectedIndex;
				Properties.Settings.Default.Save();
			}
			SetColorTemplate();
		}

		private void btn_Convert_Click(object sender, RoutedEventArgs e)
		{
			Initiate();
		}

		private void btn_Help_MouseUp(object sender, MouseButtonEventArgs e)
		{
			About ab = new About();
			ab.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			ab.Show();
		}

		private void Initiate()
		{
			FillItemList();
			GetSettings();

			images.Clear();

			if (isConverting)
			{
				btn_Convert.Content = "Convert";
				stopWork = true;
				isConverting = false;

				workerThread.Join();
			}
			else
			{
				btn_Convert.Content = "Stop";
				stopWork = false;
				isConverting = true;

				workerThread = new Thread(new ThreadStart(HandleConversion));
				workerThread.Start();
			}
		}

		private void FillItemList()
		{
			items.Clear();

			foreach (var item in lv_List.Items)
			{
				items.Add(item);
			}
		}

		private void HandleColorPicked()
		{
			cp_ColorPicker.Visibility = Visibility.Hidden;
			if (cp_ColorPicker.SelectedColor != null)
			{
				if (isColorOriginal)
				{
					canvas_Color_ReplaceThis.Background = new SolidColorBrush((Color)cp_ColorPicker.SelectedColor);
				}
				else
				{
					canvas_Color_ReplaceWithThis.Background = new SolidColorBrush((Color)cp_ColorPicker.SelectedColor);
				}
			}
		}

		private void AddFilesToList(IEnumerable<string> files)
		{
			if (files != null && files.Count() > 0)
			{
				foreach (string file in files)
				{
					if (!images.Contains(file.ToLower())) { images.Add(file.ToLower()); }
				}

				UpdateList();
			}
		}

		private void UpdateList()
		{
			lv_List.Items.Clear();

			foreach (string img in images)
			{
				var data = ImageParser.GetImageData(img);
				if (data != null)
					lv_List.Items.Add(data);
			}

			SetButton();

			GC.Collect();
		}

		private bool CheckAllReady()
		{
			if (lv_List.Items.Count > 0 &&
				(cb_UseOriginFolder.IsChecked == true || (!string.IsNullOrEmpty(tb_OutputFolder.Text) && Directory.Exists(tb_OutputFolder.Text))) &&
				(rb_OriginalSize.IsChecked == true || CountCheckedSizes() > 0))
			{
				return true;
			}

			return false;
		}

		private int CountCheckedSizes()
		{
			int ret = cb_Size_24.IsChecked == true ? 1 : 0;
			ret += cb_Size_32.IsChecked == true ? 1 : 0;
			ret += cb_Size_64.IsChecked == true ? 1 : 0;
			ret += cb_Size_128.IsChecked == true ? 1 : 0;
			ret += cb_Size_256.IsChecked == true ? 1 : 0;
			ret += cb_Size_512.IsChecked == true ? 1 : 0;
			ret += cb_Size_1024.IsChecked == true ? 1 : 0;
			ret += cb_Size_2048.IsChecked == true ? 1 : 0;

			return ret;
		}

		private void SetButton()
		{
			if (IsLoaded)
			{
				if (CheckAllReady())
				{
					btn_Convert.IsEnabled = true;
				}
				else
				{
					btn_Convert.IsEnabled = false;
				}
			}
		}

		private void GetSettings()
		{
			string sizes = "";
			settings = new Settings()
			{
				ColorDepth = Misc.GetColorDepth(cb_ColorDepth.SelectedIndex),
				Sizes = GetSizes(out sizes),
				OriginalColor = Misc.ColorFromBrush(canvas_Color_ReplaceThis.Background),
				ReplacementColor = Misc.ColorFromBrush(canvas_Color_ReplaceWithThis.Background),
				ReplaceColor = cb_ReplaceColor.IsChecked == true,
				OutputFolder = cb_UseOriginFolder.IsChecked == true ? "" : tb_OutputFolder.Text,
				UseOriginFolder = cb_UseOriginFolder.IsChecked == true,
				ReplacementTolerance = (int)slider_ColorTolerance.Value
			};

			def.s_ColorDepth = cb_ColorDepth.SelectedIndex;
			def.s_FolderPath = (cb_UseOriginFolder.IsChecked == true || !Directory.Exists(tb_OutputFolder.Text)) ? "" : tb_OutputFolder.Text;
			def.s_OriginalColor = Misc.ColorFromBrush(canvas_Color_ReplaceThis.Background);
			def.s_OriginalSize = rb_OriginalSize.IsChecked == true;
			def.s_ReplaceColor = cb_ReplaceColor.IsChecked == true;
			def.s_ReplacementColor = Misc.ColorFromBrush(canvas_Color_ReplaceWithThis.Background);
			def.s_ReplacementTolerance = (int)slider_ColorTolerance.Value;
			def.s_Sizes = sizes;
			def.s_UseOriginalFolder = cb_UseOriginFolder.IsChecked == true;

			def.Save();
		}

		private List<_Size> GetSizes(out string sizes)
		{
			sizes = "";
			List<_Size> sizeList = new List<_Size>();

			if (rb_OriginalSize.IsChecked == false)
			{
				_Size size;
				if (Misc.CheckBox(cb_Size_24, out size))	{ sizeList.Add(size); sizes += "1"; } else { sizes += "0"; }
				if (Misc.CheckBox(cb_Size_32, out size))	{ sizeList.Add(size); sizes += "1"; } else { sizes += "0"; }
				if (Misc.CheckBox(cb_Size_64, out size))	{ sizeList.Add(size); sizes += "1"; } else { sizes += "0"; }
				if (Misc.CheckBox(cb_Size_128, out size))	{ sizeList.Add(size); sizes += "1"; } else { sizes += "0"; }
				if (Misc.CheckBox(cb_Size_256, out size))	{ sizeList.Add(size); sizes += "1"; } else { sizes += "0"; }
				if (Misc.CheckBox(cb_Size_512, out size))	{ sizeList.Add(size); sizes += "1"; } else { sizes += "0"; }
				if (Misc.CheckBox(cb_Size_1024, out size))	{ sizeList.Add(size); sizes += "1"; } else { sizes += "0"; }
				if (Misc.CheckBox(cb_Size_2048, out size))	{ sizeList.Add(size); sizes += "1"; } else { sizes += "0"; }
			}

			return sizeList;
		}

		private void SetSizes(string sizes)
		{
			for (int i = 0; i < sizes.Length; i++)
			{
				CheckBox cb = null;

				switch (i)
				{
					case 0: cb = cb_Size_24;	break;
					case 1: cb = cb_Size_32;	break;
					case 2: cb = cb_Size_64;	break;
					case 3: cb = cb_Size_128;	break;
					case 4: cb = cb_Size_256;	break;
					case 5: cb = cb_Size_512;	break;
					case 6: cb = cb_Size_1024;	break;
					case 7: cb = cb_Size_2048;	break;
				}

				cb.IsChecked = sizes[i] == '1';
			}
		}

		private void HandleConversion()
		{
			while (items.Count > 0)
			{
				var item = items[0];

				if (stopWork) { workerThread.Abort(); break; }

				ImageConverter.ConvertImage(((ImageData)item).Path, settings);

				Dispatcher.BeginInvoke((Action)delegate () {
					removeItemFromList(item);
				});

				items.Remove(item);
			}

			Dispatcher.BeginInvoke((Action)delegate () { Initiate(); });
		}

		private void SetColorTemplate()
		{
			switch (cb_ColorTemplate.SelectedIndex)
			{
				//Purple
				case 0:
					Header.Background = new SolidColorBrush(Color.FromArgb(255, 148, 97, 101));

					lv_List.Background = new SolidColorBrush(Color.FromArgb(255, 229, 197, 238));

					MainForm.Background = new SolidColorBrush(Color.FromArgb(255, 195, 163, 204));
					LeftPanel.Background = new SolidColorBrush(Color.FromArgb(255, 195, 163, 204));
					RightPanel.Background = new SolidColorBrush(Color.FromArgb(255, 195, 163, 204));
					break;

				//Blue
				case 1:
					Header.Background = new SolidColorBrush(Color.FromArgb(255, 55, 120, 169));

					lv_List.Background = new SolidColorBrush(Color.FromArgb(255, 165, 254, 239));

					MainForm.Background = new SolidColorBrush(Color.FromArgb(255, 147, 220, 205));
					LeftPanel.Background = new SolidColorBrush(Color.FromArgb(255, 147, 220, 205));
					RightPanel.Background = new SolidColorBrush(Color.FromArgb(255, 147, 220, 205));
					break;

				//Orange
				case 2:
					Header.Background = new SolidColorBrush(Color.FromArgb(255, 149, 113, 75));

					lv_List.Background = new SolidColorBrush(Color.FromArgb(255, 255, 200, 124));

					MainForm.Background = new SolidColorBrush(Color.FromArgb(255, 237, 166, 90));
					LeftPanel.Background = new SolidColorBrush(Color.FromArgb(255, 237, 166, 90));
					RightPanel.Background = new SolidColorBrush(Color.FromArgb(255, 237, 166, 90));
					break;

				//Cyan
				case 3:
					Header.Background = new SolidColorBrush(Color.FromArgb(255, 109, 105, 106));

					lv_List.Background = new SolidColorBrush(Color.FromArgb(255, 191, 250, 255));

					MainForm.Background = new SolidColorBrush(Color.FromArgb(255, 157, 232, 238));
					LeftPanel.Background = new SolidColorBrush(Color.FromArgb(255, 157, 232, 238));
					RightPanel.Background = new SolidColorBrush(Color.FromArgb(255, 157, 232, 238));
					break;
			}
		}

		//Delegate
		private void removeItemFromList(object item)
		{
			lv_List.Items.Remove(item);
		}
	}
}
