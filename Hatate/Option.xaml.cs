using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using Options = Hatate.Properties.Settings;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using CheckBox = System.Windows.Controls.CheckBox;
using ListViewItem = System.Windows.Controls.ListViewItem;

namespace Hatate
{
	public enum ParenthesisValue : byte
	{
		NumberOfTags = 1,
		NumberOfMatches,
		MatchSource,
		MatchSimilarity,
		HighestSimilarity
	}

	public enum RetryMethod : byte
	{
		DontRetry,
		SameEngine,
		OtherEngine
	}

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
			foreach (var value in Enum.GetValues(typeof(IqdbApi.Enums.MatchType))) {
				Combo_MatchType.Items.Add(value);
			}

			// Add search engines
			foreach (var value in Enum.GetValues(typeof(SearchEngine))) {
				Combo_SearchEngines.Items.Add(value);
			}

			// Add sources
			List<CheckBox> checkboxes = new List<CheckBox>();
			checkboxes.Add(new CheckBox() { Content = "Danbooru", Tag = Math.Abs(Options.Default.Source_Danbooru), IsChecked = Options.Default.Source_Danbooru > 0 });
			checkboxes.Add(new CheckBox() { Content = "Konachan", Tag = Math.Abs(Options.Default.Source_Konachan), IsChecked = Options.Default.Source_Konachan > 0 });
			checkboxes.Add(new CheckBox() { Content = "Yandere", Tag = Math.Abs(Options.Default.Source_Yandere), IsChecked = Options.Default.Source_Yandere > 0 });
			checkboxes.Add(new CheckBox() { Content = "Gelbooru", Tag = Math.Abs(Options.Default.Source_Gelbooru), IsChecked = Options.Default.Source_Gelbooru > 0 });
			checkboxes.Add(new CheckBox() { Content = "SankakuChannel", Tag = Math.Abs(Options.Default.Source_SankakuChannel), IsChecked = Options.Default.Source_SankakuChannel > 0 });
			checkboxes.Add(new CheckBox() { Content = "Eshuushuu", Tag = Math.Abs(Options.Default.Source_Eshuushuu), IsChecked = Options.Default.Source_Eshuushuu > 0 });
			checkboxes.Add(new CheckBox() { Content = "TheAnimeGallery", Tag = Math.Abs(Options.Default.Source_TheAnimeGallery), IsChecked = Options.Default.Source_TheAnimeGallery > 0 });
			checkboxes.Add(new CheckBox() { Content = "Zerochan", Tag = Math.Abs(Options.Default.Source_Zerochan), IsChecked = Options.Default.Source_Zerochan > 0 });
			checkboxes.Add(new CheckBox() { Content = "AnimePictures", Tag = Math.Abs(Options.Default.Source_AnimePictures), IsChecked = Options.Default.Source_AnimePictures > 0 });
			checkboxes.Add(new CheckBox() { Content = "Pivix", Tag = Math.Abs(Options.Default.Source_Pixiv), IsChecked = Options.Default.Source_Pixiv > 0 });
			checkboxes.Add(new CheckBox() { Content = "Twitter", Tag = Math.Abs(Options.Default.Source_Twitter), IsChecked = Options.Default.Source_Twitter > 0 });
			checkboxes.Add(new CheckBox() { Content = "Nico Nico Seiga", Tag = Math.Abs(Options.Default.Source_Seiga), IsChecked = Options.Default.Source_Seiga > 0 });
			checkboxes.Add(new CheckBox() { Content = "DeviantArt", Tag = Math.Abs(Options.Default.Source_DeviantArt), IsChecked = Options.Default.Source_DeviantArt > 0 });
			checkboxes.Add(new CheckBox() { Content = "Pawoo", Tag = Math.Abs(Options.Default.Source_Pawoo), IsChecked = Options.Default.Source_Pawoo > 0 });
			checkboxes.Add(new CheckBox() { Content = "MangaDex", Tag = Math.Abs(Options.Default.Source_MangaDex), IsChecked = Options.Default.Source_MangaDex > 0 });
			checkboxes.Add(new CheckBox() { Content = "ArtStation", Tag = Math.Abs(Options.Default.Source_ArtStation), IsChecked = Options.Default.Source_ArtStation > 0 });
			checkboxes.Add(new CheckBox() { Content = "Other sources", Tag = Math.Abs(Options.Default.Source_Other), IsChecked = Options.Default.Source_Other > 0 });

			// Retry method
			switch (Options.Default.RetryMethod) {
				case (byte)RetryMethod.DontRetry: this.RadioButton_DontRetry.IsChecked = true; break;
				case (byte)RetryMethod.SameEngine: this.RadioButton_RetrySameEngine.IsChecked = true; break;
				case (byte)RetryMethod.OtherEngine: this.RadioButton_RetryOtherEngine.IsChecked = true; break;
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
			this.Slider_SimilarityThreshold.Value = Options.Default.SimilarityThreshold;

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

			this.Slider_SimilarityThreshold.ToolTip = this.Label_SimilarityThreshold.ToolTip;

			this.UpdateSources(checkboxes);
			this.UpdateLabels();

			// Create sources list context menu
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item.Header = "Move up";
			item.Tag = "up";
			item.Click += this.ContextMenu_MenuItem_MoveSourceUpOrDown;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Move down";
			item.Tag = "down";
			item.Click += this.ContextMenu_MenuItem_MoveSourceUpOrDown;
			context.Items.Add(item);

			this.ListView_Sources.ContextMenu = context;
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

		private void UpdateSources(List<CheckBox> checkboxes)
		{
			// Sort by tag
			checkboxes = checkboxes.OrderBy(checkbox => (sbyte)checkbox.Tag).ToList();

			foreach (CheckBox checkbox in checkboxes) {
				this.ListView_Sources.Items.Add(checkbox);
			}
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
			Options.Default.SimilarityThreshold = (byte)this.Slider_SimilarityThreshold.Value;

			// Sources
			foreach (CheckBox checkbox in this.ListView_Sources.Items) {
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

			// Retry method
			if ((bool)this.RadioButton_RetrySameEngine.IsChecked) {
				Options.Default.RetryMethod = (byte)RetryMethod.SameEngine;
			} else if ((bool)this.RadioButton_RetryOtherEngine.IsChecked) {
				Options.Default.RetryMethod = (byte)RetryMethod.OtherEngine;
			} else {
				Options.Default.RetryMethod = (byte)RetryMethod.DontRetry;
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

			this.Label_Retry.Content = "When a search with " + (iqdbIsSelected ? "IQDB" : "SauceNAO") + " gives no result:";
			this.RadioButton_RetryOtherEngine.Content = "Retry with " + (iqdbIsSelected ? "SauceNAO" : "IQDB");
		}

		private void ListView_Sources_PreviewMouseMoveEvent(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (sender is ListViewItem && e.RightButton == System.Windows.Input.MouseButtonState.Pressed) {
				ListViewItem draggedItem = sender as ListViewItem;
				DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
				draggedItem.IsSelected = true;
			}
		}

		private void ListView_Sources_Drop(object sender, DragEventArgs e)
		{
			CheckBox droppedData = e.Data.GetData(typeof(CheckBox)) as CheckBox;
			CheckBox target = ((ListViewItem)(sender)).DataContext as CheckBox;
			sbyte removedIndex = (sbyte)ListView_Sources.Items.IndexOf(droppedData);
			sbyte targetIndex = (sbyte)ListView_Sources.Items.IndexOf(target);

			if (removedIndex == targetIndex) {
				return;
			}

			this.ListView_Sources.Items.RemoveAt(removedIndex);
			this.ListView_Sources.Items.Insert(targetIndex, droppedData);

			// Update tag indexes for ordering
			foreach (CheckBox checkbox in this.ListView_Sources.Items) {
				sbyte index = (sbyte)(1 + this.ListView_Sources.Items.IndexOf(checkbox));

				if (!(bool)checkbox.IsChecked) {
					index *= -1;
				}

				checkbox.Tag = index;
			}
		}

		private void Slider_SimilarityThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			this.Label_SimilarityThreshold.Content = "Similarity threshold (" + (byte)this.Slider_SimilarityThreshold.Value + "%)";
		}

		private void ContextMenu_MenuItem_MoveSourceUpOrDown(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			if (mi == null) {
				return;
			}

			bool up = (string)mi.Tag == "up";

			// Don't move source up if it's already at the top
			if (up && this.ListView_Sources.SelectedIndex < 1) {
				return;
			} else if (!up && this.ListView_Sources.SelectedIndex == this.ListView_Sources.Items.Count - 1) {
				return;
			}

			CheckBox selectedItem = this.ListView_Sources.SelectedItem as CheckBox;
			CheckBox otherItem = this.ListView_Sources.Items.GetItemAt(this.ListView_Sources.SelectedIndex + (up ? -1 : 1)) as CheckBox;

			// Exchange tag with the item above
			sbyte selectedItemTag = sbyte.Parse(selectedItem.Tag.ToString());
			sbyte otherItemTag = sbyte.Parse(otherItem.Tag.ToString());

			if (selectedItemTag == otherItemTag) {
				if (up) otherItemTag -= 1;
				else otherItemTag += 1;
			}

			selectedItem.Tag = otherItemTag;
			otherItem.Tag = selectedItemTag;

			// Build new list
			List<CheckBox> checkboxes = new List<CheckBox>();

			foreach (CheckBox checkbox in this.ListView_Sources.Items) {
				checkboxes.Add(checkbox);
			}

			// Update sources
			this.ListView_Sources.Items.Clear();
			this.UpdateSources(checkboxes);
		}
	}
}
