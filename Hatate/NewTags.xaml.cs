using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for NewTags.xaml
	/// </summary>
	public partial class NewTags : Window
	{
		public NewTags()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.ShowDialog();
		}

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
		/// Called when clicking on the Add button, add a tag to the list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Add_Click(object sender, RoutedEventArgs e)
		{
			string value = this.TextBox_Value.Text.Trim();

			if (String.IsNullOrEmpty(value)) {
				return;
			}

			string nameSpace = this.ComboBox_Namespace.SelectedValue.ToString();

			if (nameSpace == "unnamespaced") {
				nameSpace = null;
			}

			Tag tag = new Tag(value, nameSpace);

			if (!this.ListBox_Tags.Items.Contains(tag)) {
				this.ListBox_Tags.Items.Add(tag);
			}

			this.TextBox_Value.Clear();
			this.TextBox_Value.Focus();
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

		#endregion Event
	}
}
