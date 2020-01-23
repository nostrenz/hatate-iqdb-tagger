using System.Windows;
using System.Collections.Generic;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for EditTag.xaml
	/// </summary>
	public partial class EditTag : Window
	{
		public const string VARIOUS = "< various >";

		private List<Tag> tags = new List<Tag>();

		public EditTag(System.Collections.IList items)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			foreach (Tag tag in items) {
				this.tags.Add(tag);
			}

			this.UpdateTextBox();
			this.ShowDialog();
		}

		/*
		============================================
		Private
		============================================
		*/

		private void UpdateTextBox()
		{
			this.TextBox_Tag.Clear();

			if (this.tags.Count == 1) {
				this.TextBox_Tag.Text = this.tags[0].Namespaced;

				return;
			}

			string nameSpace = this.tags[0].Namespace;
			string value = this.tags[0].Value;

			foreach (Tag tag in this.tags) {
				if (nameSpace != VARIOUS && tag.Namespace != nameSpace) {
					nameSpace = VARIOUS;
				}

				if (value != VARIOUS && tag.Value != value) {
					value = VARIOUS;
				}
			}

			if (nameSpace != null) {
				this.TextBox_Tag.Text = nameSpace + ':';
			}

			this.TextBox_Tag.Text += value;
		}

		private void SetNamespace(string nameSpace)
		{
			foreach (Tag tag in this.tags) {
				tag.Namespace = nameSpace;
			}

			this.UpdateTextBox();
		}

		/*
		============================================
		Accessor
		============================================
		*/

		new public Tag Tag
		{
			get { return new Tag(this.TextBox_Tag.Text, true); }
		}

		/*
		============================================
		Event
		============================================
		*/

		private void Button_AddAsUnnamespaced_Click(object sender, RoutedEventArgs e)
		{
			this.SetNamespace(null);
		}

		private void Button_AddAsSeries_Click(object sender, RoutedEventArgs e)
		{
			this.SetNamespace("series");
		}

		private void Button_AddAsCharacter_Click(object sender, RoutedEventArgs e)
		{
			this.SetNamespace("character");
		}

		private void Button_AddAsCreator_Click(object sender, RoutedEventArgs e)
		{
			this.SetNamespace("creator");
		}

		private void Button_AddAsMeta_Click(object sender, RoutedEventArgs e)
		{
			this.SetNamespace("meta");
		}

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
