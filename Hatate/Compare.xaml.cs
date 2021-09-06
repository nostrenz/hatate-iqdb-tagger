using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Media;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for Compare.xaml
	/// </summary>
	public partial class Compare : Window
	{
		private Result result = null;

		public Compare()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			if (App.Current.MainWindow.Left + App.Current.MainWindow.Width + this.Width < SystemParameters.WorkArea.Width) { // Right
				this.Left = App.Current.MainWindow.Left + App.Current.MainWindow.Width;
				this.Top = App.Current.MainWindow.Top;
			} else if (App.Current.MainWindow.Left - this.Width > 0) { // Left
				this.Left = App.Current.MainWindow.Left - this.Width;
				this.Top = App.Current.MainWindow.Top;
			} else if (App.Current.MainWindow.Top + App.Current.MainWindow.Height + this.Height < SystemParameters.WorkArea.Height) { // Bottom
				this.Left = App.Current.MainWindow.Left;
				this.Top = App.Current.MainWindow.Top + App.Current.MainWindow.Height;
			} else if (App.Current.MainWindow.Top - this.Height > 0) { // Top
				this.Left = App.Current.MainWindow.Left;
				this.Top = App.Current.MainWindow.Top - this.Height;
			} else { // Center
				this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			}
		}

		/*
		============================================
		Public
		============================================
		*/

		public void LoadResultImages(Result result)
		{
			// Already loaded
			if (this.result != null && result.ImagePath == this.result.ImagePath && result.Full == this.result.Full) {
				return;
			}

			this.result = result;
			this.Image_Local.Source = null;
			this.Image_Remote.Source = null;

			this.Status("Loading 0% " + this.result.Full);

			try {
				this.Image_Local.Source = App.CreateBitmapImage(result.ImagePath);
				this.Image_Remote.Source = new BitmapImage(new Uri(result.PreviewUrl));
			} catch (Exception) {
				this.Status("Image loading failed.");

				return;
			}

			BitmapImage bitmapImage = new BitmapImage();

			bitmapImage.BeginInit();
			bitmapImage.UriSource = new Uri(result.Full);
			bitmapImage.CacheOption = BitmapCacheOption.OnDemand;
			bitmapImage.DownloadProgress += this.FullImageLoadingProgress;
			bitmapImage.DownloadCompleted += this.FullImageLoadingCompleted;
			bitmapImage.DownloadFailed += this.FullImageLoadingCompleted;
			bitmapImage.EndInit();

			this.Show();
		}

		/*
		============================================
		Private
		============================================
		*/

		private void Status(string message)
		{
			this.Label_Status.Content = message;
		}

		/*
		============================================
		Event
		============================================
		*/

		private void FullImageLoadingProgress(object sender, DownloadProgressEventArgs e)
		{
			this.Status("Loading " + e.Progress + "% " + this.result.Full);
		}

		private void FullImageLoadingCompleted(object sender, EventArgs args)
		{
			BitmapImage bitmapImage = (BitmapImage)sender;
			bitmapImage.Freeze();

			this.Image_Remote.Source = bitmapImage;
			this.Status("Loaded.");
		}

		private void FullImageLoadingFailed(object sender, ExceptionEventArgs args)
		{
			this.Status("Higher quality image loading failed.");
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape) {
				this.Close();
			}
		}
	}
}
