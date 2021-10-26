using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for About.xaml
	/// </summary>
	public partial class About : Window
	{
		public About()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.Label_Release.Content += " " + App.RELEASE_NUMBER;
			this.TextBlock_GithubUrl.Text = App.RepositoryUrl;
		}

		private void TextBlock_GithubUrl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Process.Start(App.RepositoryUrl);
		}
	}
}
