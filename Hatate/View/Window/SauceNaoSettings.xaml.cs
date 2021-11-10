using System.Windows;
using System.Diagnostics;
using Hatate.Properties;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for SauceNaoSettings.xaml
	/// </summary>
	public partial class SauceNaoSettings : Window
	{
		public SauceNaoSettings()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.TextBox_ApiKey.Text = Settings.Default.SauceNaoApiKey;
		}

		private void TextBlock_Register_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Process.Start(new ProcessStartInfo("https://saucenao.com/user.php"));
		}

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.SauceNaoApiKey = this.TextBox_ApiKey.Text;

			Settings.Default.Save();

			this.Close();
		}
	}
}
