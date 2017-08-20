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
			
			this.CheckBox_KnownTags.IsChecked = Options.Default.KnownTags;
			this.CheckBox_AddRating.IsChecked = Options.Default.AddRating;
			this.CheckBox_MatchType.IsChecked = Options.Default.CheckMatchType;
			this.Combo_MatchType.SelectedItem = Options.Default.MatchType;
			this.TextBox_MinimumTagsCount.Text = Options.Default.TagsCount.ToString();
			this.Slider_Similarity.Value = Options.Default.Similarity;
			this.Slider_Delay.Value = Options.Default.Delay;
			this.CheckBox_Randomize.IsChecked = Options.Default.Randomize;
			this.CheckBox_AutoMove.IsChecked = Options.Default.AutoMove;

			// Sources
			this.CheckBox_Source_Danbooru.IsChecked = Options.Default.Source_Danbooru;
			this.CheckBox_Source_Konachan.IsChecked = Options.Default.Source_Konachan;
			this.CheckBox_Source_Yandere.IsChecked = Options.Default.Source_Yandere;
			this.CheckBox_Source_Gelbooru.IsChecked = Options.Default.Source_Gelbooru;
			this.CheckBox_Source_SankakuChannel.IsChecked = Options.Default.Source_SankakuChannel;
			this.CheckBox_Source_Eshuushuu.IsChecked = Options.Default.Source_Eshuushuu;
			this.CheckBox_Source_TheAnimeGallery.IsChecked = Options.Default.Source_TheAnimeGallery;
			this.CheckBox_Source_Zerochan.IsChecked = Options.Default.Source_Zerochan;
			this.CheckBox_Source_AnimePictures.IsChecked = Options.Default.Source_AnimePictures;

			this.UpdateLabels();
			this.ShowDialog();
		}

		/*
		============================================
		Private
		============================================
		*/

		private void UpdateLabels()
		{
			int delay = (int)this.Slider_Delay.Value;
			int half = delay / 2;
			int min = delay - half;
			int max = delay + half;

			this.Label_Similarity.Content = "Minimum similarity (" + (int)this.Slider_Similarity.Value + "%)";
			this.Label_Delay.Content = "Delay (" + delay + "secs / " + (delay / 60) + "mins)";
			this.CheckBox_Randomize.Content = "Randomize the delay (" + min + "~" + max + " secs / " + (min / 60) + "~" + (max / 60) + " mins)";
		}

		/*
		============================================
		Event
		============================================
		*/

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			Options.Default.KnownTags = (bool)this.CheckBox_KnownTags.IsChecked;
			Options.Default.AddRating = (bool)this.CheckBox_AddRating.IsChecked;
			Options.Default.CheckMatchType = (bool)this.CheckBox_MatchType.IsChecked;
			Options.Default.MatchType = (IqdbApi.Enums.MatchType)this.Combo_MatchType.SelectedItem;
			Options.Default.TagsCount = Int32.Parse(this.TextBox_MinimumTagsCount.Text);
			Options.Default.Similarity = (byte)this.Slider_Similarity.Value;
			Options.Default.Delay = (int)this.Slider_Delay.Value;
			Options.Default.Randomize = (bool)this.CheckBox_Randomize.IsChecked;
			Options.Default.AutoMove = (bool)this.CheckBox_AutoMove.IsChecked;

			// Sources
			Options.Default.Source_Danbooru = (bool)this.CheckBox_Source_Danbooru.IsChecked;
			Options.Default.Source_Konachan = (bool)this.CheckBox_Source_Konachan.IsChecked;
			Options.Default.Source_Yandere = (bool)this.CheckBox_Source_Yandere.IsChecked;
			Options.Default.Source_Gelbooru = (bool)this.CheckBox_Source_Gelbooru.IsChecked;
			Options.Default.Source_SankakuChannel =(bool)this.CheckBox_Source_SankakuChannel.IsChecked;
			Options.Default.Source_Eshuushuu = (bool)this.CheckBox_Source_Eshuushuu.IsChecked;
			Options.Default.Source_TheAnimeGallery = (bool)this.CheckBox_Source_TheAnimeGallery.IsChecked;
			Options.Default.Source_Zerochan = (bool)this.CheckBox_Source_Zerochan.IsChecked;
			Options.Default.Source_AnimePictures = (bool)this.CheckBox_Source_AnimePictures.IsChecked;

			Options.Default.Save();

			this.Close();
		}

		private void CheckBox_MatchType_Click(object sender, RoutedEventArgs e)
		{
			this.Label_MatchType.IsEnabled = (bool)this.CheckBox_MatchType.IsChecked;
			this.Combo_MatchType.IsEnabled = (bool)this.CheckBox_MatchType.IsChecked;
		}

		private void Sliders_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (this.IsLoaded) {
				this.UpdateLabels();
			}
		}
	}
}
