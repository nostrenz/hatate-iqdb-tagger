using System.Windows;
using System.Windows.Controls;
using Hatate.Properties;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for IqdbSettings.xaml
	/// </summary>
	public partial class IqdbSettings : Window
	{
		public IqdbSettings()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			// Tag namespace
			this.SetNamespace(this.CheckBox_GetTags, this.TextBox_TagNamespace, Settings.Default.Iqdb_TagNamespace);
		}

		/*
		Private
		*/

		private void SetNamespace(CheckBox checkBox, TextBox textBox, string text)
		{
			checkBox.IsChecked = text != null && !text.StartsWith("-");
			textBox.IsEnabled = (bool)checkBox.IsChecked;

			if (text == null) {
				textBox.Text = "";
			} else {
				textBox.Text = text.StartsWith("-") ? text.Substring(1) : text;
			}
		}

		private string GetNamespace(CheckBox checkbox, TextBox textbox)
		{
			if (textbox.Text == null) {
				return null;
			}

			return ((bool)checkbox.IsChecked ? "" : "-") + textbox.Text.Trim();
		}

		/*
		Event
		*/

		private void CheckBox_GetTags_Click(object sender, RoutedEventArgs e)
		{
			this.TextBox_TagNamespace.IsEnabled = (bool)this.CheckBox_GetTags.IsChecked;
		}

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.Iqdb_TagNamespace = this.GetNamespace(this.CheckBox_GetTags, this.TextBox_TagNamespace);

			Settings.Default.Save();

			this.Close();
		}
	}
}
