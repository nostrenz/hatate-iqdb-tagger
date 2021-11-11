using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
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

			this.TextBox_Limit.Text = Options.Default.HydrusQuery_Limit.ToString();
			this.CheckBox_InboxOnly.IsChecked = Options.Default.HydrusQuery_InboxOnly;
			this.CheckBox_ArchiveOnly.IsChecked = Options.Default.HydrusQuery_ArchiveOnly;
			this.CheckBox_WarnBeforeImport.IsChecked = Options.Default.HydrusQuery_WarnBeforeImport;

			this.TextBox_Tag.Focus();

			this.GetHydrusServices();
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
			this.EnableOrDisableExecuteQueryButton(true);
		}

		private async void GetHydrusServices()
		{
			bool doesSearchFilesSupportsServiceArguments = await App.hydrusApi.DoesSearchFilesSupportsServiceArguments();

			// Cannot contact the API
			if (App.hydrusApi.Unreachable) {
				this.Close();

				return;
			}

			// Hydrus API allows specifying the file and tag service in queries starting from API version 19
			if (!doesSearchFilesSupportsServiceArguments) {
				this.Button_ExecuteQuery.IsEnabled = true;

				return;
			}

			JObject services = await App.hydrusApi.GetServices();

			foreach (var item in services) {
				string key = (string)item.Key;
				JArray jArray = (JArray)item.Value;

				// File services
				if (key == "local_files" || key == "file_repositories" || key == "all_local_files" || key == "all_known_files") {
					foreach (JObject service in jArray) {
						ComboBoxItem comboBoxItem = new ComboBoxItem();
						comboBoxItem.Content = service.GetValue("name");
						comboBoxItem.Tag = service.GetValue("service_key");

						this.ComboBox_FileService.Items.Add(comboBoxItem);
					}
				} else if (key == "local_tags" || key == "tag_repositories" || key == "all_known_tags") { // Tag services
					foreach (JObject service in jArray) {
						ComboBoxItem comboBoxItem = new ComboBoxItem();
						comboBoxItem.Content = service.GetValue("name");
						comboBoxItem.Tag = service.GetValue("service_key");

						this.ComboBox_TagService.Items.Add(comboBoxItem);
					}
				}
			}

			this.ComboBox_FileService.SelectedIndex = 0;
			this.ComboBox_TagService.SelectedIndex = 0;

			this.ComboBox_FileService.IsEnabled = true;
			this.ComboBox_TagService.IsEnabled = true;

			this.ComboBox_FileService.ToolTip = null;
			this.ComboBox_TagService.ToolTip = null;

			this.Button_ExecuteQuery.IsEnabled = true;
		}

		private void EnableOrDisableExecuteQueryButton(bool enabled)
		{
			this.Button_ExecuteQuery.IsEnabled = enabled;
			this.Button_ExecuteQuery.Content = enabled ? "Execute query" : "Executing query...";
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

		private uint Limit
		{
			get
			{
				string text = this.TextBox_Limit.Text.Trim();

				if (String.IsNullOrEmpty(text)) {
					return 0;
				}

				uint limit = 0;
				uint.TryParse(text, out limit);

				return limit;
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

		private async void Button_ExecuteQuery_Click(object sender, RoutedEventArgs e)
		{
			this.EnableOrDisableExecuteQueryButton(false);

			uint limit = this.Limit;

			// Add limit tag
			if (limit > 0) {
				Tag tag = new Tag("limit = " + limit, "system");

				if (!this.ListBox_Tags.Items.Contains(tag)) {
					this.ListBox_Tags.Items.Add(tag);
				}
			}

			string tagServiceKey = null;
			string fileServiceKey = null;
			bool doesSearchFilesSupportsServiceArguments = await App.hydrusApi.DoesSearchFilesSupportsServiceArguments();

			if (App.hydrusApi.Unreachable) {
				this.CancelQuery();

				return;
			}

			if (doesSearchFilesSupportsServiceArguments) {
				ComboBoxItem selectedTagService = (ComboBoxItem)this.ComboBox_TagService.SelectedItem;
				ComboBoxItem selectedFileService = (ComboBoxItem)this.ComboBox_FileService.SelectedItem;

				if (selectedTagService != null) {
					tagServiceKey = selectedTagService.Tag.ToString();
				}

				if (selectedFileService != null) {
					fileServiceKey = selectedFileService.Tag.ToString();
				}
			}

			// Get files
			JArray fileIds = await App.hydrusApi.SearchFiles(this.Tags, (bool)this.CheckBox_InboxOnly.IsChecked, (bool)this.CheckBox_ArchiveOnly.IsChecked, tagServiceKey, fileServiceKey);

			if (App.hydrusApi.Unreachable) {
				this.CancelQuery();

				return;
			}

			if (fileIds == null || fileIds.Count < 1) {
				this.NoResult();

				return;
			}

			// Create a smaller array according to the limit
			// As of Hydrus version 448 this should not be needed anymore thanks to the "system:limit" tag
			if (limit > 0 && limit < fileIds.Count) {
				JArray limitedFileIds = new JArray();

				for (int i = 0; i < limit; i++) {
					limitedFileIds.Add(fileIds[i]);
				}

				fileIds = limitedFileIds;
				limitedFileIds = null;
			}

			// Warn the user about a huge number of files being imported
			if ((bool)this.CheckBox_WarnBeforeImport.IsChecked && !App.AskUser("You're about to import " + fileIds.Count + " files, are you sure about that?")) {
				this.CancelQuery();

				return;
			}

			// Get files' metadata
			JArray jTokens = await App.hydrusApi.GetFilesMetadata(fileIds);

			if (App.hydrusApi.Unreachable) {
				this.CancelQuery();

				return;
			}

			if (jTokens == null || jTokens.Count < 1) {
				this.NoResult();

				return;
			}

			// Fill the HydrusMetadata list from the JTokens
			this.hydrusMetadataList = new List<HydrusMetadata>();
			List<string> unsupportedMimetypes = new List<string>();

			foreach (JToken jToken in jTokens) {
				HydrusMetadata hydrusMetadata = new HydrusMetadata(jToken);

				if (hydrusMetadata.IsImage) {
					this.hydrusMetadataList.Add(hydrusMetadata);

					continue;
				}

				if (!unsupportedMimetypes.Contains(hydrusMetadata.Mime)) {
					unsupportedMimetypes.Add(hydrusMetadata.Mime);
				}
			}

			// Warn about files not imported due to unsupported type
			if (unsupportedMimetypes.Count > 0) {
				MessageBox.Show(
					jTokens.Count + " files were found from the query but only " + this.hydrusMetadataList.Count + " will be imported due to some file types not being supported:\n\n"
					+ String.Join(", ", unsupportedMimetypes.ToArray())
				);
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
			App.PasteTags(this.TextBox_Tag, this.ListBox_Tags, e);
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

		private void Window_Closed(object sender, EventArgs e)
		{
			Options.Default.HydrusQuery_Limit = this.Limit;
			Options.Default.HydrusQuery_InboxOnly = (bool)this.CheckBox_InboxOnly.IsChecked;
			Options.Default.HydrusQuery_ArchiveOnly = (bool)this.CheckBox_ArchiveOnly.IsChecked;
			Options.Default.HydrusQuery_WarnBeforeImport = (bool)this.CheckBox_WarnBeforeImport.IsChecked;

			Options.Default.Save();
		}

		#endregion Event
	}
}
