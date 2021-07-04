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

			// Add search engines
			foreach (var item in Enum.GetValues(typeof(SearchEngine))) {
				Combo_SearchEngines.Items.Add(item);
			}

			this.CheckBox_AddRating.IsChecked = Options.Default.AddRating;
			this.CheckBox_MatchType.IsChecked = Options.Default.CheckMatchType;
			this.Combo_MatchType.SelectedItem = Options.Default.MatchType;
			this.TextBox_MinimumTagsCount.Text = Options.Default.TagsCount.ToString();
			this.Slider_Similarity.Value = Options.Default.Similarity;
			this.Slider_Delay.Value = Options.Default.Delay;
			this.CheckBox_Randomize.IsChecked = Options.Default.Randomize;
			this.CheckBox_AskTags.IsChecked = Options.Default.AskTags;
			this.CheckBox_LogMatchedUrls.IsChecked = Options.Default.LogMatchedUrls;
			this.CheckBox_ParseTags.IsChecked = Options.Default.ParseTags;
			this.CheckBox_ResizeImage.IsChecked = Options.Default.ResizeImage;
			this.TextBox_ThumbWidth.Text = Options.Default.ThumbWidth.ToString();
			this.Combo_SearchEngines.SelectedItem = (SearchEngine)Options.Default.SearchEngine;
			this.CheckBox_RemoveResultAfter.IsChecked = Options.Default.RemoveResultAfter;
			this.CheckBox_StartupReleaseCheck.IsChecked = Options.Default.StartupReleaseCheck;

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
			this.CheckBox_Source_Pixiv.IsChecked = Options.Default.Source_Pixiv;
			this.CheckBox_Source_Twitter.IsChecked = Options.Default.Source_Twitter;
			this.CheckBox_Source_Seiga.IsChecked = Options.Default.Source_Seiga;
			this.CheckBox_Source_DeviantArt.IsChecked = Options.Default.Source_DeviantArt;
			this.CheckBox_Source_ArtStation.IsChecked = Options.Default.Source_ArtStation;
			this.CheckBox_Source_Pawoo.IsChecked = Options.Default.Source_Pawoo;
			this.CheckBox_Source_MangaDex.IsChecked = Options.Default.Source_MangaDex;
			this.CheckBox_Source_Other.IsChecked = Options.Default.Source_Other;

			// Tags
			this.CheckBox_AddFoundTag.IsChecked = Options.Default.AddFoundTag;
			this.CheckBox_AddNotfoundTag.IsChecked = Options.Default.AddNotfoundTag;
			this.CheckBox_AddTaggedTag.IsChecked = Options.Default.AddTaggedTag;
			this.TextBox_FoundTag.Text = Options.Default.FoundTag;
			this.TextBox_NotfoundTag.Text = Options.Default.NotfoundTag;
			this.TextBox_TaggedTag.Text = Options.Default.TaggedTag;
			this.TextBox_FoundTag.IsEnabled = Options.Default.AddFoundTag;
			this.TextBox_NotfoundTag.IsEnabled = Options.Default.AddNotfoundTag;
			this.TextBox_TaggedTag.IsEnabled = Options.Default.AddTaggedTag;

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
			Options.Default.AddRating = (bool)this.CheckBox_AddRating.IsChecked;
			Options.Default.CheckMatchType = (bool)this.CheckBox_MatchType.IsChecked;
			Options.Default.MatchType = (IqdbApi.Enums.MatchType)this.Combo_MatchType.SelectedItem;
			Options.Default.TagsCount = Int32.Parse(this.TextBox_MinimumTagsCount.Text);
			Options.Default.Similarity = (byte)this.Slider_Similarity.Value;
			Options.Default.Delay = (int)this.Slider_Delay.Value;
			Options.Default.Randomize = (bool)this.CheckBox_Randomize.IsChecked;
			Options.Default.AskTags = (bool)this.CheckBox_AskTags.IsChecked;
			Options.Default.LogMatchedUrls = (bool)this.CheckBox_LogMatchedUrls.IsChecked;
			Options.Default.ParseTags = (bool)this.CheckBox_ParseTags.IsChecked;
			Options.Default.ResizeImage = (bool)this.CheckBox_ResizeImage.IsChecked;
			Options.Default.SearchEngine = (byte)(SearchEngine)this.Combo_SearchEngines.SelectedItem;
			Options.Default.RemoveResultAfter = (bool)this.CheckBox_RemoveResultAfter.IsChecked;
			Options.Default.StartupReleaseCheck = (bool)this.CheckBox_StartupReleaseCheck.IsChecked;

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
			Options.Default.Source_Pixiv = (bool)this.CheckBox_Source_Pixiv.IsChecked;
			Options.Default.Source_Twitter = (bool)this.CheckBox_Source_Twitter.IsChecked;
			Options.Default.Source_Seiga = (bool)this.CheckBox_Source_Seiga.IsChecked;
			Options.Default.Source_DeviantArt = (bool)this.CheckBox_Source_DeviantArt.IsChecked;
			Options.Default.Source_ArtStation = (bool)this.CheckBox_Source_ArtStation.IsChecked;
			Options.Default.Source_Pawoo = (bool)this.CheckBox_Source_Pawoo.IsChecked;
			Options.Default.Source_MangaDex = (bool)this.CheckBox_Source_MangaDex.IsChecked;
			Options.Default.Source_Other = (bool)this.CheckBox_Source_Other.IsChecked;

			// Tags
			Options.Default.AddFoundTag = (bool)this.CheckBox_AddFoundTag.IsChecked;
			Options.Default.AddNotfoundTag = (bool)this.CheckBox_AddNotfoundTag.IsChecked;
			Options.Default.AddTaggedTag = (bool)this.CheckBox_AddTaggedTag.IsChecked;
			Options.Default.FoundTag = this.TextBox_FoundTag.Text.Trim();
			Options.Default.NotfoundTag = this.TextBox_NotfoundTag.Text.Trim();
			Options.Default.TaggedTag = this.TextBox_TaggedTag.Text.Trim();

			// Default tags
			if (string.IsNullOrEmpty(Options.Default.FoundTag)) {
				Options.Default.FoundTag = "hatate:found";
				Options.Default.AddFoundTag = false;
			}
			if (string.IsNullOrEmpty(Options.Default.NotfoundTag)) {
				Options.Default.NotfoundTag = "hatate:not found";
				Options.Default.AddNotfoundTag = false;
			}
			if (string.IsNullOrEmpty(Options.Default.TaggedTag)) {
				Options.Default.TaggedTag = "hatate:tagged";
				Options.Default.AddTaggedTag = false;
			}

			int thumbWidth = 0;
			int.TryParse(this.TextBox_ThumbWidth.Text, out thumbWidth);
			Options.Default.ThumbWidth = thumbWidth > 0 ? thumbWidth : 150;

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

		private void CheckBox_AddTag_Click(object sender, RoutedEventArgs e)
		{
			if (sender == this.CheckBox_AddFoundTag) {
				this.TextBox_FoundTag.IsEnabled = (bool)this.CheckBox_AddFoundTag.IsChecked;
			} else if (sender == this.CheckBox_AddNotfoundTag) {
				this.TextBox_NotfoundTag.IsEnabled = (bool)this.CheckBox_AddNotfoundTag.IsChecked;
			} else if (sender == this.CheckBox_AddTaggedTag) {
				this.TextBox_TaggedTag.IsEnabled = (bool)this.CheckBox_AddTaggedTag.IsChecked;
			}
		}

		/// <summary>
		/// Some options are only used for IQDB, enable or diable them depending on which search engine is selected.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Combo_SearchEngines_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			bool iqdbIsSelected = ((SearchEngine)this.Combo_SearchEngines.SelectedItem == SearchEngine.IQDB);

			this.TextBox_MinimumTagsCount.IsEnabled = iqdbIsSelected;
			this.Combo_MatchType.IsEnabled = iqdbIsSelected;
			this.CheckBox_MatchType.IsEnabled = iqdbIsSelected;
		}
	}
}
