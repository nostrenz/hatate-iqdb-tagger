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
	/// Interaction logic for Compare.xaml
	/// </summary>
	public partial class Compare : Window
	{
		private bool isGood = false;

		public Compare(string thumb, string match)
		{
			InitializeComponent();

			this.Image_Original.Source = new BitmapImage(new Uri(thumb));
			this.Image_Match.Source = new BitmapImage(new Uri(match));

			this.ShowDialog();
		}

		public bool IsGood()
		{
			return this.isGood;
		}

		private void Button_Good_Click(object sender, RoutedEventArgs e)
		{
			this.isGood = true;

			this.Close();
		}
	}
}
