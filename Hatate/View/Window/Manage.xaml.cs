using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for Tags.xaml
	/// </summary>
	public partial class Manage : Window
	{
		private bool okClicked = false;

		public Manage(bool show=true)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item.Header = "Copy";
			item.Tag = "copy";
			item.Click += this.ContextMenu_MenuItem_Copy;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Remove";
			item.Tag = "remove";
			item.Click += this.ContextMenu_MenuItem_Remove;
			context.Items.Add(item);

			this.ListBox_Tags.ContextMenu = context;
			this.TextBox_Value.Focus();

			DataObject.AddPastingHandler(this.TextBox_Value, this.TextBox_Tag_Paste);

			if (show) {
				this.ShowDialog();
			}
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Add a new tag to the list from string values.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="nameSpace"></param>
		public void AddTag(string value, string nameSpace=null)
		{
			this.ListBox_Tags.Items.Add(new Tag(value, nameSpace) { Source = Hatate.Tag.SOURCE_USER });
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Add a tag to the list.
		/// </summary>
		private void AddTag(string nameSpace)
		{
			this.TextBox_Value.Focus();

			string value = this.TextBox_Value.Text.Trim();

			if (String.IsNullOrEmpty(value)) {
				return;
			}

			Tag tag = null;

			if (nameSpace != null) {
				tag = new Tag(value, nameSpace) { Source = Hatate.Tag.SOURCE_USER };
			} else {
				tag = new Tag(value, true) { Source = Hatate.Tag.SOURCE_USER };
			}

			if (!this.ListBox_Tags.Items.Contains(tag)) {
				this.ListBox_Tags.Items.Add(tag);
			}

			this.TextBox_Value.Clear();
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public List<Tag> Tags
		{
			get {
				List<Tag> tags = new List<Tag>();

				foreach (Tag item in this.ListBox_Tags.Items) {
					tags.Add(item);
				}

				return tags;
			}
		}

		public bool OkClicked
		{
			get { return this.okClicked; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		///  Add unnamespaced tag when pressing Enter (same as clicking on the Add button).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Value_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) {
				this.AddTag(null);
			}
		}

		/// <summary>
		/// Called after clicking on an option from the Tags ListBox's context menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ContextMenu_MenuItem_Copy(object sender, RoutedEventArgs e)
		{
			App.CopySelectedTagsToClipboard(this.ListBox_Tags);
		}

		/// <summary>
		/// Called after clicking on an option from the Tags ListBox's context menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ContextMenu_MenuItem_Remove(object sender, RoutedEventArgs e)
		{
			while (this.ListBox_Tags.SelectedItems.Count > 0) {
				this.ListBox_Tags.Items.Remove(this.ListBox_Tags.SelectedItems[0]);
			}
		}

		/// <summary>
		/// Called when clicking on the Ok button, close the window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			this.okClicked = true;

			this.Close();
		}

		/// <summary>
		/// Add the entered text as a series tag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_AddAsSeries_Click(object sender, RoutedEventArgs e)
		{
			this.AddTag("series");
		}

		/// <summary>
		/// Add the entered text as a character tag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_AddAsCharacter_Click(object sender, RoutedEventArgs e)
		{
			this.AddTag("character");
		}

		/// <summary>
		/// Add the entered text as a creator tag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_AddAsCreator_Click(object sender, RoutedEventArgs e)
		{
			this.AddTag("creator");
		}

		/// <summary>
		/// Add the entered text as an unnamespaced tag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_AddAsMeta_Click(object sender, RoutedEventArgs e)
		{
			this.AddTag("meta");
		}

		/// <summary>
		/// Sort the tags by namespace and value.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Sort_Click(object sender, RoutedEventArgs e)
		{
			this.ListBox_Tags.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Namespace", System.ComponentModel.ListSortDirection.Descending));
			this.ListBox_Tags.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Value", System.ComponentModel.ListSortDirection.Ascending));
		}

		/// <summary>
		/// Remove a tag in the list by double clicking on it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Tags_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			this.ListBox_Tags.Items.Remove(this.ListBox_Tags.SelectedItem);
		}

		private void TextBox_Tag_Paste(object sender, DataObjectPastingEventArgs e)
		{
			App.PasteTags(this.TextBox_Value, this.ListBox_Tags, e);
		}

		#endregion Event
	}
}
