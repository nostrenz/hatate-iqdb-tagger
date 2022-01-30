using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
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

			// Tag namespaces
			this.SetNamespace(this.Checkbox_Tag_Title, this.TextBox_TagNamespace_Title, Settings.Default.SauceNao_TagNamespace_Title);
			this.SetNamespace(this.Checkbox_Tag_Creator, this.TextBox_TagNamespace_Creator, Settings.Default.SauceNao_TagNamespace_Creator);
			this.SetNamespace(this.Checkbox_Tag_Material, this.TextBox_TagNamespace_Material, Settings.Default.SauceNao_TagNamespace_Material);
			this.SetNamespace(this.Checkbox_Tag_Character, this.TextBox_TagNamespace_Character, Settings.Default.SauceNao_TagNamespace_Character);
			this.SetNamespace(this.Checkbox_Tag_PixivIllustId, this.TextBox_TagNamespace_PixivIllustId, Settings.Default.SauceNao_TagNamespace_PixivIllustId);
			this.SetNamespace(this.Checkbox_Tag_PixivMemberId, this.TextBox_TagNamespace_PixivMemberId, Settings.Default.SauceNao_TagNamespace_PixivMemberId);
			this.SetNamespace(this.Checkbox_Tag_PixivMemberName, this.TextBox_TagNamespace_PixivMemberName, Settings.Default.SauceNao_TagNamespace_PixivMemberName);
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

		private void TextBlock_Register_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Process.Start(new ProcessStartInfo("https://saucenao.com/user.php"));
		}

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.SauceNaoApiKey = this.TextBox_ApiKey.Text;

			Settings.Default.SauceNao_TagNamespace_Title = this.GetNamespace(this.Checkbox_Tag_Title, this.TextBox_TagNamespace_Title);
			Settings.Default.SauceNao_TagNamespace_Creator = this.GetNamespace(this.Checkbox_Tag_Creator, this.TextBox_TagNamespace_Creator);
			Settings.Default.SauceNao_TagNamespace_Material = this.GetNamespace(this.Checkbox_Tag_Material, this.TextBox_TagNamespace_Material);
			Settings.Default.SauceNao_TagNamespace_Character = this.GetNamespace(this.Checkbox_Tag_Character, this.TextBox_TagNamespace_Character);
			Settings.Default.SauceNao_TagNamespace_PixivIllustId = this.GetNamespace(this.Checkbox_Tag_PixivIllustId, this.TextBox_TagNamespace_PixivIllustId);
			Settings.Default.SauceNao_TagNamespace_PixivMemberId = this.GetNamespace(this.Checkbox_Tag_PixivMemberId, this.TextBox_TagNamespace_PixivMemberId);
			Settings.Default.SauceNao_TagNamespace_PixivMemberName = this.GetNamespace(this.Checkbox_Tag_PixivMemberName, this.TextBox_TagNamespace_PixivMemberName);

			Settings.Default.Save();

			this.Close();
		}

		private void CheckBox_TagNamepace_Click(object sender, RoutedEventArgs e)
		{
			CheckBox checkBox = (CheckBox)sender;

			if (checkBox == null) {
				return;
			}

			string name = checkBox.Name;
			name = name.Replace("Checkbox_Tag_", "TextBox_TagNamespace_");

			Panel panel = (Panel)this.GroupBox_TagNamepaces.Content;
			UIElementCollection elementCollection = panel.Children;

			// Find the corresponding textbox
			foreach (UIElement element in elementCollection) {
				if (element.GetType() != typeof(TextBox)) {
					continue;
				}

				TextBox textBox = (TextBox)element;

				if (name == textBox.Name) {
					textBox.IsEnabled = (bool)checkBox.IsChecked;
				}
			}
		}
	}
}
