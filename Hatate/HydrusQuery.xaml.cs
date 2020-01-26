using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Options = Hatate.Properties.Settings;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for HydrusQuery.xaml
	/// </summary>
	public partial class HydrusQuery : Window
	{
		const int WARN_LIMIT = 1000;
		private List<HydrusMetadata> hydrusMetadataList = null;

		public HydrusQuery()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			if (Options.Default.AddFoundTag) {
				this.ListBox_Tags.Items.Add(new Tag('-' + Options.Default.FoundTag, true));
			}
			if (Options.Default.AddNotfoundTag) {
				this.ListBox_Tags.Items.Add(new Tag('-' + Options.Default.NotfoundTag, true));
			}

			this.CreateTagsListContextMenu();

			// Register tag paste event
			DataObject.AddPastingHandler(this.TextBox_Tag, this.TextBox_Tag_Paste);

			this.ShowDialog();
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private void AddTagToList()
		{
			this.TextBox_Tag.Focus();

			string value = this.TextBox_Tag.Text.Trim();

			if (String.IsNullOrEmpty(value)) {
				return;
			}

			Tag tag = new Tag(value, true);

			if (!this.ListBox_Tags.Items.Contains(tag)) {
				this.ListBox_Tags.Items.Add(tag);
			}

			this.TextBox_Tag.Clear();
		}

		private void CreateTagsListContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item.Header = "Copy";
			item.Tag = "copy";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Remove";
			item.Tag = "remove";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Exclude";
			item.Tag = "exclude";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Permit";
			item.Tag = "permit";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			this.ListBox_Tags.ContextMenu = context;
		}

		private Tag GetSelectedItemAt(int index)
		{
			return (Tag)this.ListBox_Tags.SelectedItems[index];
		}

		private void NoResult()
		{
			MessageBox.Show("There's no result for this query.");

			this.CancelQuery();
		}

		private void CancelQuery()
		{
			this.hydrusMetadataList = null;
			this.Button_ExecuteQuery.Content = "Execute query";
			this.Button_ExecuteQuery.IsEnabled = true;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		private string[] Tags
		{
			get
			{
				string[] tags = new string[this.ListBox_Tags.Items.Count];

				for (int i=0; i< this.ListBox_Tags.Items.Count; i++) {
					tags[i] = ((Tag)this.ListBox_Tags.Items[i]).Namespaced;
				}

				return tags;
			}
		}

		private int Limit
		{
			get
			{
				string text = this.TextBox_Limit.Text.Trim();

				if (String.IsNullOrEmpty(text)) {
					return 0;
				}

				int limit = 0;
				int.TryParse(text, out limit);

				return Math.Abs(limit);
			}
		}

		public List<HydrusMetadata> HydrusMetadataList
		{
			get { return this.hydrusMetadataList; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return) {
				this.AddTagToList();
			}
		}

		private void Button_AddTag_Click(object sender, RoutedEventArgs e)
		{
			this.AddTagToList();
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			this.Button_ExecuteQuery.IsEnabled = false;
			this.Button_ExecuteQuery.Content = "Executing query...";

			// Get files
			JArray fileIds = await App.hydrusApi.SearchFiles(this.Tags);

			if (fileIds == null || fileIds.Count < 1) {
				this.NoResult();

				return;
			}

			int limit = this.Limit;

			// Create a smaller array according to the limit
			if (Limit > 0 && limit < fileIds.Count) {
				JArray limitedFileIds = new JArray();

				for (int i = 0; i < limit; i++) {
					limitedFileIds.Add(fileIds[i]);
				}

				fileIds = limitedFileIds;
				limitedFileIds = null;
			}

			// Warn the user about a huge number of files being imported
			if (fileIds.Count >= WARN_LIMIT && !App.AskUser("You're about to import " + fileIds.Count + " files, are you sure about that?")) {
				this.TextBox_Limit.Text = (WARN_LIMIT - 1).ToString();
				this.CancelQuery();

				return;
			}

			// Get files' metadata
			JArray jTokens = await App.hydrusApi.GetFilesMetadata(fileIds);

			if (jTokens == null || jTokens.Count < 1) {
				this.NoResult();

				return;
			}

			// Fill the HydrusMetadata list from the JTokens
			this.hydrusMetadataList = new List<HydrusMetadata>();

			foreach (JToken jToken in jTokens) {
				HydrusMetadata hydrusMetadata = new HydrusMetadata(jToken);

				if (hydrusMetadata.IsImage) {
					this.hydrusMetadataList.Add(hydrusMetadata);
				}
			}

			this.Close();
		}

		/// <summary>
		///  Remove a tag by double clicking on it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Tags_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			this.ListBox_Tags.Items.Remove(this.ListBox_Tags.SelectedItem);
		}

		private void TextBox_Tag_Paste(object sender, DataObjectPastingEventArgs e)
		{
			if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true)) {
				return;
			}

			string text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
			string[] lines = text.Split('\n');

			if (lines.Length <= 1) {
				return;
			}

			// Add each line as a tag
			foreach (string line in lines) {
				Tag tag = new Tag(line.Trim(), true);

				if (!this.ListBox_Tags.Items.Contains(tag)) {
					this.ListBox_Tags.Items.Add(tag);
				}
			}

			this.TextBox_Tag.Clear();
		}

		private void ContextMenu_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			if (mi == null) {
				return;
			}

			switch (mi.Tag) {
				case "copy":
					App.CopySelectedTagsToClipboard(this.ListBox_Tags);
				break;
				case "remove":
					while (this.ListBox_Tags.SelectedItems.Count > 0) {
						this.ListBox_Tags.Items.Remove(this.GetSelectedItemAt(0));
					}
				break;
				case "exclude":
					foreach (Tag tag in this.ListBox_Tags.SelectedItems) {
						tag.Exclude = true;
					}

					this.ListBox_Tags.Items.Refresh();
				break;
				case "permit":
					foreach (Tag tag in this.ListBox_Tags.SelectedItems) {
						tag.Exclude = false;
					}

					this.ListBox_Tags.Items.Refresh();
				break;
			}
		}

		#endregion Event
	}
}
