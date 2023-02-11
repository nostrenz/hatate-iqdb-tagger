using System.Windows;
using Options = Hatate.Properties.Settings;

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

			// Add search engines
			foreach (var value in System.Enum.GetValues(typeof(Enum.SearchEngine))) {
				Combo_SearchEngines.Items.Add(value);
			}

			// Retry method
			switch (Options.Default.RetryMethod) {
				case (byte)RetryMethod.DontRetry: this.RadioButton_DontRetry.IsChecked = true; break;
				case (byte)RetryMethod.SameEngine: this.RadioButton_RetrySameEngine.IsChecked = true; break;
				case (byte)RetryMethod.OtherEngine: this.RadioButton_RetryOtherEngine.IsChecked = true; break;
			}

			this.CheckBox_AddRating.IsChecked = Options.Default.AddRating;
			this.Slider_Delay.Value = Options.Default.Delay;
			this.CheckBox_Randomize.IsChecked = Options.Default.Randomize;
			this.CheckBox_AskTags.IsChecked = Options.Default.AskTags;
			this.CheckBox_LogMatchedUrls.IsChecked = Options.Default.LogMatchedUrls;
			this.CheckBox_ParseTags.IsChecked = Options.Default.ParseTags;
			this.CheckBox_ResizeImage.IsChecked = Options.Default.ResizeImage;
			this.TextBox_ThumbWidth.Text = Options.Default.ThumbWidth.ToString();
			this.Combo_SearchEngines.SelectedItem = (Enum.SearchEngine)Options.Default.SearchEngine;
			this.CheckBox_RemoveResultAfter.IsChecked = Options.Default.RemoveResultAfter;
			this.CheckBox_StartupReleaseCheck.IsChecked = Options.Default.StartupReleaseCheck;
			

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

			this.Label_Delay.Content = "Delay between searches (" + delay + "secs / " + (delay / 60) + "mins)";
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
			Options.Default.Delay = (int)this.Slider_Delay.Value;
			Options.Default.Randomize = (bool)this.CheckBox_Randomize.IsChecked;
			Options.Default.AskTags = (bool)this.CheckBox_AskTags.IsChecked;
			Options.Default.LogMatchedUrls = (bool)this.CheckBox_LogMatchedUrls.IsChecked;
			Options.Default.ParseTags = (bool)this.CheckBox_ParseTags.IsChecked;
			Options.Default.ResizeImage = (bool)this.CheckBox_ResizeImage.IsChecked;
			Options.Default.SearchEngine = (byte)(Enum.SearchEngine)this.Combo_SearchEngines.SelectedItem;
			Options.Default.RemoveResultAfter = (bool)this.CheckBox_RemoveResultAfter.IsChecked;
			Options.Default.StartupReleaseCheck = (bool)this.CheckBox_StartupReleaseCheck.IsChecked;

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
			bool iqdbIsSelected = ((Enum.SearchEngine)this.Combo_SearchEngines.SelectedItem == Enum.SearchEngine.IQDB);

			this.Label_Retry.Content = "When a search with " + (iqdbIsSelected ? "IQDB" : "SauceNAO") + " gives no result:";
			this.RadioButton_RetryOtherEngine.Content = "Retry with " + (iqdbIsSelected ? "SauceNAO" : "IQDB");
		}

        private void Button_ValidMatchRules_Click(object sender, RoutedEventArgs e)
        {
			new View.Window.ValidMatchRules(this).ShowDialog();
        }

        private void Button_BetterImageRules_Click(object sender, RoutedEventArgs e)
        {
			new View.Window.BetterImageRules(this).ShowDialog();
        }
    }
}
