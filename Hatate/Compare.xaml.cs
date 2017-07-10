using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for Compare.xaml
	/// </summary>
	public partial class Compare : Window
	{
		private bool isGood = false;

		public Compare(string thumb, string match, List<string> tagList)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.Image_Original.Source = new BitmapImage(new Uri(thumb));
			this.Image_Match.Source = new BitmapImage(new Uri(match));

			this.Label_Counters.Content = tagList.Count + " tags found";

			foreach (string tag in tagList) {
				this.ListBox_Tags.Items.Add(tag);
			}

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
