using System.Windows;
using System.Diagnostics;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for Release.xaml
	/// </summary>
	public partial class Release : Window
	{
		ushort releaseNumber = 0;

		public Release(ushort releaseNumber)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.releaseNumber = releaseNumber;

			this.Label_Release.Content += " " + releaseNumber;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public string Changelog
		{
			set { this.WebBrowser_Changelog.NavigateToString("<html><body style=\"background: #333333; color: #C8C8C8\">" + value + "</body></html>"); }
		}

		/*
		============================================
		Event
		============================================
		*/

		private void Button_Open_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(App.GITHUB_REPOSITORY_URL + App.GITHUB_LATEST_RELEASE);

			this.Close();
		}

		private void Button_Close_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
