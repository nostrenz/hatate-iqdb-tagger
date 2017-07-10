using System;
using System.Windows;
using Options = Hatate.Properties.Settings;

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

			// Add match types
			foreach (var item in Enum.GetValues(typeof(IqdbApi.Enums.MatchType))) {
				Combo_MatchType.Items.Add(item);
			}

			this.CheckBox_Compare.IsChecked = Options.Default.Compare;
			this.CheckBox_KnownTags.IsChecked = Options.Default.KnownTags;
			this.CheckBox_MatchType.IsChecked = Options.Default.CheckMatchType;
			this.Combo_MatchType.SelectedItem = Options.Default.MatchType;
			this.TextBox_MinimumTagsCount.Text = Options.Default.TagsCount.ToString();
			this.Slider_Similarity.Value = Options.Default.Similarity;

			this.UpdateLabel();
			this.ShowDialog();
		}

		/*
		============================================
		Private
		============================================
		*/

		private void UpdateLabel()
		{
			this.Label_Similarity.Content = "Minimum similarity (" + (int)this.Slider_Similarity.Value + "%)";
		}

		/*
		============================================
		Event
		============================================
		*/

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			Options.Default.Compare = (bool)this.CheckBox_Compare.IsChecked;
			Options.Default.KnownTags = (bool)this.CheckBox_KnownTags.IsChecked;
			Options.Default.CheckMatchType = (bool)this.CheckBox_MatchType.IsChecked;
			Options.Default.MatchType = (IqdbApi.Enums.MatchType)this.Combo_MatchType.SelectedItem;
			Options.Default.TagsCount = Int32.Parse(this.TextBox_MinimumTagsCount.Text);
			Options.Default.Similarity = (byte)Slider_Similarity.Value;

			Options.Default.Save();

			this.Close();
		}

		private void Slider_Similarity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (this.IsLoaded) {
				this.UpdateLabel();
			}
		}

		private void CheckBox_MatchType_Click(object sender, RoutedEventArgs e)
		{
			this.Label_MatchType.IsEnabled = (bool)this.CheckBox_MatchType.IsChecked;
			this.Combo_MatchType.IsEnabled = (bool)this.CheckBox_MatchType.IsChecked;
		}
	}
}
