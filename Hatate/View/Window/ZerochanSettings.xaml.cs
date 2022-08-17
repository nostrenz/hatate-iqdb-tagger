using System.Linq;
using System.Windows;
using System.Collections.Generic;
using Hatate.Properties;
using Hatate.View.Control;

namespace Hatate.View.Window
{
	/// <summary>
	/// Interaction logic for ZerochanSettings.xaml
	/// </summary>
	public partial class ZerochanSettings : System.Windows.Window
	{
		private TagNamespaces.Zerochan tagNamespaces = new TagNamespaces.Zerochan();

		public ZerochanSettings()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.LoadTagNamespaces();
		}

		/*
		Private
		*/

		private void LoadTagNamespaces()
		{
			List<TagNamespaceItem> tagNamespaceItems = new List<TagNamespaceItem>();

			foreach (KeyValuePair<string, TagNamespace> keyValuePair in this.tagNamespaces.TagNamespacesList) {
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

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			this.tagNamespaces.Clear();

			foreach (TagNamespaceItem tagNamespaceItem in this.ListView_TagNamespaces.Items) {
				this.tagNamespaces.Add(tagNamespaceItem.KeyName, new TagNamespace(tagNamespaceItem));
			}
			
			Settings.Default.ZerochanTagNamespaces = this.tagNamespaces.Serialize();
			Settings.Default.Save();

			this.Close();
		}
	}
}
