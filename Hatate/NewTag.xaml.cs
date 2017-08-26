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
	/// Interaction logic for NewTag.xaml
	/// </summary>
	public partial class NewTag : Window
	{
		public NewTag()
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

		public Tag GetTag
		{
			get
			{
				string value = this.TextBox_Value.Text.Trim();

				if (String.IsNullOrEmpty(value)) {
					return null;
				}

				string nameSpace = this.ComboBox_Namespace.SelectedValue.ToString();

				if (nameSpace == "unnamespaced") {
					nameSpace = null;
				}

				return new Tag(value, nameSpace);
			}
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private void Button_Add_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		#endregion Event
	}
}
