using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for Capture.xaml
	/// </summary>
	public partial class Capture : Window
	{
		private double xPos = 0;
		private double yPos = 0;

		public Capture()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			// Make the window fill the whole screen, including all connected displays
			this.Left = 0;
			this.Top = 0;
			this.Width = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
			this.Height = System.Windows.Forms.SystemInformation.VirtualScreen.Height;

			this.ShowDialog();
		}

		/*
		============================================
		Private
		============================================
		*/

		private void CaptureArea()
		{
			// Same thickness on all sides
			int borderThickness = (int)this.Border_Area.BorderThickness.Left;

			int left = (int)this.Border_Area.Margin.Left + borderThickness;
			int top = (int)this.Border_Area.Margin.Top + borderThickness;
			int width = (int)this.Border_Area.Width - borderThickness*2;
			int height = (int)this.Border_Area.Height - borderThickness*2;
			this.FilePath = App.TempPngFilePath;

			try {
				Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
				Graphics graphics = Graphics.FromImage(bitmap);

				graphics.CopyFromScreen(left, top, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
				bitmap.Save(this.FilePath, ImageFormat.Png);
			} catch (System.Exception) {
				this.FilePath = null;
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public string FilePath
		{
			get; internal set;
		}

		/*
		============================================
		Event
		============================================
		*/

		private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			// Close window
			if (e.Key == System.Windows.Input.Key.Escape) {
				this.Close();
			}
		}

		private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			// Close window
			if (e.ChangedButton != System.Windows.Input.MouseButton.Left) {
				this.Close();

				return;
			}

			System.Windows.Point point = e.GetPosition(this);

			this.xPos = point.X;
			this.yPos = point.Y;

			this.Border_Area.Margin = new Thickness(point.X, point.Y, 0, 0);
			this.Border_Area.Width = 0;
			this.Border_Area.Height = 0;
			this.Border_Area.Visibility = Visibility.Visible;
		}

		private void Window_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (this.xPos == 0 || this.yPos == 0) {
				return;
			}

			// We'll capture the area right after closing the window so it won't be in the way
			this.Close();
			this.CaptureArea();
		}

		private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (this.xPos == 0 || this.yPos == 0) {
				return;
			}

			System.Windows.Point point = e.GetPosition(this);
			double width = 0;
			double height = 0;
			double x = this.xPos;
			double y = this.yPos;

			if (point.X >= this.xPos) {
				width = point.X - this.xPos;
			} else {
				width = this.xPos - point.X;
				x = point.X;
			}

			if (point.Y >= this.yPos) {
				height = point.Y - this.yPos;
			} else {
				height = this.yPos - point.Y;
				y = point.Y;
			}

			if (width < 0) width = 0;
			if (height < 0) height = 0;

			this.Border_Area.Width = width;
			this.Border_Area.Height = height;
			this.Border_Area.Margin = new Thickness(x, y, 0, 0);
		}
	}
}
