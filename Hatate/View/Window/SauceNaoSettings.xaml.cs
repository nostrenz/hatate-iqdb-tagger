using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;
using Hatate.Properties;
using Hatate.View.Control;

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
			this.LoadTagNamespaces();
		}

		/*
		Private
		*/

		private void LoadTagNamespaces()
		{
			List<TagNamespaceItem> tagNamespaceItems = new List<TagNamespaceItem>();

			foreach (KeyValuePair<string, TagNamespace> keyValuePair in App.tagNamespaces.TagNamespacesList) {
				TagNamespaceItem tagNamespaceItem = new TagNamespaceItem(keyValuePair.Key, keyValuePair.Value);
				tagNamespaceItems.Add(tagNamespaceItem);
			}

			// Sort by name
			tagNamespaceItems = tagNamespaceItems.OrderBy(tagNamespaceItem => tagNamespaceItem.KeyName).ToList();

			foreach (TagNamespaceItem tagNamespaceItem in tagNamespaceItems) {
				this.ListView_TagNamespaces.Items.Add(tagNamespaceItem);
			}
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
			// Tag namespaces
			App.tagNamespaces.Clear();

			foreach (TagNamespaceItem tagNamespaceItem in this.ListView_TagNamespaces.Items) {
				App.tagNamespaces.Add(tagNamespaceItem.KeyName, new TagNamespace(tagNamespaceItem));
			}

			Settings.Default.SauceNaoApiKey = this.TextBox_ApiKey.Text;
			Settings.Default.TagNamespaces = App.tagNamespaces.Serialize();

			Settings.Default.Save();

			this.Close();
		}
	}
}
