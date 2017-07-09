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
	/// Interaction logic for Option.xaml
	/// </summary>
	public partial class Option : Window
	{
		public Option()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.CheckBox_Compare.IsChecked = Properties.Settings.Default.Compare;

			this.ShowDialog();
		}

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.Compare = (bool)this.CheckBox_Compare.IsChecked;

			this.Close();
		}
	}
}
