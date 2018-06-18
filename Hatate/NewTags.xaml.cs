using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for NewTags.xaml
	/// </summary>
	public partial class NewTags : Window
	{
		public NewTags(bool show=true)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item.Header = "Remove";
			item.Tag = "remove";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			this.ListBox_Tags.ContextMenu = context;
			this.TextBox_Value.Focus();

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
			this.ListBox_Tags.Items.Add(new Tag(value, nameSpace));
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

			if (nameSpace == "unnamespaced") {
				nameSpace = null;
			}

			Tag tag = new Tag(value, nameSpace);

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
				this.AddTag("unnamespaced");
			}
		}

		/// <summary>
		/// Called after clicking on an option from the Tags ListBox's context menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ContextMenu_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			if (mi == null) {
				return;
			}

			while (this.ListBox_Tags.SelectedItems.Count > 0) {
				this.ListBox_Tags.Items.Remove(this.ListBox_Tags.SelectedItems[0]);
			}
		}

		/// <summary>
		/// Called when clicking on the Ok button, close the window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Ok_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Add the entered text as an unnamespaced tag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_AddAsUnnamespaced(object sender, RoutedEventArgs e)
		{
			this.AddTag("unnamespaced");
		}

		/// <summary>
		/// Add the entered text as a series tag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_AddAsSeries(object sender, RoutedEventArgs e)
		{
			this.AddTag("series");
		}

		/// <summary>
		/// Add the entered text as a character tag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_AddAsCharacter(object sender, RoutedEventArgs e)
		{
			this.AddTag("character");
		}

		/// <summary>
		/// Add the entered text as a creator tag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_AddAsCreator(object sender, RoutedEventArgs e)
		{
			this.AddTag("creator");
		}

		#endregion Event
	}
}
