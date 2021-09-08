using System.Windows;

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

			this.ShowDialog();
		}

		/*
		============================================
		Private
		============================================
		*/

		private void CaptureArea()
		{
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

			Point point = e.GetPosition(this);

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

			this.CaptureArea();
			this.Close();
		}

		private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (this.xPos == 0 || this.yPos == 0) {
				return;
			}

			Point point = e.GetPosition(this);

			this.Border_Area.Width = point.X - this.xPos;
			this.Border_Area.Height = point.Y - this.yPos;
		}
	}
}
