using System;
using System.Windows;
using System.Windows.Media.Imaging;

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

			this.Owner = App.Current.MainWindow;

			this.Image_Original.Source = new BitmapImage(new Uri(thumb));
			this.Image_Match.Source = new BitmapImage(new Uri(match));

			this.ShowDialog();
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public bool IsGood
		{
			get { return this.isGood; }
		}

		/*
		============================================
		Event
		============================================
		*/

		private void Button_Good_Click(object sender, RoutedEventArgs e)
		{
			this.isGood = true;

			this.Close();
		}
	}
}
