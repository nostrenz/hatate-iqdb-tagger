using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for UnknownTags.xaml
	/// </summary>
	public partial class UnknownTags : Window
	{
		public UnknownTags(List<string> tags, IqdbApi.Enums.Source source)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.Label_UnknownTags.Content += " from " + source.ToString();
			this.ListBox_UnknownTags.SelectionMode = SelectionMode.Extended;

			this.CreateUnknownTagsContextMenu();
			
			this.ListBox_Unnamespaceds.ContextMenu = this.CreateKnownTagsContextMenu("Unnamespaced");
			this.ListBox_Series.ContextMenu = this.CreateKnownTagsContextMenu("Series");
			this.ListBox_Characters.ContextMenu = this.CreateKnownTagsContextMenu("Character");
			this.ListBox_Creators.ContextMenu = this.CreateKnownTagsContextMenu("Creator");

			foreach (string tag in tags) {
				this.ListBox_UnknownTags.Items.Add(tag);
			}
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private void CreateUnknownTagsContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item = new MenuItem();
			item.Header = "Add as unnamespaced";
			item.Tag = "addUnnamespaced";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Add as series";
			item.Tag = "addSeries";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Add as character";
			item.Tag = "addCharacter";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Add as creator";
			item.Tag = "addCreator";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			this.ListBox_UnknownTags.ContextMenu = context;
		}

		private ContextMenu CreateKnownTagsContextMenu(string name)
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item = new MenuItem();
			item.Header = "Remove";
			item.Tag = "remove" + name;
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			return context;
		}

		private void MoveToList(ListBox from, ListBox to)
		{
			foreach (var item in from.SelectedItems) {
				to.Items.Add(item);
			}

			while (from.SelectedItems.Count > 0) {
				from.Items.Remove(from.SelectedItems[0]);
			}
		}

		private List<string> ItemListToStringList(ListBox listBox)
		{
			List<string> strings = new List<string>();

			foreach (var item in listBox.Items) {
				strings.Add(item.ToString());
			}

			return strings;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public List<string> Unnamespaceds
		{
			get { return this.ItemListToStringList(this.ListBox_Unnamespaceds); }
		}

		public List<string> Series
		{
			get { return this.ItemListToStringList(this.ListBox_Series); }
		}

		public List<string> Characters
		{
			get { return this.ItemListToStringList(this.ListBox_Characters); }
		}

		public List<string> Creators
		{
			get { return this.ItemListToStringList(this.ListBox_Creators); }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private void ContextMenu_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			if (mi == null) {
				return;
			}

			switch (mi.Tag) {
				case "addUnnamespaced":
					this.MoveToList(this.ListBox_UnknownTags, this.ListBox_Unnamespaceds);
				break;
				case "addSeries":
					this.MoveToList(this.ListBox_UnknownTags, this.ListBox_Series);
				break;
				case "addCharacter":
					this.MoveToList(this.ListBox_UnknownTags, this.ListBox_Characters);
				break;
				case "addCreator":
					this.MoveToList(this.ListBox_UnknownTags, this.ListBox_Creators);
				break;
				case "removeUnnamespaced":
					this.MoveToList(this.ListBox_Unnamespaceds, this.ListBox_UnknownTags);
				break;
				case "removeSeries":
					this.MoveToList(this.ListBox_Series, this.ListBox_UnknownTags);
				break;
				case "removeCharacter":
					this.MoveToList(this.ListBox_Characters, this.ListBox_UnknownTags);
				break;
				case "removeCreator":
					this.MoveToList(this.ListBox_Creators, this.ListBox_UnknownTags);
				break;
			}
		}

		private void Button_Ok_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		#endregion Event
	}
}
