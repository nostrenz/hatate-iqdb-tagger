using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using Options = Hatate.Properties.Settings;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for Option.xaml
	/// </summary>
	public partial class Option : Window
	{
		public const byte PARENTHESIS_VALUE_NUMBER_OF_TAGS      = 1;
		public const byte PARENTHESIS_VALUE_NUMBER_OF_MATCHES   = 2;
		public const byte PARENTHESIS_VALUE_MATCH_SOURCE        = 3;
		public const byte PARENTHESIS_VALUE_MATCH_SIMILARITY    = 4;
		public const byte PARENTHESIS_VALUE_HIGHEST_SIMILARITY  = 5;

		// If the list of results needs to be refreshed after closing the options window
		private bool listRefreshRequired = false;

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

			// Add sources
			List<System.Windows.Controls.CheckBox> checkboxes = new List<System.Windows.Controls.CheckBox>();
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Danbooru", Tag = Math.Abs(Options.Default.Source_Danbooru), IsChecked = Options.Default.Source_Danbooru > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Konachan", Tag = Math.Abs(Options.Default.Source_Konachan), IsChecked = Options.Default.Source_Konachan > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Yandere", Tag = Math.Abs(Options.Default.Source_Yandere), IsChecked = Options.Default.Source_Yandere > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Gelbooru", Tag = Math.Abs(Options.Default.Source_Gelbooru), IsChecked = Options.Default.Source_Gelbooru > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "SankakuChannel", Tag = Math.Abs(Options.Default.Source_SankakuChannel), IsChecked = Options.Default.Source_SankakuChannel > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Eshuushuu", Tag = Math.Abs(Options.Default.Source_Eshuushuu), IsChecked = Options.Default.Source_Eshuushuu > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "TheAnimeGallery", Tag = Math.Abs(Options.Default.Source_TheAnimeGallery), IsChecked = Options.Default.Source_TheAnimeGallery > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Zerochan", Tag = Math.Abs(Options.Default.Source_Zerochan), IsChecked = Options.Default.Source_Zerochan > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "AnimePictures", Tag = Math.Abs(Options.Default.Source_AnimePictures), IsChecked = Options.Default.Source_AnimePictures > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Pivix", Tag = Math.Abs(Options.Default.Source_Pixiv), IsChecked = Options.Default.Source_Pixiv > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Twitter", Tag = Math.Abs(Options.Default.Source_Twitter), IsChecked = Options.Default.Source_Twitter > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Nico Nico Seiga", Tag = Math.Abs(Options.Default.Source_Seiga), IsChecked = Options.Default.Source_Seiga > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "DeviantArt", Tag = Math.Abs(Options.Default.Source_DeviantArt), IsChecked = Options.Default.Source_DeviantArt > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Pawoo", Tag = Math.Abs(Options.Default.Source_Pawoo), IsChecked = Options.Default.Source_Pawoo > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "MangaDex", Tag = Math.Abs(Options.Default.Source_MangaDex), IsChecked = Options.Default.Source_MangaDex > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "ArtStation", Tag = Math.Abs(Options.Default.Source_ArtStation), IsChecked = Options.Default.Source_ArtStation > 0 });
			checkboxes.Add(new System.Windows.Controls.CheckBox() { Content = "Other sources", Tag = Math.Abs(Options.Default.Source_Other), IsChecked = Options.Default.Source_Other > 0 });

			// Sort sources
			checkboxes = checkboxes.OrderBy(checkbox => (sbyte)checkbox.Tag).ToList();

			foreach (System.Windows.Controls.CheckBox checkbox in checkboxes) {
				this.ListView_Sources.Items.Add(checkbox);
			}

			// Parenthesis value
			switch (Options.Default.SearchedParenthesisValue) {
				case PARENTHESIS_VALUE_NUMBER_OF_TAGS: this.RadioButton_Parenthesis_NumberOfTags.IsChecked = true; break;
				case PARENTHESIS_VALUE_NUMBER_OF_MATCHES: this.RadioButton_Parenthesis_NumberOfMatches.IsChecked = true; break;
				case PARENTHESIS_VALUE_MATCH_SOURCE: this.RadioButton_Parenthesis_MatchSource.IsChecked = true; break;
				case PARENTHESIS_VALUE_MATCH_SIMILARITY: this.RadioButton_Parenthesis_MatchSimilarity.IsChecked = true; break;
				case PARENTHESIS_VALUE_HIGHEST_SIMILARITY: this.RadioButton_Parenthesis_HighestSimilarity.IsChecked = true; break;
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
			this.TextBox_SimilarityThreshold.Text = Options.Default.SimilarityThreshold.ToString();

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
		Accessor
		============================================
		*/

		public bool ListRefreshRequired
		{
			get { return this.listRefreshRequired; } 
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
			foreach (System.Windows.Controls.CheckBox checkbox in this.ListView_Sources.Items) {
				sbyte index = (sbyte)(1 + this.ListView_Sources.Items.IndexOf(checkbox));

				if (!(bool)checkbox.IsChecked) {
					index *= -1;
				}

				switch (checkbox.Content.ToString()) {
					case "Danbooru": Options.Default.Source_Danbooru = index; break;
					case "Konachan": Options.Default.Source_Konachan = index; break;
					case "Yandere": Options.Default.Source_Yandere = index; break;
					case "Gelbooru": Options.Default.Source_Gelbooru = index; break;
					case "SankakuChannel": Options.Default.Source_SankakuChannel = index; break;
					case "Eshuushuu": Options.Default.Source_Eshuushuu = index; break;
					case "TheAnimeGallery": Options.Default.Source_TheAnimeGallery = index; break;
					case "Zerochan": Options.Default.Source_Zerochan = index; break;
					case "AnimePictures": Options.Default.Source_AnimePictures = index; break;
					case "Pixiv": Options.Default.Source_Pixiv = index; break;
					case "Twitter": Options.Default.Source_Twitter = index; break;
					case "Nico Nico Seiga": Options.Default.Source_Seiga = index; break;
					case "DeviantArt": Options.Default.Source_DeviantArt = index; break;
					case "ArtStation": Options.Default.Source_ArtStation = index; break;
					case "Pawoo": Options.Default.Source_Pawoo = index; break;
					case "MangaDex": Options.Default.Source_MangaDex = index; break;
					case "Other sources": Options.Default.Source_Other = index; break;
				}
			}

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

			byte previousParenthesisValue = Options.Default.SearchedParenthesisValue;

			// Parenthesis value
			if ((bool)this.RadioButton_Parenthesis_NumberOfTags.IsChecked) {
				Options.Default.SearchedParenthesisValue = PARENTHESIS_VALUE_NUMBER_OF_TAGS;
			} else if ((bool)this.RadioButton_Parenthesis_NumberOfMatches.IsChecked) {
				Options.Default.SearchedParenthesisValue = PARENTHESIS_VALUE_NUMBER_OF_MATCHES;
			} else if ((bool)this.RadioButton_Parenthesis_MatchSource.IsChecked) {
				Options.Default.SearchedParenthesisValue = PARENTHESIS_VALUE_MATCH_SOURCE;
			} else if ((bool)this.RadioButton_Parenthesis_MatchSimilarity.IsChecked) {
				Options.Default.SearchedParenthesisValue = PARENTHESIS_VALUE_MATCH_SIMILARITY;
			} else if ((bool)this.RadioButton_Parenthesis_HighestSimilarity.IsChecked) {
				Options.Default.SearchedParenthesisValue = PARENTHESIS_VALUE_HIGHEST_SIMILARITY;
			}

			// We'll need to refresh the list
			if (previousParenthesisValue != Options.Default.SearchedParenthesisValue) {
				this.listRefreshRequired = true;
			}

			int thumbWidth = 0;
			int.TryParse(this.TextBox_ThumbWidth.Text, out thumbWidth);
			Options.Default.ThumbWidth = thumbWidth > 0 ? thumbWidth : 150;

			byte similarityThreshold = 0;
			byte.TryParse(this.TextBox_SimilarityThreshold.Text, out similarityThreshold);
			Options.Default.SimilarityThreshold = (similarityThreshold >= 0 && similarityThreshold <= 100) ? similarityThreshold : (byte)5;

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

		private void ListView_Sources_PreviewMouseMoveEvent(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (sender is System.Windows.Controls.ListBoxItem && e.RightButton == System.Windows.Input.MouseButtonState.Pressed) {
				System.Windows.Controls.ListBoxItem draggedItem = sender as System.Windows.Controls.ListBoxItem;
				DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
				draggedItem.IsSelected = true;
			}
		}

		private void ListView_Sources_Drop(object sender, DragEventArgs e)
		{
			System.Windows.Controls.CheckBox droppedData = e.Data.GetData(typeof(System.Windows.Controls.CheckBox)) as System.Windows.Controls.CheckBox;
			System.Windows.Controls.CheckBox target = ((System.Windows.Controls.ListBoxItem)(sender)).DataContext as System.Windows.Controls.CheckBox;
			int removedIndex = ListView_Sources.Items.IndexOf(droppedData);
			int targetIndex = ListView_Sources.Items.IndexOf(target);

			if (removedIndex == targetIndex) {
				return;
			}

			if (removedIndex < targetIndex) {
				this.ListView_Sources.Items.RemoveAt(removedIndex);
				this.ListView_Sources.Items.Insert(targetIndex, droppedData);
			} else {
				this.ListView_Sources.Items.RemoveAt(removedIndex);
				this.ListView_Sources.Items.Insert(targetIndex, droppedData);
			}
		}
	}
}
