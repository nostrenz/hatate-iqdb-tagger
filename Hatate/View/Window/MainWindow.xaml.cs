using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using FileIO = Microsoft.VisualBasic.FileIO;
using Directory = System.IO.Directory;
using Options = Hatate.Properties.Settings;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Brush = System.Windows.Media.Brush;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Timer = System.Windows.Forms.Timer;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const int MAX_PATH_LENGTH = 260;

		const string TXT_IGNOREDS     = "ignoreds.txt";
		const string TXT_MATCHED_URLS = "matched_urls.txt";

		// Tags list
		private List<string> ignoreds;

		private int lastSearchedInSeconds = 0;
		private int found = 0;
		private int notFound = 0;
		private int delay = 0;
		private bool retrySearch = false;
		private Timer timer = new Timer();
		private Compare compareWindow = null;

		// List of accepted image extentions
		private string[] imagesFilesExtensions = new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".jfif", ".webp", ".tiff" };

		public MainWindow()
		{
			InitializeComponent();

			this.LoadIgnoredTags();

			this.CreateFilesListContextMenu();
			this.CreateTagsListContextMenu();
			this.CreateIgnoredsListContextMenu();

			this.Label_SourceInfos.Content = "";
			this.Label_MatchInfos.Content = "";

			// Prevent closing the window if we have some search results left
			this.Closing += new System.ComponentModel.CancelEventHandler(CustomClosing);

			// Populate the View menu
			this.AddParenthesisValueRadioToViewMenu("Number of tags", ParenthesisValue.NumberOfTags);
			this.AddParenthesisValueRadioToViewMenu("Number of matches", ParenthesisValue.NumberOfMatches);
			this.AddParenthesisValueRadioToViewMenu("Highest similarity", ParenthesisValue.HighestSimilarity);
			this.AddParenthesisValueRadioToViewMenu("Match similarity", ParenthesisValue.MatchSimilarity);
			this.AddParenthesisValueRadioToViewMenu("Match source", ParenthesisValue.MatchSource);
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Select multiple files to be added to the list.
		/// </summary>
		private void AddFiles()
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Image files|*.jpg;*.png;*.bmp;*.jpeg;*.webp,*.tiff";
			dlg.Multiselect = true;

			if (!(bool)dlg.ShowDialog()) {
				return;
			}

			// Ask for tags if enabled
			List<Tag> tags = this.AskForNewTags();

			foreach (string filename in dlg.FileNames) {
				this.AddFileToList(filename, tags);
			}

			this.UpdateLabels();
			this.ChangeStartButtonEnabledValue();
		}

		/// <summary>
		/// Select all the files from a folder to be added to the list.
		/// </summary>
		private void AddFolder()
		{
			using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog()) {
				System.Windows.Forms.DialogResult result = fbd.ShowDialog();

				if (result != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
					return;
				}

				// Add files to the list
				this.GetImagesFromFolder(fbd.SelectedPath);
			}

			this.UpdateLabels();
			this.ChangeStartButtonEnabledValue();
		}

		/// <summary>
		/// Load known tags from text files.
		/// </summary>
		private void LoadIgnoredTags()
		{
			string txtPath = this.IgnoredsTxtPath;

			if (File.Exists(txtPath)) {
				this.ignoreds = new List<string>(File.ReadAllLines(txtPath));
			}

			this.SetStatus("Tags loaded.");
		}

		/// <summary>
		/// Get all the images in the working directory and add them to the list.
		/// </summary>
		private void GetImagesFromFolder(string path)
		{
			string[] files = "*.jpg|*.jpeg|*.png|*.bmp".Split('|').SelectMany(filter => Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly)).ToArray();

			// Ask for tags if enabled
			List<Tag> tags = this.AskForNewTags();

			foreach (string file in files) {
				this.AddFileToList(file, tags);
			}

			int count = files.Length;
			this.Button_Start.IsEnabled = (count > 0);

			this.UpdateLabels();
			this.SetStatus(count > 0 ? "Ready." : "No images found.");
		}

		/// <summary>
		/// Get some informations about the local image attached to a Result and also generate a smaller image.
		/// </summary>
		/// <param name="result"></param>
		/// <returns>
		/// True if the file was successfuly read, false otherwise.
		/// </returns>
		private bool ReadLocalImage(Result result)
		{
			if (result == null) {
				return false;
			}

			// Don't read the local image as it's already a thumbnail and we already have informations about it from Hydrus metadata
			if (result.HydrusFileId != null) {
				result.ThumbPath = result.ImagePath;

				return true;
			}

			// Thumbnail already exists, get missing infos about it if missing
			if (File.Exists(result.ThumbPath)) {
				if (result.Local.Width == 0 || result.Local.Height == 0) {
					try {
						System.Drawing.Image img = System.Drawing.Image.FromFile(result.ImagePath);

						result.Local.Width = img.Width;
						result.Local.Height = img.Height;

						img.Dispose();
					} catch (OutOfMemoryException) { }
				}

				if (result.Local.Size == 0) {
					result.Local.Size = new FileInfo(result.ImagePath).Length;
				}

				return true;
			}

			string thumbsDir = App.ThumbsDirPath;

			result.Local.Format = result.ImagePath.Substring(result.ImagePath.LastIndexOf('.') + 1);
			result.ThumbPath = thumbsDir + Guid.NewGuid().ToString() + '.' + result.Local.Format;

			Directory.CreateDirectory(thumbsDir);

			// We'll generate a thumbnail to be uploaded
			System.Drawing.Image image = null;

			try {
				image = System.Drawing.Image.FromFile(result.ImagePath);
			} catch (OutOfMemoryException) { // Cannot open file, we'll upload the original file
				result.ThumbPath = result.ImagePath;

				return true;
			} catch (FileNotFoundException) { // Missing file, remove it from the list
				return false;
			}

			result.Local.Width = image.Width;
			result.Local.Height = image.Height;
			result.Local.Size = new FileInfo(result.ImagePath).Length;

			// We don't want to generate a thumbnail or image width is lower than the resized width, we'll upload the original image
			if (!Options.Default.ResizeImage || image.Width <= Options.Default.ThumbWidth) {
				image.Dispose();

				result.ThumbPath = result.ImagePath;

				return true;
			}

			Decimal sizeRatio = ((Decimal)image.Height / image.Width);
			int thumbHeight = Decimal.ToInt32(sizeRatio * Options.Default.ThumbWidth);

			Bitmap bmp = new Bitmap(Options.Default.ThumbWidth, thumbHeight);
			Graphics gr = Graphics.FromImage(bmp);
			Rectangle rectDestination = new Rectangle(0, 0, Options.Default.ThumbWidth, thumbHeight);

			gr.SmoothingMode = SmoothingMode.HighQuality;
			gr.CompositingQuality = CompositingQuality.HighQuality;
			gr.InterpolationMode = InterpolationMode.HighQualityBicubic;

			try {
				gr.DrawImage(image, rectDestination, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
			} catch (OutOfMemoryException) { // Cannot open file, we'll upload the original file
				gr.Dispose();
				bmp.Dispose();
				image.Dispose();

				result.ThumbPath = result.ImagePath;

				return true;
			}

			try {
				bmp.Save(result.ThumbPath, ImageFormat.Jpeg);
			} catch (IOException) { // Cannot save thumbnail, we'll upload the original file
				result.ThumbPath = result.ImagePath;
			}

			// Liberate resources
			gr.Dispose();
			bmp.Dispose();
			image.Dispose();

			return true;
		}

		/// <summary>
		/// Get the next row index in the list.
		/// </summary>
		/// <returns></returns>
		private int GetNextIndex()
		{
			int progress = 0;

			// Will run until the list is empty or every files in it had been searched
			// NOTE: progress is incremented each time the loop doesn't reach the end where it's reset to 0
			while (this.ListBox_Files.Items.Count > 0) {
				// No more files
				if (progress >= this.ListBox_Files.Items.Count) {
					return -1;
				}

				Result result = this.GetResultAt(progress);

				// Already searched
				if (result != null && result.Searched) {
					progress++;

					continue;
				}

				return progress;
			}

			return -1;
		}

		/// <summary>
		/// Execute the next image search.
		/// </summary>
		private async void NextSearch()
		{
			int progress = this.GetNextIndex();

			// No more files to search, end here
			if (progress < 0) {
				this.EndSearch();

				return;
			}

			await this.SearchFile(progress, this.SearchEngine);

			// Refresh the items to update the foreground color from the Result objets
			this.ListBox_Files.Items.Refresh();

			// This is the last search, end here
			if (progress >= this.ListBox_Files.Items.Count - 1 && !this.retrySearch) {
				this.EndSearch();

				return;
			}

			// Wait some time until the next search
			this.delay = Options.Default.Delay;

			// If the delay is 60 seconds, this will randomly change to between 30 and 90 seconds
			if (Options.Default.Randomize) {
				int half = this.delay / 2;

				this.delay += new Random().Next(half * -1, half);
			}

			// Timer was not recreated
			if (this.timer == null) {
				return;
			}

			this.timer.Interval = 1000;
			this.timer.Tick += new EventHandler(Timer_Tick);
			this.timer.Start();
		}

		/// <summary>
		/// Search a single file using its index in the Files list.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private async Task SearchFile(int index, Enum.SearchEngine searchEngine)
		{
			Result result = this.GetResultAt(index);
			bool isRetry = this.retrySearch;

			this.retrySearch = false;

			// Some file format aren't supported by some search engines
			if (!this.ImageFormatIsSupported(result.Local, searchEngine)) {
				this.SetStatus("Image of type " + result.Local.Format + " not supported by " + searchEngine.ToString() + ", skipping.");

				result.AddWarning(this.Label_Status.Content.ToString());
				result.Searched = true;

				return;
			}

			// Generate a smaller image for uploading
			this.SetStatus("Generating thumbnail...");
			bool read = this.ReadLocalImage(result);

			// Unable to read the file
			if (!read) {
				this.RemoveResultFromFilesListbox(result);

				return;
			}

			// Search the image
			switch (searchEngine) {
				case Enum.SearchEngine.IQDB:
					this.SetStatus("Searching file with IQDB...");
					await this.SearchWithIqdb(result);
				break;
				case Enum.SearchEngine.SauceNAO:
					this.SetStatus("Searching file with SauceNAO...");
					await this.SearchWithSauceNao(result);
				break;
			}

			// Image found
			if (result.Found) {
				this.SetStatus("File found.");
				this.found++;
			} else { // Not found on IQDB
				if (!isRetry) {
					if (Options.Default.RetryMethod == (byte)RetryMethod.SameEngine) {
						// Retry with the same search engine
						this.retrySearch = true;

						return;
					} else if (Options.Default.RetryMethod == (byte)RetryMethod.OtherEngine) {
						// Retry with the other search engine
						this.retrySearch = true;
						searchEngine = (searchEngine == Enum.SearchEngine.IQDB ? Enum.SearchEngine.SauceNAO : Enum.SearchEngine.IQDB);

						await this.SearchFile(index, searchEngine);

						return;
					}
				}

				this.SetStatus("File not found.");
				this.notFound++;
			}

			result.Searched = true;

			// Send to Hydrus
			if (result.HasTags && Options.Default.AutoSend) {
				string hydrusPageKey = null;

				if (Options.Default.AddImagesToHydrusPage) {
					hydrusPageKey = await App.hydrusApi.GetPageNamed(Options.Default.HydrusPageName);
				}

				bool success = await this.SendTagsToHydrusForResult(result, hydrusPageKey);

				if (success) {
					this.RemoveResultFromFilesListbox(result);
				} else {
					this.ListBox_Files.SelectedItems.Remove(result);
				}
			}

			// Update informations on the right if the image is selected in the list
			if (this.ListBox_Files.SelectedItem == result) {
				this.UpdateRightView(result);
			}

			// Update counters (remaining, found, not found)
			this.UpdateLabels();
		}

		/// <summary>
		/// Search a Result using IQDB.
		/// </summary>
		/// <param name="api"></param>
		/// <param name="thumbPath"></param>
		/// <param name="filename"></param>
		/// <returns></returns>
		private async Task SearchWithIqdb(Result result)
		{
			FileStream fs;

			try {
				fs = new FileStream(result.ThumbPath, FileMode.Open);
			} catch (IOException) {
				return; // May happen if the file is in use
			}

			IqdbApi.Models.SearchResult iqdbResult = null;

			try {
				iqdbResult = await new IqdbApi.IqdbClient().SearchFile(fs);
			} catch (Exception) {
				// FormatException may happen in case of an invalid HTML response where no tags can be parsed
			}

			result.UsedSearchEngine = Enum.SearchEngine.IQDB;

			// Result(s) found
			if (iqdbResult != null) {
				if (iqdbResult.YourImage != null) {
					result.UploadedImageUrl = "https://iqdb.org" + iqdbResult.YourImage.PreviewUrl;
				}

				if (iqdbResult.Matches != null) {
					this.lastSearchedInSeconds = (int)iqdbResult.SearchedInSeconds;

					result.UseIqdbApiMatches(iqdbResult.Matches);

					// Check for matching results
					this.CheckMatches(result);
				}
			}

			this.PortTagsOfMatchToResult(result);

			fs.Close();
			fs.Dispose();
		}

		/// <summary>
		/// Search a Result using SauceNAO.
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		private async Task SearchWithSauceNao(Result result)
		{
			SauceNao sauceNao = new SauceNao();

			try {
				await sauceNao.SearchFile(result.ThumbPath);
			} catch (Exception) {
				// FormatException may happen in case of an invalid HTML response where no tags can be parsed
			}

			if (sauceNao.DailyLimitExceeded) {
				MessageBox.Show(
					"Daily Search Limit Exceeded.",
					"SauceNAO error",
					MessageBoxButton.OK,
					MessageBoxImage.Warning
				);

				this.StopSearches();

				return;
			}

			this.lastSearchedInSeconds = 1;

			result.Matches = sauceNao.Matches;
			result.UploadedImageUrl = sauceNao.UploadedImageUrl;
			result.UsedSearchEngine = Enum.SearchEngine.SauceNAO;

			this.CheckMatches(result);
			this.PortTagsOfMatchToResult(result);
		}

		private Match GetMatchWithBestSourceOrdering(Result result, Match currentMatch)
		{
			byte bestOrdering = 0;
			byte similarityThreshold = Options.Default.SimilarityThreshold;
			float lowestAcceptableSimilarity = currentMatch.Similarity - similarityThreshold;

			foreach (Match match in result.Matches) {
				// Similarity too low, we won't select this match even if it's source is higher in the ordering
				if (match.Similarity < lowestAcceptableSimilarity) {
					continue;
				}

				Source source = App.sources.GetByEnumValue(match.Source);

				if (source == null) {
					continue;
				}

				// Source is disabled
				if (!source.Enabled) {
					continue;
				}

				if (bestOrdering == 0 || source.Ordering < bestOrdering) {
					currentMatch = match;
					bestOrdering = source.Ordering;
				}
			}

			return currentMatch;
		}

		/// <summary>
		/// Check the various matches to find the best one.
		/// </summary>
		private void CheckMatches(Result result)
		{
			foreach (Match match in result.Matches) {
				// Check minimum similarity
				if (match.Similarity < Options.Default.Similarity) {
					continue;
				}

				// Check minimum number of tags (only for IQDB)
				if (result.UsedSearchEngine == Enum.SearchEngine.IQDB && Options.Default.TagsCount > 0 && (match.Tags == null || match.Tags.Count < Options.Default.TagsCount)) {
					continue;
				}

				// Check match type if enabled (only for IQDB)
				if (result.UsedSearchEngine == Enum.SearchEngine.IQDB && Options.Default.CheckMatchType && match.MatchType > Options.Default.MatchType) {
					continue;
				}

				// Check if source is enabled
				if (!App.sources.IsEnabled(match.Source)) {
					continue;
				}

				// Match found
				result.Match = this.GetMatchWithBestSourceOrdering(result, match);

				// Log the URL
				if (Options.Default.LogMatchedUrls) {
					this.LogUrl(result.Url);
				}

				// We want to retrieve tags from a booru
				if (Options.Default.ParseTags) {
					bool success = this.ParseBooruPage(result);

					// Unable to retrieve tags for this match, we'll try another one
					if (!success) {
						continue;
					}
				}

				// We have our match, no need to check the others
				return;
			}
		}

		/// <summary>
		/// Port tags from the result's selected match to the result's tags list.
		/// </summary>
		/// <param name="result"></param>
		private void PortTagsOfMatchToResult(Result result)
		{
			this.AddSearchEngineTags(result);
			this.AddHatateTags(result);
		}

		/// <summary>
		/// Add tags from the currently selected Match to the currently selected Result.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="nameSpace"></param>
		private void AddSearchEngineTags(Result result)
		{
			// Remove the previous search engine-added tags
			result.ClearTagsOfSource(Enum.TagSource.SearchEngine);

			if (result.Match == null) {
				return;
			}

			foreach (Tag tag in result.Match.Tags) {
				result.AddTag(tag);
			}
		}

		private void AddHatateTags(Result result)
		{
			// Remove the previous Hatate-added tags
			result.ClearTagsOfSource(Enum.TagSource.Hatate);

			// Found on IQDB
			if (result.Found) {
				if (Options.Default.AddFoundTag) {
					result.AddTag(new Tag(Options.Default.FoundTag, true) { Source = Enum.TagSource.Hatate });
				}
			} else { // Not found on IQDB
				if (Options.Default.AddNotfoundTag) {
					result.AddTag(new Tag(Options.Default.NotfoundTag, true) { Source = Enum.TagSource.Hatate });
				}
			}

			// Add tagged tag if at least one booru tags exists
			if (!Options.Default.AddTaggedTag) {
				return;
			}

			foreach (Tag tag in result.Tags) {
				// Don't add the "hatate:tagged" tag is we only have user-added or hatate-added tags
				if (tag.Source == Enum.TagSource.User || tag.Source == Enum.TagSource.Hatate) {
					continue;
				}

				result.AddTag(new Tag(Options.Default.TaggedTag, true) { Source = Enum.TagSource.Hatate });

				return;
			}
		}

		/// <summary>
		/// Parse the matched booru page to retrieve tags and other informations about the image.
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		private bool ParseBooruPage(Result result)
		{
			Parser.IParser booru = null;

			switch (result.Source) {
				case Enum.Source.Danbooru: booru = new Parser.Danbooru(); break;
				case Enum.Source.Gelbooru: booru = new Parser.Gelbooru(); break;
				case Enum.Source.Konachan: booru = new Parser.Konachan(); break;
				case Enum.Source.Yandere: booru = new Parser.Yandere(); break;
				case Enum.Source.SankakuChannel: booru = new Parser.SankakuChannel(); break;
				case Enum.Source.Eshuushuu: booru = new Parser.Eshuushuu(); break;
				case Enum.Source.TheAnimeGallery: booru = new Parser.TheAnimeGallery(); break;
				case Enum.Source.Zerochan: booru = new Parser.Zerochan(); break;
				case Enum.Source.AnimePictures: booru = new Parser.AnimePictures(); break;
				case Enum.Source.Pixiv: booru = new Parser.Pixiv(); break;
				case Enum.Source.NicoNicoSeiga: booru = new Parser.NicoNicoSeiga(); break;
				case Enum.Source.DeviantArt: booru = new Parser.DeviantArt(); break;
				default: return false;
			}

			// Add tags from the parsed booru page
			result.ClearTagsOfSource(Enum.TagSource.Booru);

			// Parse booru page
			bool success = booru.FromUrl(result.Url);

			// For some booru we might know if the image is available on the page even if the parsing fails
			result.Unavailable = booru.Unavailable;

			// Don't go further if we can't retrieve tags from the URL
			if (!success) {
				this.ListBox_Tags.Items.Refresh();

				return false;
			}

			// Add tags if enabled in the options
			if (App.sources.ShouldGetTags(result.Source)) {
				this.AddTagsToResult(booru.Tags, result);
			}

			result.Full = booru.Full;
			result.Pages = booru.Pages;
			result.Remote = new Image();
			result.Remote.Size = booru.Size;
			result.Remote.Width = booru.Width;
			result.Remote.Height = booru.Height;

			if (booru.Full != null) {
				result.Remote.Format = booru.Full.Substring(booru.Full.LastIndexOf('.') + 1);
			}

			switch (booru.Rating) {
				case null: break;
				case "Safe": result.Rating = IqdbApi.Enums.Rating.Safe; break;
				case "Questionable": result.Rating = IqdbApi.Enums.Rating.Questionable; break;
				case "Explicit": result.Rating = IqdbApi.Enums.Rating.Explicit; break;
			}

			// Check ignored tags
			for (int i = result.Tags.Count - 1; i >= 0; i--) {
				Tag tag = result.Tags[i];
				bool isIgnored = this.IsTagInIgnoreds(tag);

				if (isIgnored && !result.Ignoreds.Contains(tag)) {
					result.Ignoreds.Add(tag);
					result.Tags.Remove(tag);
				}
			}

			// Add rating if not already in tags
			if (Options.Default.AddRating
			&& result.Rating != IqdbApi.Enums.Rating.Unrated
			&& !result.Tags.Exists(t => t.Namespace == "rating")
			) {
				result.AddTag(new Tag(result.Rating.ToString().ToLower(), "rating") { Source = Enum.TagSource.Booru });
			}

			return true;
		}

		/// <summary>
		/// Reset the state of the window, ready for starting another search.
		/// </summary>
		private void EndSearch()
		{
			this.SetStatus("Finished.");
			this.SetStartButton("Start", "#FF3CB21A");
			this.ChangeStartButtonEnabledValue();
		}

		/// <summary>
		/// Update the labels with some useful informations.
		/// </summary>
		private void UpdateLabels()
		{
			int remaining = this.ListBox_Files.Items.Count;
			int remainSeconds = (Options.Default.Delay + lastSearchedInSeconds) * remaining;

			this.Label_Remaining.Content = "Remaining: " + remaining + " files (~ " + remainSeconds + " seconds / " + (remainSeconds / 60) + " minutes)";
			this.Label_Results.Content = "Results: " + this.found + " found, " + this.notFound + " not";
		}

		/// <summary>
		/// Take the tag list and write it into a text file with the same name as the image.
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="tags"></param>
		private void WriteTagsToTxt(string filepath, List<string> tags, bool append=false)
		{
			using (StreamWriter file = new StreamWriter(filepath, append)) {
				foreach (string tag in tags) {
					file.WriteLine(tag);
				}
			}
		}

		/// <summary>
		/// Take the tag list and write it into a text file with the same name as the image.
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="tags"></param>
		private void WriteTagsToTxt(string filepath, List<Tag> tags, bool append=false)
		{
			using (StreamWriter file = new StreamWriter(filepath, append)) {
				foreach (Tag tag in tags) {
					file.WriteLine(tag.Namespaced);
				}
			}
		}

		/// <summary>
		/// Create the context menu for the Files ListBox.
		/// </summary>
		private void CreateFilesListContextMenu()
		{
			ContextMenu context = new ContextMenu();

			// Empty context menu as we'll populate it when opened

			this.ListBox_Files.ContextMenu = context;
		}

		/// <summary>
		/// Create the context menu for the Tags ListBox.
		/// </summary>
		private void CreateTagsListContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item.Header = "Edit";
			item.Tag = "editTags";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Remove";
			item.Tag = "removeTags";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Ignore";
			item.Tag = "ignore";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Copy to clipboard";
			item.Tag = "copyTags";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Search on Danbooru";
			item.Tag = "helpTag";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			context.Items.Add(new Separator());

			item = new MenuItem();
			item.Header = "Add tags";
			item.Tag = "addTagsForSelectedResult";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			this.ListBox_Tags.ContextMenu = context;
		}

		/// <summary>
		/// Create the context menu for the UnknownTags ListBox.
		/// </summary>
		private void CreateIgnoredsListContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item.Header = "Unignore";
			item.Tag = "unignore";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			context.Items.Add(new Separator());

			item = new MenuItem();
			item.Header = "Copy to clipboard";
			item.Tag = "copyUnknownTag";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Search on Danbooru";
			item.Tag = "helpUnknownTag";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			this.ListBox_Ignoreds.ContextMenu = context;
		}

		/// <summary>
		/// Move a given item's file to the tagged folder with the tags in a .txt file and remove the row.
		/// </summary>
		/// <param name="item"></param>
		private bool WriteTagsToFilesForResult(Result result)
		{
			// Not tags
			if (result == null || !result.HasTags) {
				return false;
			}

			if (File.Exists(result.ImagePath)) {
				// Warn a user trying to write a txt file into the Hydrus' "client_files" folder
				if (this.IsHydrusOwnedFolder(result.ImagePath) && !App.AskUser("This image is located inside the Hydrus' \"client_files\" folder. Writing a txt files there might not be a good idea, do you really want to do that?")) {
					return false;
				}

				this.WriteTagsToTxt(result.ImagePath + ".txt", result.NonHiddenTags.ToList());
			}

			// Write the ignored tags to txt
			if (result.Ignoreds.Count > 0) {
				this.WriteIgnoredsTags(result.Ignoreds);
			}

			return true;
		}

		/// <summary>
		/// Send tags to Hydrus for a given file and remove the row.
		/// </summary>
		/// <param name="item"></param>
		private async Task<bool> SendTagsToHydrusForResult(Result result, string hydrusPageKey=null)
		{
			if (result == null) {
				return false;
			}

			bool imported = false;

			// Not a Hydrus file, we need to import it first
			if (String.IsNullOrEmpty(result.Local.Hash) && File.Exists(result.ImagePath)) {
				result.Local.Hash = await App.hydrusApi.ImportFile(result);

				// Still no hash, abort
				if (String.IsNullOrEmpty(result.Local.Hash)) {
					return false;
				}

				// Import successful
				imported = true;
			}

			// Send tags to Hydrus
			if (result.HasTags) {
				bool success = await App.hydrusApi.SendTagsForFile(result);

				if (!success) {
					result.AddWarning("Hydrus: failed to send tags");

					return false;
				}
			}

			// Link matched URL
			if (Options.Default.AssociateUrl) {
				string url = result.Url;
				bool success = true;

				if (url != null) {
					success = await App.hydrusApi.AssociateUrl(result.Local.Hash, result.Url);
				}

				if (!success) {
					result.AddWarning("Hydrus: failed to associate URL");

					return false;
				}
			}

			// Display in a Hydrus page
			if (Options.Default.AddImagesToHydrusPage && hydrusPageKey != null && !String.IsNullOrEmpty(result.Local.Hash)) {
				await App.hydrusApi.AddFileToPage(hydrusPageKey, result.Local.Hash);
			}

			// Write the ignored tags to txt
			if (result.Ignoreds.Count > 0) {
				this.WriteIgnoredsTags(result.Ignoreds);
			}

			// The file was imported into Hydrus, we can now delete it
			if (imported && Options.Default.DeleteImported) {
				this.SendFileToRecycleBin(result);
			}

			return true;
		}

		/// <summary>
		/// Add the ignoreds tags from a result to the txt list.
		/// </summary>
		/// <param name="ignoredTags"></param>
		private void WriteIgnoredsTags(List<Tag> ignoredTags)
		{
			StreamWriter file = new StreamWriter(this.IgnoredsTxtPath, true);

			if (this.ignoreds == null) {
				this.ignoreds = new List<string>();
			}

			foreach (Tag tag in ignoredTags) {
				string namespaced = tag.Namespaced;

				// Tag isn't already known as ignored, add it
				if (!this.ignoreds.Contains(namespaced)) {
					this.ignoreds.Add(namespaced);
					file.WriteLine(namespaced);
				}
			}

			file.Close();
		}

		/// <summary>
		/// Remove a result from the Files listbox.
		/// </summary>
		/// <param name="result"></param>
		private void RemoveResultFromFilesListbox(Result result)
		{
			// Remove the row
			this.ListBox_Files.Items.Remove(result);

			// Empty tags listboxes
			result.Tags.Clear();
			result.Ignoreds.Clear();

			// Refresh view
			this.ListBox_Tags.Items.Refresh();
			this.ListBox_Ignoreds.Items.Refresh();

			// Delete thumbnail
			if (result.ThumbPath != null && result.ThumbPath != result.ImagePath) {
				try {
					using (new FileStream(result.ThumbPath, FileMode.Truncate, FileAccess.ReadWrite, FileShare.Delete, 1, FileOptions.DeleteOnClose | FileOptions.Asynchronous));
				} catch (IOException) { }
			}
		}

		/// <summary>
		/// Check if a row in the Files ListBox has an associated result.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private bool HasFoundResult(int index)
		{
			Result result = this.GetResultAt(index);

			return result != null && result.Found;
		}

		/// <summary>
		/// Get a Result from the files listbox at a given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private Result GetResultAt(int index)
		{
			if (index < 0 || index >= this.ListBox_Files.Items.Count) {
				return null;
			}

			return (Result)this.ListBox_Files.Items.GetItemAt(index);
		}

		/// <summary>
		/// Get one of the selected Results from the files listbox.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private Result GetSelectedResultAt(int index)
		{
			return (Result)this.ListBox_Files.SelectedItems[index];
		}

		/// <summary>
		/// Remove all the selected files from the Files listbox.
		/// </summary>
		private void RemoveSelectedFiles()
		{
			while (this.ListBox_Files.SelectedItems.Count > 0) {
				this.RemoveResultFromFilesListbox(this.GetSelectedResultAt(0));
			}
		}

		/// <summary>
		/// Send all selected files in the list to the recycle bin.
		/// </summary>
		private async Task DeleteSelectedFiles()
		{
			MessageBoxResult choice = MessageBox.Show(
				"This will send all selected files to the recycle bin.",
				"Are you sure?",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning
			);

			if (choice != MessageBoxResult.Yes) {
				return;
			}

			this.ListBox_Files.IsEnabled = false;

			while (this.ListBox_Files.SelectedItems.Count > 0) {
				Result result = this.GetSelectedResultAt(0);

				await Task.Run(() => this.SendFileToRecycleBin(result));

				this.RemoveResultFromFilesListbox(result);
			}

			this.ListBox_Files.IsEnabled = true;
		}

		/// <summary>
		/// Copy file path for all the selected files.
		/// </summary>
		private void CopySelectedFilePaths()
		{
			string text = "";

			for (int i = 0; i < this.ListBox_Files.SelectedItems.Count; i++) {
				Result result = this.ListBox_Files.SelectedItems[i] as Result;

				text += result.ImagePath;

				if (i < this.ListBox_Files.SelectedItems.Count - 1) {
					text += "\n";
				}
			}

			Clipboard.SetText(text);
		}

		/// <summary>
		/// Copy hashes for all the selected files.
		/// </summary>
		private void CopySelectedHashes()
		{
			string text = "";

			for (int i = 0; i < this.ListBox_Files.SelectedItems.Count; i++) {
				Result result = this.ListBox_Files.SelectedItems[i] as Result;

				// No hash, calculate it
				if (String.IsNullOrEmpty(result.Local.Hash)) {
					result.CalculateLocalHash();

					// Hash is still null or empty
					if (String.IsNullOrEmpty(result.Local.Hash)) {
						continue;
					}
				}

				text += "md5:" + result.Local.Hash;

				if (i < this.ListBox_Files.SelectedItems.Count - 1) {
					text += "\n";
				}
			}

			Clipboard.SetText(text);
		}

		/// <summary>
		/// Copy matched URLs for all the selected files.
		/// </summary>
		private void CopySelectedUrls()
		{
			string text = "";

			for (int i = 0; i < this.ListBox_Files.SelectedItems.Count; i++) {
				string url = (this.ListBox_Files.SelectedItems[i] as Result).Url;

				if (String.IsNullOrEmpty(url)) {
					continue;
				}

				text += url;

				if (i < this.ListBox_Files.SelectedItems.Count - 1) {
					text += "\n";
				}
			}

			Clipboard.SetText(text);
		}

		/// <summary>
		/// Copy matched source URLs for all the selected files.
		/// </summary>
		private void CopySelectedSourceUrls()
		{
			string text = "";

			for (int i = 0; i < this.ListBox_Files.SelectedItems.Count; i++) {
				Match match = (this.ListBox_Files.SelectedItems[i] as Result).Match;

				if (match == null) {
					continue;
				}

				string sourceUrl = match.SourceUrl;

				if (String.IsNullOrEmpty(sourceUrl)) {
					continue;
				}

				text += sourceUrl;

				if (i < this.ListBox_Files.SelectedItems.Count - 1) {
					text += "\n";
				}
			}

			Clipboard.SetText(text);
		}

		/// <summary>
		/// Remove all the selected tags from the Tags listbox.
		/// </summary>
		private void RemoveSelectedTags()
		{
			Result result = this.SelectedResult;

			while (this.ListBox_Tags.SelectedItems.Count > 0) {
				Tag tag = (Tag)this.ListBox_Tags.SelectedItems[0];

				result.Tags.Remove(tag);
				this.ListBox_Tags.Items.Refresh();
			}
		}

		private void EditSelectedTags()
		{
			Result result = this.SelectedResult;
			List<Tag> selectedTags = new List<Tag>();

			foreach (Tag selectedTag in this.ListBox_Tags.SelectedItems) {
				selectedTags.Add(selectedTag);
				result.Tags.Remove(selectedTag);
			}

			this.ListBox_Tags.SelectedItems.Clear();

			EditTag window = new EditTag(selectedTags);
			Tag editedTag = window.Tag;

			foreach (Tag selectedTag in selectedTags) {
				if (editedTag.Namespace != EditTag.VARIOUS) {
					selectedTag.Namespace = editedTag.Namespace;
				}

				if (editedTag.Value != EditTag.VARIOUS) {
					selectedTag.Value = editedTag.Value;
				}

				result.AddTag(selectedTag);
			}

			this.RefreshTagsListBox(result);
		}

		/// <summary>
		/// Remove result and background color of all the selected files.
		/// </summary>
		private void ResetSelectedFilesResult()
		{
			foreach (Result result in this.ListBox_Files.SelectedItems) {
				result.Reset();
			}

			this.ListBox_Tags.Items.Refresh();
			this.ListBox_Ignoreds.Items.Refresh();
		}

		/// <summary>
		/// Rewrite tags in a txt file without duplicates.
		/// </summary>
		/// <param name="txt"></param>
		/// <param name="tags"></param>
		private int CleanIgnoredsTxt()
		{
			string txtPath = this.IgnoredsTxtPath;

			if (!File.Exists(txtPath)) {
				return 0;
			}

			List<string> copies = new List<string>();
			int unecessary = 0;

			foreach (string tag in this.ignoreds) {
				if (!copies.Contains(tag)) {
					copies.Add(tag);
				} else {
					unecessary++;
				}
			}

			this.WriteTagsToTxt(txtPath, copies, false);

			return unecessary;
		}

		/// <summary>
		/// Set the text in the status bar at the window's bottom.
		/// </summary>
		/// <param name="text"></param>
		private void SetStatus(string text)
		{
			this.Label_Status.Content = text;
		}

		/// <summary>
		/// Check if a file is an image using the extension.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="filters"></param>
		/// <returns></returns>
		private bool IsCorrespondingToFilter(string path, string[] filters)
		{
			foreach (string filter in filters) {
				if (this.Extension(path).ToLower() == filter) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Get a file extension from its path.
		/// Note: Dot included (example: returns ".txt").
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string Extension(string path)
		{
			int dot = path.LastIndexOf(".");

			if (dot < 0) {
				return String.Empty;
			}

			return path.Substring(dot, path.Length - path.LastIndexOf("."));
		}

		/// <summary>
		/// Add a file to the list if it's not already in it or not in the tagged or notfound folder.
		/// </summary>
		private void AddFileToList(string filepath, List<Tag> tags=null, HydrusMetadata hydrusMetadata=null, bool focus=false)
		{
			// Windows does not support longer file paths, causing a PathTooLongException
			if (filepath.Length > MAX_PATH_LENGTH) {
				return;
			}

			Result result = new Result(filepath);

			if (//this.ListBox_Files.Items.Contains(result)
			this.ListBox_Files.Items.Cast<Result>().Any(r => r.ImagePath == filepath)) {
				return;
			}

			// Add tags to the result
			if (tags != null && tags.Count > 0) {
				foreach (Tag tag in tags) {
					result.AddTag(tag);
				}
			}

			// Set Hydrus metadata
			if (hydrusMetadata != null) {
				result.SetHydrusMetadata(hydrusMetadata);
			}

			this.ListBox_Files.Items.Add(result);

			// Focus the added file
			if (focus && this.ListBox_Files.Items.Count > 0) {
				this.ListBox_Files.SelectedItem = this.ListBox_Files.Items[this.ListBox_Files.Items.Count - 1];
			}
		}

		/// <summary>
		/// Add all the selected items in a given list to he ignoreds list and remove them from the listbox.
		/// </summary>
		private void IgnoreSelectItems()
		{
			Result result = this.SelectedResult;

			while (this.ListBox_Tags.SelectedItems.Count > 0) {
				Tag tag = (Tag)this.ListBox_Tags.SelectedItems[0];

				result.Tags.Remove(tag);
				result.Ignoreds.Add(tag);

				this.ListBox_Tags.Items.Refresh();
				this.ListBox_Ignoreds.Items.Refresh();
			}
		}

		/// <summary>
		/// Remove the selected ignoreds tags from the txt and from the list and move it to the tags list.
		/// </summary>
		/// <param name="from"></param>
		private void UningnoreSelectItems()
		{
			Result result = this.SelectedResult;
			bool hasIgnoredTags = this.HasIgnoredTags;

			while (this.ListBox_Ignoreds.SelectedItems.Count > 0) {
				Tag tag = (Tag)this.ListBox_Ignoreds.SelectedItems[0];

				// We don't have any ignored tags
				if (hasIgnoredTags) {
					this.ignoreds.Remove(tag.Namespaced);
				}

				result.Ignoreds.Remove(tag);
				result.AddTag(tag);

				this.ListBox_Tags.Items.Refresh();
				this.ListBox_Ignoreds.Items.Refresh();
			}

			string txtPath = this.IgnoredsTxtPath;

			// Rewrite the ignoreds tags list since we removed some items from it
			if (hasIgnoredTags && File.Exists(txtPath)) {
				this.WriteTagsToTxt(txtPath, this.ignoreds, false);
			}
		}

		/// <summary>
		/// Get a brush from an hexadeciaml string value.
		/// </summary>
		/// <returns></returns>
		private Brush GetBrushFromString(string value)
		{
			return (Brush)new System.Windows.Media.BrushConverter().ConvertFromString(value);
		}

		/// <summary>
		/// Set the content and background color of the start button.
		/// </summary>
		private void SetStartButton(string content, string color)
		{
			this.Button_Start.Content = content;
			this.Button_Start.Background = this.GetBrushFromString(color);
		}

		/// <summary>
		/// Open a folder or launch a file.
		/// </summary>
		/// <param name="path"></param>
		private void StartProcess(string path)
		{
			Process.Start(new ProcessStartInfo(path));
		}

		/// <summary>
		/// Search the selected tag on danbooru.
		/// </summary>
		private void OpenHelpForSelectedTag(ListBox from)
		{
			var selectedItem = from.SelectedItem;

			if (selectedItem != null) {
				Tag item = from.SelectedItem as Tag;

				this.StartProcess("https://danbooru.donmai.us/wiki_pages/show_or_new?title=" + item.Underscored);
			}
		}

		/// <summary>
		/// Set the IsEnabled accessor of a ContextMenuItem  for the Files listbox.
		/// </summary>
		private void SetContextMenuItemEnabled(ListBox listBox, int index, bool enabled)
		{
			if (listBox.ContextMenu.Items.Count < 1) {
				return;
			}

			try {
				((MenuItem)listBox.ContextMenu.Items[index]).IsEnabled = enabled;
			} catch (Exception) {
				return;
			}
		}

		/// <summary>
		/// Enable or disable the Start button depending if the list is empty or if the search loop is already running.
		/// </summary>
		private void ChangeStartButtonEnabledValue()
		{
			this.Button_Start.IsEnabled = this.ListBox_Files.Items.Count > 0;
		}

		/// <summary>
		/// Same as IsTagInList() but useq a Tag object.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="list"></param>
		/// <returns></returns>
		private bool IsTagInIgnoreds(Tag tag)
		{
			if (!this.HasIgnoredTags) {
				return false;
			}

			return this.ignoreds.Contains(tag.Namespaced);
		}

		/// <summary>
		/// Open a window to input a new tag and save it for the selected file and into the known tags.
		/// </summary>
		private void AddTagsForSelectedResult()
		{
			List<Tag> tags = this.AskForNewTags(true);

			if (tags.Count == 0) {
				return;
			}

			Result result = this.SelectedResult;

			this.AddTagsToResult(tags, result);
			this.RefreshTagsListBox(result);
		}

		/// <summary>
		/// Add tags for all the selected results.
		/// </summary>
		private void AddTagsForSelectedResults()
		{
			List<Tag> tags = this.AskForNewTags(true);

			if (tags.Count == 0) {
				return;
			}

			foreach (Result result in this.ListBox_Files.SelectedItems) {
				this.AddTagsToResult(tags, result);
			}

			this.RefreshTagsListBox(this.SelectedResult);
		}

		/// <summary>
		/// Add new tags into a result's tags list while preventing duplicates.
		/// </summary>
		private void AddTagsToResult(List<Tag> tags, Result result)
		{
			foreach (Tag tag in tags) {
				result.AddTag(tag);
			}
		}

		/// <summary>
		/// Open a window asking for new tags, write them to the txt files then return them.
		/// </summary>
		/// <param name="bypassOption">If true, open the ask tag window even if the AskTags option is disabled</param>
		private List<Tag> AskForNewTags(bool bypassOption=false)
		{
			List<Tag> tags = new List<Tag>();

			if (bypassOption || Options.Default.AskTags) {
				Manage window = new Manage();
				tags = window.Tags;
			}

			return tags;
		}

		/// <summary>
		/// Set a tag list as items source for a listbox.
		/// </summary>
		/// <param name="listBox"></param>
		/// <param name="tags"></param>
		private void SetListBoxItemsSource(ListBox listBox, List<Tag> tags)
		{
			if (listBox.ItemsSource == null) {
				listBox.Items.Clear();
			}

			listBox.ItemsSource = tags;
			listBox.Items.Refresh();
		}

		/// <summary>
		/// Takes a bytes value and transform it into a more human readable value like "616 KB" or "7.25 MB".
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		private string HumanReadableFileSize(long bytes)
		{
			if (bytes < 1000000) {
				return (bytes / 1000) + " KB";
			}

			return ((float)bytes / 1000000).ToString("0.00") + " MB";
		}

		/// <summary>
		/// Checks if the given path looks like a Hydrus owned folder, like the "client_files" one.
		/// </summary>
		/// <returns></returns>
		private bool IsHydrusOwnedFolder(string path)
		{
			return path.Contains(@"\client_files\");
		}

		private void WriteTagsForSelectedFiles()
		{
			int successes = 0;
			int failures = 0;
			string counts = null;

			this.ListBox_Files.IsEnabled = false;

			while (this.ListBox_Files.SelectedItems.Count > 0) {
				Result result = this.GetSelectedResultAt(0);
				bool success = this.WriteTagsToFilesForResult(result);
				counts = this.HandleProcessedResult(result, success, ref successes, ref failures);

				this.SetStatus("Writing tags... " + counts);
			}

			this.ListBox_Files.Items.Refresh();
			this.ListBox_Files.IsEnabled = true;
			this.SetStatus("Tags wrote for all the selected files. " + counts);
		}

		private async void SendTagsForSelectedFiles()
		{
			int successes = 0;
			int failures = 0;
			string counts = null;
			string hydrusPageKey = null;

			App.hydrusApi.ResetUnreachableFlag();
			this.ListBox_Files.IsEnabled = false;

			if (Options.Default.AddImagesToHydrusPage) {
				hydrusPageKey = await App.hydrusApi.GetPageNamed(Options.Default.HydrusPageName, true);
			}

			// Process each selected file until no one remain or the API becomes unreachable
			while (this.ListBox_Files.SelectedItems.Count > 0 && !App.hydrusApi.Unreachable) {
				Result result = this.GetSelectedResultAt(0);
				bool success = await this.SendTagsToHydrusForResult(result, hydrusPageKey);
				counts = this.HandleProcessedResult(result, success, ref successes, ref failures);

				this.SetStatus("Sending tags to Hydrus... " + counts);
			}

			this.ListBox_Files.Items.Refresh();
			this.ListBox_Files.IsEnabled = true;
			this.SetStatus("Tags sent to Hydrus for all the selected files. " + counts);
		}

		private async void SendUrlForSelectedFiles(bool sendPageUrlInsteadOfImageUrl)
		{
			int successes = 0;
			int failures = 0;
			string counts = null;

			App.hydrusApi.ResetUnreachableFlag();
			this.ListBox_Files.IsEnabled = false;

			// Process each selected file until no one remain or the API becomes unreachable
			while (this.ListBox_Files.SelectedItems.Count > 0 && !App.hydrusApi.Unreachable) {
				Result result = this.GetSelectedResultAt(0);

				// Not a searched result, skip
				if (result == null || !result.Searched || !result.HasMatch) {
					counts = this.HandleProcessedResult(result, false, ref successes, ref failures);

					continue;
				}

				// Image is deleted on the source page
				if (result.Unavailable) {
					result.AddWarning("Hydrus: URL not sent - image no longer available on the source page, downloader would ignore it");
					counts = this.HandleProcessedResult(result, false, ref successes, ref failures);

					continue;
				}

				bool success = await App.hydrusApi.SendUrl(result, sendPageUrlInsteadOfImageUrl ? result.Url : result.Full);
				counts = this.HandleProcessedResult(result, success, ref successes, ref failures);

				// Move file to recycle bin
				if (success && Options.Default.DeleteImported) {
					this.SendFileToRecycleBin(result);
				}

				this.SetStatus("Sending URLs to Hydrus... " + counts);
			}

			this.ListBox_Files.Items.Refresh();
			this.ListBox_Files.IsEnabled = true;
			this.SetStatus("URLs sent to Hydrus for all the selected files. " + counts);
		}

		/// <summary>
		/// Send a single URL to Hydrus for a result.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="url"></param>
		private async void SendUrlToHydrus(Result result, string url)
		{
			App.hydrusApi.ResetUnreachableFlag();
			this.ListBox_Files.IsEnabled = false;

			if (result == null) {
				return;
			}

			int successes = 0;
			int failures = 0;

			bool success = await App.hydrusApi.SendUrl(result, url);
			this.HandleProcessedResult(result, success, ref successes, ref failures);

			// Move file to recycle bin
			if (success && Options.Default.DeleteImported) {
				this.SendFileToRecycleBin(result);
			}

			this.ListBox_Files.Items.Refresh();
			this.ListBox_Files.IsEnabled = true;
			this.SetStatus("URL sent to Hydrus.");
		}

		/// <summary>
		/// Keep a URL in a text file.
		/// </summary>
		/// <param name="url"></param>
		private void LogUrl(string url)
		{
			string filePath = App.appDir + @"\" + TXT_MATCHED_URLS;

			using (StreamWriter file = new StreamWriter(filePath, true)) {
				file.WriteLine(url);
			}
		}

		/// <summary>
		/// What to do with a Result after being processed.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="success"></param>
		/// <param name="successes"></param>
		/// <param name="failures"></param>
		/// <returns></returns>
		private string HandleProcessedResult(Result result, bool success, ref int successes, ref int failures)
		{
			bool unselected = false;

			if (success) {
				if (Options.Default.RemoveResultAfter) {
					this.RemoveResultFromFilesListbox(result);
					unselected = true;
				}

				successes++;
			} else {
				failures++;
			}

			if (!unselected) {
				this.ListBox_Files.SelectedItems.Remove(result);
			}

			return "(" + successes + " successful, " + failures + " failed)";
		}

		/// <summary>
		/// Delete a file by sending it to the recycle bin.
		/// </summary>
		/// <param name="result"></param>
		private void SendFileToRecycleBin(Result result)
		{
			if (!File.Exists(result.ImagePath)) {
				return;
			}

			// Don't delete file if it's one of Hydrus client files
			if (this.IsHydrusOwnedFolder(result.ImagePath)) {
				return;
			}

			try {
				FileIO.FileSystem.DeleteFile(result.ImagePath, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin);
			} catch (Exception) { }
		}

		/// <summary>
		/// Update the view elements on the right (tag lists, images, labels...)
		/// </summary>
		private void UpdateRightView(Result result)
		{
			// Add tags to the list
			result.Tags.Sort();
			result.Ignoreds.Sort();

			this.SetListBoxItemsSource(this.ListBox_Tags, result.Tags);
			this.SetListBoxItemsSource(this.ListBox_Ignoreds, result.Ignoreds);

			this.GroupBox_Tags.Header = "Tags (" + result.CountNonHiddenTags + ")";
			this.GroupBox_Ignoreds.Header = "Ignoreds (" + result.Ignoreds.Count + ")";
			this.Label_MatchTips.Text = "";
			this.Label_MatchPages.Content = "";
			this.Label_MatchPages.ToolTip = "";
			this.Label_MatchPages.Visibility = Visibility.Hidden;
			this.ComboBox_TagSources.IsEnabled = true;

			this.RefreshTagsListBox(result);

			// Set selected tag sources
			this.Checkbox_TagSource_User.IsChecked = result.SelectedTagSources.Contains(Enum.TagSource.User);
			this.Checkbox_TagSource_Booru.IsChecked = result.SelectedTagSources.Contains(Enum.TagSource.Booru);
			this.Checkbox_TagSource_SearchEngine.IsChecked = result.SelectedTagSources.Contains(Enum.TagSource.SearchEngine);
			this.Checkbox_TagSource_Hatate.IsChecked = result.SelectedTagSources.Contains(Enum.TagSource.Hatate);

			// No matches, nothing to update
			if (!result.HasMatches) {
				this.Label_MatchTips.Text = "No matches";

				return;
			}

			// Set matches in selector
			this.ComboBox_Matches.ItemsSource = result.Matches;
			this.ComboBox_Matches.IsEnabled = true;

			// No match found, nothing to update
			if (!result.HasMatch) {
				this.Label_MatchTips.Text = result.Matches.Count + " possible matches available in the dropdown below";

				return;
			}

			// Set match labels
			this.Label_Match.Content = result.Source.ToString();
			this.Label_MatchInfos.Content = "";

			if (result.Remote.Format != null) {
				this.Label_Match.Content += " " + result.Remote.Format.ToUpper();
			}

			if (result.Remote.Size > 0) {
				this.Label_MatchInfos.Content += this.HumanReadableFileSize(result.Remote.Size) + " ";
			}

			if (result.Remote.Width > 0 && result.Remote.Height > 0) {
				this.Label_MatchInfos.Content += "(" + result.Remote.Width + "x" + result.Remote.Height + ")";
			}

			// Set match image
			try {
				this.Image_Match.Source = (result.PreviewUrl != null ? new BitmapImage(new Uri(result.PreviewUrl)) : null);
			} catch (Exception) {
				// UriFormatException may happen if the uri is incorrect
			}

			// Set borders
			if (result.IsMatchBetterThanLocal) {
				this.Border_Local.BorderBrush = this.GetBrushFromString("#CC0");
				this.Border_Match.BorderBrush = this.GetBrushFromString("#0F0");
			} else {
				this.Border_Local.BorderBrush = this.GetBrushFromString("#0F0");
				this.Border_Match.BorderBrush = this.GetBrushFromString("#CC0");
			}

			// Set selected match
			this.ComboBox_Matches.SelectedItem = result.Match;

			// Set tag sources checkbox names
			this.Checkbox_TagSource_Booru.Content = result.Source.ToString();
			this.Checkbox_TagSource_SearchEngine.Content = result.UsedSearchEngine.ToString();

			if (result.Unavailable) {
				// Set DELETED label
				this.Label_MatchPages.Content = "DELETED";
				this.Label_MatchPages.ToolTip = "Work is no longer available on the page";
				this.Label_MatchPages.Visibility = Visibility.Visible;
				this.Label_MatchPages.Foreground = this.GetBrushFromString("#F00");
				this.Label_MatchPages.HorizontalAlignment = HorizontalAlignment.Stretch;
				this.Label_MatchPages.Width = Double.NaN;
			} else {
				// Set album pages counter
				this.Label_MatchPages.Content = result.Pages;
				this.Label_MatchPages.ToolTip = "Match is part of a " + result.Pages + " pages album";
				this.Label_MatchPages.Visibility = (result.Pages > 1 ? Visibility.Visible : Visibility.Hidden);
				this.Label_MatchPages.Foreground = this.GetBrushFromString("#FFD2D2D2");
				this.Label_MatchPages.HorizontalAlignment = HorizontalAlignment.Right;
				this.Label_MatchPages.Width = 29;
			}
		}

		private void AttachMatchUrlsSubmenuToMenuItem(MenuItem sub, string tag, string tooltip=null)
		{
			float similarity = 0;
			List<string> addedUrls = new List<string>();

			foreach (Match match in this.SelectedResult.Matches) {
				MenuItem sub2 = new MenuItem();

				if (match.Similarity != similarity) {
					sub2 = new MenuItem();
					sub2.Header = "- " + match.Similarity + "% similarity -";
					sub2.Foreground = this.GetBrushFromString("#FF808080");
					sub.Items.Add(sub2);
				}

				if (match.Url != null && !addedUrls.Contains(match.Url)) {
					sub2 = new MenuItem();
					sub2.Header = match.Url;
					sub2.Tag = tag;
					sub2.Click += this.ContextMenu_MenuItem_Click;
					sub.Items.Add(sub2);

					addedUrls.Add(match.Url);

					if (tooltip != null) {
						sub2.ToolTip = tooltip;
					}
				}

				if (match.SourceUrl != null && !addedUrls.Contains(match.SourceUrl)) {
					sub2 = new MenuItem();
					sub2.Header = match.SourceUrl;
					sub2.Tag = tag;
					sub2.Click += this.ContextMenu_MenuItem_Click;
					sub.Items.Add(sub2);

					addedUrls.Add(match.SourceUrl);

					if (tooltip != null) {
						sub2.ToolTip = tooltip;
					}
				}

				similarity = match.Similarity;
			}
		}

		private void StopSearches()
		{
			if (this.timer != null) {
				this.timer.Stop();
				this.timer.Tick -= new EventHandler(Timer_Tick);
			}

			this.timer = null;

			this.SetStatus("Stopped.");
			this.SetStartButton("Start", "#FF3CB21A");
		}

		/// <summary>
		/// Checks if the format of an image is supported by a search engine.
		/// </summary>
		/// <param name="image"></param>
		/// <param name="searchEngine"></param>
		/// <returns></returns>
		private bool ImageFormatIsSupported(Image image, Enum.SearchEngine searchEngine)
		{
			// webp not supported by IQDB
			if (searchEngine == Enum.SearchEngine.IQDB && image.Format == "webp") {
				return false;
			}

			// tiff not supported by SauceNao
			if (searchEngine == Enum.SearchEngine.SauceNAO && image.Format == "tiff") {
				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if there's a new release on GitHub.
		/// </summary>
		/// <param name="messageWhenUpToDate"></param>
		private async Task CheckForNewRelease(bool messageWhenUpToDate)
		{
			Supremes.Nodes.Document doc = null;

			try {
				doc = Supremes.Dcsoup.Parse(new Uri(App.RepositoryUrl + App.GITHUB_LATEST_RELEASE), 5000);
			} catch (Exception) {
				return;
			}

			if (doc == null) {
				this.GitHubReleaseParsingErrorMessage();

				return;
			}

			Supremes.Nodes.Elements links = doc.Select("a");

			if (links.Count < 1) {
				this.GitHubReleaseParsingErrorMessage();

				return;
			}

			foreach (Supremes.Nodes.Element link in links) {
				string href = link.Attr("href");
				string path = App.GITHUB_REPOSITORY + "/tree/r";

				if (!href.StartsWith(path)) {
					continue;
				}

				int lastSlashPos = href.LastIndexOf('/');

				// Last slash in the matched path should be the last in the whole URL
				if (lastSlashPos != path.Length - 2) {
					this.GitHubReleaseParsingErrorMessage();

					return;
				}

				href = href.Substring(lastSlashPos + 1);

				if (href.Length < 2 || href[0] != 'r') {
					this.GitHubReleaseParsingErrorMessage();

					return;
				}

				ushort release;

				// Release number is prefixed with 'r'
				if (!ushort.TryParse(href.Remove(0, 1), out release)) {
					this.GitHubReleaseParsingErrorMessage();

					return;
				}

				// Not a newer release
				if (release <= App.RELEASE_NUMBER) {
					if (messageWhenUpToDate) {
						MessageBox.Show("You have the latest release (r" + release + ").");
					}

					return;
				}

				Supremes.Nodes.Element changelog = doc.Select("div.release-main-section div.markdown-body").First;

				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					Release releaseWindow = new Release(release);

					if (changelog != null) {
						releaseWindow.Changelog = changelog.Html;
					}

					releaseWindow.ShowDialog();
				}));

				return;
			}
		}

		/// <summary>
		/// Displays a message warning about not being able to retrieve the latest release from GitHub.
		/// </summary>
		private void GitHubReleaseParsingErrorMessage()
		{
			System.Windows.Forms.MessageBox.Show("Unable to find the latest release.\nPlease check the Github repository to download it.\n\n" + App.RepositoryUrl);
		}

		/// <summary>
		/// Opens a window displaying the local and remote iamge for a result.
		/// </summary>
		/// <param name="result"></param>
		private void OpenCompareWindowForResult(Result result)
		{
			if (result == null || !result.HasMatch) {
				return;
			}

			// Missing images to be displayed in the window
			if (result.ImagePath == null || result.PreviewUrl == null) {
				return;
			}

			if (this.compareWindow == null) {
				this.compareWindow = new Compare();
				this.compareWindow.Closed += this.CompareWindow_Closed;
			}

			this.compareWindow.LoadResultImages(result);
		}

		private void ImportImageFromClipboard()
		{
			if (!Clipboard.ContainsImage()) {
				MessageBox.Show("The clipboard doesn't contains an image.");

				return;
			}

			IDataObject clipboardData = Clipboard.GetDataObject();

			if (clipboardData == null) {
				MessageBox.Show("The clipboard is empty.");

				return;
			}

			if (!clipboardData.GetDataPresent(System.Windows.Forms.DataFormats.Bitmap)) {
				return;
			}

			System.Windows.Interop.InteropBitmap interopBitmap = (System.Windows.Interop.InteropBitmap)clipboardData.GetData(System.Windows.Forms.DataFormats.Bitmap);
			string filePath = App.TempPngFilePath;

			// Save to file
			using (var fileStream = new FileStream(filePath, FileMode.Create)) {
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(interopBitmap));
				encoder.Save(fileStream);
			}

			this.AddFileToList(filePath, null, null, true);

			this.UpdateLabels();
			this.ChangeStartButtonEnabledValue();
		}

		/// <summary>
		/// Delete all files in a directory.
		/// </summary>
		/// <param name="folderPath"></param>
		private void DeleteFilesInFolder(string folderPath, bool checkUsage = false)
		{
			this.SetStatus("Deleting files in \"" + folderPath + "\"...");

			string[] files = Directory.GetFiles(folderPath);
			int deleted = 0;
			int locked = 0;

			foreach (string file in files) {
				// Checks if this file is in the list
				if (checkUsage && this.IsFileInList(file)) {
					MessageBoxResult result = MessageBox.Show(
						"This temporary image is currently in the list:\n\n" + file + "\n\nDelete it anyway? It will be removed from the list.",
						"Delete this file?",
						MessageBoxButton.YesNoCancel
					);

					// Skip this file or cancel deleting the other files
					if (result == MessageBoxResult.No) {
						continue;
					} else if (result == MessageBoxResult.Cancel) {
						break;
					}
				}

				try {
					File.Delete(file);

					deleted++;
				} catch (Exception) {
					locked++;
				}
			}

			this.SetStatus(deleted + " files deleted (" + locked + " in use).");
		}

		/// <summary>
		/// Check if a given file is in the list.
		/// </summary>
		/// <returns></returns>
		private bool IsFileInList(string filePath)
		{
			if (this.ListBox_Files.Items.Count == 0) {
				return false;
			}

			foreach (Result result in this.ListBox_Files.Items) {
				if (filePath == (string)result.ImagePath) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Search the selected result with the given search engine.
		/// </summary>
		private async Task SearchSelectedResultWithEngine(Enum.SearchEngine searchEngine)
		{
			this.SelectedResult.Reset();

			// We don't want to use any of the retry methods when manually searching with a chosen engine
			this.retrySearch = true;

			await this.SearchFile(this.ListBox_Files.SelectedIndex, searchEngine);

			this.ListBox_Files.Items.Refresh();
		}

		/// <summary>
		/// Add a new radio button to the View menu.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="tag"></param>
		private void AddParenthesisValueRadioToViewMenu(string label, ParenthesisValue tag)
		{
			RadioButton radioButton = new RadioButton() { Content = label, GroupName = "ParenthesisValue", Tag = tag };
			radioButton.Checked += this.MenuItem_View_ParenthesisValue_Checked;

			if (Options.Default.SearchedParenthesisValue == (byte)tag) {
				radioButton.IsChecked = true;
			}

			this.MenuItem_View.Items.Add(radioButton);
		}

		/// <summary>
		/// Refresh the content of the tags listbox.
		/// This will do the same thing as this.ListBox_Tags.Items.Refresh() but will also hide tags
		/// from non-selected sources as well as duplicates (as there can be two identical tags from different sources).
		/// </summary>
		/// <param name="result"></param>
		private void RefreshTagsListBox(Result result)
		{
			if (result == null || result.Tags.Count < 1) {
				this.ListBox_Tags.Items.Refresh();

				return;
			}

			ushort numberOfTags = 0;
			Tag previousTag = null;

			// Hide all tags
			foreach (Tag tag in result.Tags) {
				tag.Hidden = true;
			}

			IEnumerable<Tag> tagsFromSelectedSources =
				from tag in result.Tags
				where result.SelectedTagSources.Contains(tag.Source)
				select tag;

			// Unhide tags from selected sources
			foreach (Tag tag in tagsFromSelectedSources) {
				tag.Hidden = false;
				numberOfTags++;
			}

			// Refresh tags
			result.Tags.Sort();

			// Hide duplicates in the list
			foreach (Tag tag in result.Tags) {
				if (tag.Hidden) {
					continue;
				}

				if (previousTag == null) {
					previousTag = tag;
					continue;
				}

				if (tag.Namespaced.Equals(previousTag.Namespaced)) {
					tag.Hidden = true;
					numberOfTags--;
				}

				previousTag = tag;
			}

			// Update groupbox title
			this.GroupBox_Tags.Header = "Tags (" + numberOfTags + ")";

			// Update listbox content.
			this.ListBox_Tags.Items.Refresh();
		}

		private bool SetAsDefaultButtonEnabledStatus()
		{
			return (bool)this.Checkbox_TagSource_User.IsChecked != Options.Default.TagSource_User
				|| (bool)this.Checkbox_TagSource_Booru.IsChecked != Options.Default.TagSource_Booru
				|| (bool)this.Checkbox_TagSource_SearchEngine.IsChecked != Options.Default.TagSource_SearchEngine
				|| (bool)this.Checkbox_TagSource_Hatate.IsChecked != Options.Default.TagSource_Hatate;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// Check if the search process is currently running.
		/// </summary>
		private bool IsRunning
		{
			get { return this.timer != null && this.timer.Enabled; }
		}

		/// <summary>
		/// Get the selected result from the files listbox.
		/// </summary>
		/// <returns></returns>
		private Result SelectedResult
		{
			get { return (Result)this.ListBox_Files.SelectedItem; }
		}

		/// <summary>
		/// Check if we have ignored tags.
		/// </summary>
		private bool HasIgnoredTags
		{
			get { return this.ignoreds != null && this.ignoreds.Count > 0; }
		}

		/// <summary>
		/// Get path to the ignoreds text file.
		/// </summary>
		/// <returns></returns>
		private string IgnoredsTxtPath
		{
			get { return App.appDir + @"\" + TXT_IGNOREDS; }
		}

		/// <summary>
		/// Selected search engine in settings.
		/// </summary>
		private Enum.SearchEngine SearchEngine
		{
			get { return (Enum.SearchEngine)Options.Default.SearchEngine; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called when clicking on the Start button, start the search operations.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Start_Click(object sender, RoutedEventArgs e)
		{
			// Start searching
			if (!this.IsRunning && this.ListBox_Files.Items.Count > 0) {
				this.timer = new Timer();
				this.SetStartButton("Stop", "#FFE82B0D");
				this.NextSearch();
			} else { // Stop the search
				this.StopSearches();
			}
		}

		/// <summary>
		/// Called when clicking on the menubar's Options button, open the options window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_Options_Click(object sender, RoutedEventArgs e)
		{
			Option option = new Option();
			option.ShowDialog();
		}

		/// <summary>
		/// Opens a window allowing to edit the Hydrus API connection parameters.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_HydrusApi_Click(object sender, RoutedEventArgs e)
		{
			new HydrusSettings().ShowDialog();
		}

		/// <summary>
		/// Opens a window for setting the SauceNAO's API key.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_SauceNao_Click(object sender, RoutedEventArgs e)
		{
			new SauceNaoSettings().ShowDialog();
		}

		/// <summary>
		/// Called when selecting a row in the files list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void ListBox_Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.Label_Local.Content = "Local";
			this.Label_Match.Content = "Match";
			this.Label_MatchInfos.Content = "";
			this.Image_Local.Source = null;
			this.Image_Match.Source = null;
			this.Border_Local.BorderBrush = this.Border_Match.BorderBrush = this.GetBrushFromString("#505050");
			this.ComboBox_Matches.IsEnabled = false;
			this.ComboBox_TagSources.IsEnabled = false;
			this.GroupBox_Tags.Header = "Tags (0)";
			this.GroupBox_Ignoreds.Header = "Ignoreds (0)";

			if (this.ListBox_Files.SelectedIndex < 0) {
				return;
			}

			Result result = this.SelectedResult;

			// Generate and set the thumbnail
			bool read = await Task.Run(() => this.ReadLocalImage(result));

			// Unable to read the file
			if (!read) {
				this.RemoveResultFromFilesListbox(result);

				return;
			}

			// A different result was selected while generating the thumbnail, don't update the view
			if (this.SelectedResult != result) {
				return;
			}

			this.Image_Local.Source = App.CreateBitmapImage(result.ThumbPath);
			this.Label_SourceInfos.Content = this.HumanReadableFileSize(result.Local.Size) + " (" + result.Local.Width + "x" + result.Local.Height + ")";

			if (result.Local.Format != null) {
				this.Label_Local.Content = "Local " + result.Local.Format.ToUpper();
			}

			this.UpdateRightView(result);
		}

		/// <summary>
		/// Called after clicking on an option from the Files ListBox's context menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void ContextMenu_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			if (mi == null) {
				return;
			}

			switch (mi.Tag) {
				case "writeTagsToFiles":
					this.WriteTagsForSelectedFiles();
				break;
				case "sendTagsToHydrus":
					this.SendTagsForSelectedFiles();
				break;
				case "sendImageUrlsToHydrus":
					this.SendUrlForSelectedFiles(false);
				break;
				case "sendPageUrlsToHydrus":
					this.SendUrlForSelectedFiles(true);
				break;
				case "unignore":
					this.UningnoreSelectItems();
				break;
				case "removeFiles":
					this.RemoveSelectedFiles();
				break;
				case "editTags":
					this.EditSelectedTags();
				break;
				case "removeTags":
					this.RemoveSelectedTags();
				break;
				case "ignore":
					this.IgnoreSelectItems();
				break;
				case "copyFilePaths":
					this.CopySelectedFilePaths();
				break;
				case "openFolder":
					if (this.ListBox_Files.SelectedItem != null) {
						this.StartProcess(Directory.GetParent(this.SelectedResult.ImagePath).FullName);
					}
				break;
				case "deleteFiles":
					await this.DeleteSelectedFiles();
				break;
				case "copyTags":
					App.CopySelectedTagsToClipboard(this.ListBox_Tags);
				break;
				case "copyUnknownTag":
					App.CopySelectedTagsToClipboard(this.ListBox_Ignoreds);
				break;
				case "helpTag":
					this.OpenHelpForSelectedTag(this.ListBox_Tags);
				break;
				case "helpUnknownTag":
					this.OpenHelpForSelectedTag(this.ListBox_Ignoreds);
				break;
				case "searchIqdb":
					await this.SearchSelectedResultWithEngine(Enum.SearchEngine.IQDB);
				break;
				case "searchSauceNao":
					await this.SearchSelectedResultWithEngine(Enum.SearchEngine.SauceNAO);
				break;
				case "resetResult":
					this.ResetSelectedFilesResult();
				break;
				case "addTagsForSelectedResult":
					this.AddTagsForSelectedResult();
				break;
				case "addTagsForSelectedResults":
					this.AddTagsForSelectedResults();
				break;
				case "copyHashes":
					this.CopySelectedHashes();
				break;
				case "copyUrls":
					this.CopySelectedUrls();
				break;
				case "copySourceUrls":
					this.CopySelectedSourceUrls();
				break;
				case "sendThisUrlToHydrus":
					this.SendUrlToHydrus(this.SelectedResult, mi.Header.ToString());
				break;
				case "copyThisUrl":
					Clipboard.SetText(mi.Header.ToString());
				break;
				case "openThisUrl":
					this.StartProcess(mi.Header.ToString());
				break;
			}
		}

		/// <summary>
		/// Called by clicking on a context menu item with a URL as tag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ContextMenu_MenuItem_OpenUrl_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menuItem = sender as MenuItem;

			if (menuItem == null) {
				return;
			}

			this.StartProcess((string)menuItem.Tag);
		}

		/// <summary>
		/// Called when the Files ListBox's context menu is oppened.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Files_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			int countSelected = this.ListBox_Files.SelectedItems.Count;

			// Nothing selected, don't display the context menu
			if (countSelected < 1) {
				return;
			}

			bool singleSelected = (countSelected == 1);
			bool multipleSelecteds = (countSelected > 1);

			bool hasTags = false;
			bool hasUrl = false;
			bool hasFull = false;
			bool hasMatchUrls = false;
			bool hasHydrusFiles = false;
			bool areHydrusFiles = true;

			foreach (Result result in this.ListBox_Files.SelectedItems) {
				if (result.HasTags) {
					hasTags = true;
				}

				if (result.Url != null) {
					hasUrl = true;
				}

				if (result.Full != null) {
					hasFull = true;
				}

				if (hasHydrusFiles == false && result.HydrusFileId != null) {
					hasHydrusFiles = true;
				}

				if (areHydrusFiles == true && result.HydrusFileId == null) {
					areHydrusFiles = false;
				}

				if (result.HasMatches) {
					foreach (Match match in result.Matches) {
						if (match.Url != null || match.SourceUrl != null) {
							hasMatchUrls = true;
						}
					}
				}
			}

			ContextMenu context = new ContextMenu();

			MenuItem item = new MenuItem();
			item.Header = "Hydrus";

				MenuItem sub = new MenuItem();
				sub.Header = "Import file with tags";
				sub.ToolTip = "Imports the local file and its tags into Hydrus";
				sub.Tag = "sendTagsToHydrus";
				sub.IsEnabled = hasTags;
				sub.Click += this.ContextMenu_MenuItem_Click;

				// All the selected files are from a Hydrus query
				if (areHydrusFiles) {
					sub.Header = "Send tags";
					sub.ToolTip = "Tags will be sent to Hydrus for this client file";
				} else if (hasHydrusFiles) { // some of the selected files are from a Hydrus query
					sub.ToolTip += "\n(only tags will be sent for client files imported from a Hydrus query)";
				}

				item.Items.Add(sub);

				sub = new MenuItem();
				sub.Header = "Send match image URL";
				sub.ToolTip = "Sends matched image URL" + (Options.Default.SendUrlWithTags ? " and tags " : " ") + "to Hydrus for downloading";
				sub.Tag = "sendImageUrlsToHydrus";
				sub.IsEnabled = hasFull;
				sub.Click += this.ContextMenu_MenuItem_Click;
				item.Items.Add(sub);

				if (hasFull) {
					int urlCount = 0;

					foreach (Result result in this.ListBox_Files.SelectedItems) {
						if (result.Full == null) {
							continue;
						}

						if (urlCount == 0) {
							sub.ToolTip += "\n" + result.Full;
						}

						urlCount++;
					}

					if (urlCount > 1) {
						sub.ToolTip += "\nand " + (urlCount - 1) + " other URLs";
					}
				}

				sub = new MenuItem();
				sub.Header = "Send match page URL";
				sub.ToolTip = "Sends matched page URL" + (Options.Default.SendUrlWithTags ? " and tags " : " ") + "to Hydrus for processing";
				sub.Tag = "sendPageUrlsToHydrus";
				sub.IsEnabled = hasUrl;
				sub.Click += this.ContextMenu_MenuItem_Click;
				item.Items.Add(sub);

				if (hasUrl) {
					int urlCount = 0;

					foreach (Result result in this.ListBox_Files.SelectedItems) {
						if (result.Url == null) {
							continue;
						}

						if (urlCount == 0) {
							sub.ToolTip += "\n" + result.Url;
						}

						urlCount++;
					}

					if (urlCount > 1) {
						sub.ToolTip += "\nand " + (urlCount - 1) + " other URLs";
					}
				}

				if (singleSelected) {
					sub = new MenuItem();
					sub.Header = "Send this URL to Hydrus...";
					sub.IsEnabled = hasMatchUrls;
					item.Items.Add(sub);

					if (hasMatchUrls) {
						this.AttachMatchUrlsSubmenuToMenuItem(sub, "sendThisUrlToHydrus", "This URL will be sent to Hydrus for processing using the API");
					}
				}

			context.Items.Add(item);

			if (singleSelected) {
				Result selectedResult = this.SelectedResult;
				bool hasUploadedImageUrl = (selectedResult != null && !String.IsNullOrEmpty(selectedResult.UploadedImageUrl));

				item = new MenuItem();
				item.Header = "Search";

					if (hasUploadedImageUrl) {
						sub = new MenuItem();
						sub.Header = "Search with Hatate using...";
						sub.Foreground = this.GetBrushFromString("#FF808080");
						item.Items.Add(sub);
					}

					sub = new MenuItem();
					sub.Header = "IQDB";
					sub.Tag = "searchIqdb";
					sub.Click += this.ContextMenu_MenuItem_Click;
					item.Items.Add(sub);

					sub = new MenuItem();
					sub.Header = "SauceNAO";
					sub.Tag = "searchSauceNao";
					sub.Click += this.ContextMenu_MenuItem_Click;
					item.Items.Add(sub);

					if (hasUploadedImageUrl) {
						context.Items.Add(new Separator());

						sub = new MenuItem();
						sub.Header = "Search externally with...";
						sub.Foreground = this.GetBrushFromString("#FF808080");
						item.Items.Add(sub);

						sub = new MenuItem();
						sub.Header = "Google Images";
						sub.Tag = "https://www.google.com/searchbyimage?image_url=" + selectedResult.UploadedImageUrl + "&amp;safe=off";
						sub.Click += this.ContextMenu_MenuItem_OpenUrl_Click;
						item.Items.Add(sub);

						sub = new MenuItem();
						sub.Header = "TinEye";
						sub.Tag = "https://tineye.com/search?url=" + selectedResult.UploadedImageUrl;
						sub.Click += this.ContextMenu_MenuItem_OpenUrl_Click;
						item.Items.Add(sub);

						sub = new MenuItem();
						sub.Header = "IQDB";
						sub.Tag = "https://iqdb.org?url=" + selectedResult.UploadedImageUrl;
						sub.Click += this.ContextMenu_MenuItem_OpenUrl_Click;
						item.Items.Add(sub);

						sub = new MenuItem();
						sub.Header = "SauceNAO";
						sub.Tag = "https://saucenao.com/search.php?db=999&amp;dbmaski=32768&amp;url=" + selectedResult.UploadedImageUrl;
						sub.Click += this.ContextMenu_MenuItem_OpenUrl_Click;
						item.Items.Add(sub);

						sub = new MenuItem();
						sub.Header = "ascii2d";
						sub.Tag = "https://ascii2d.net/search/url/" + selectedResult.UploadedImageUrl;
						sub.Click += this.ContextMenu_MenuItem_OpenUrl_Click;
						item.Items.Add(sub);

						sub = new MenuItem();
						sub.Header = "Trace.moe";
						sub.Tag = "https://trace.moe/?auto&amp;url=" + selectedResult.UploadedImageUrl;
						sub.Click += this.ContextMenu_MenuItem_OpenUrl_Click;
						item.Items.Add(sub);

						sub = new MenuItem();
						sub.Header = "Yandex";
						sub.Tag = "https://yandex.com/images/search?rpt=imageview&amp;url=" + selectedResult.UploadedImageUrl;
						sub.Click += this.ContextMenu_MenuItem_OpenUrl_Click;
						item.Items.Add(sub);
					}

				context.Items.Add(item);
			}

			item = new MenuItem();
			item.Header = "Copy";

				sub = new MenuItem();
				sub.Header = "File path";
				sub.ToolTip = this.SelectedResult.ImagePath;
				sub.Tag = "copyFilePaths";
				sub.Click += this.ContextMenu_MenuItem_Click;
				item.Items.Add(sub);

				sub = new MenuItem();
				sub.Header = "File hash";
				sub.Tag = "copyHashes";
				sub.Click += this.ContextMenu_MenuItem_Click;
				item.Items.Add(sub);

				sub = new MenuItem();
				sub.Header = "Match URL";
				sub.Tag = "copyUrls";
				sub.IsEnabled = hasUrl;
				sub.Click += this.ContextMenu_MenuItem_Click;
				item.Items.Add(sub);

				if (singleSelected && hasUrl) {
					sub.ToolTip = this.SelectedResult.Url;
				}

				if (singleSelected) {
					sub = new MenuItem();
					sub.Header = "This URL...";
					sub.IsEnabled = hasMatchUrls;
					item.Items.Add(sub);

					if (hasMatchUrls) {
						this.AttachMatchUrlsSubmenuToMenuItem(sub, "copyThisUrl");
					}
				}

			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Open";

				if (singleSelected) {
					sub = new MenuItem();
					sub.Header = "Containing folder";
					sub.Tag = "openFolder";
					sub.Click += this.ContextMenu_MenuItem_Click;
					item.Items.Add(sub);
				}

				sub = new MenuItem();
				sub.Header = "Match URL";
				sub.Tag = "openMatchUrls";
				sub.IsEnabled = hasUrl;
				sub.Click += this.ContextMenu_MenuItem_Click;
				item.Items.Add(sub);

				if (singleSelected && hasUrl) {
					sub.ToolTip = this.SelectedResult.Url;
				}

				if (singleSelected) {
					sub = new MenuItem();
					sub.Header = "This URL...";
					sub.IsEnabled = hasMatchUrls;
					item.Items.Add(sub);

					if (hasMatchUrls) {
						this.AttachMatchUrlsSubmenuToMenuItem(sub, "openThisUrl");
					}
				}

			context.Items.Add(item);

			context.Items.Add(new Separator());

			item = new MenuItem();
			item.Header = "Add tags";
			item.Tag = "addTagsForSelectedResults";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Write tags";
			item.ToolTip = "Tags will be written to a .txt file alongside each selected file(s)";
			item.Tag = "writeTagsToFiles";
			item.IsEnabled = hasTags;
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Reset result";
			item.ToolTip = "Tags, URLs and other search results will be cleared for each selected file(s)";
			item.Tag = "resetResult";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Remove from list";
			item.Tag = "removeFiles";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Delete file";
			item.Tag = "deleteFiles";
			item.Click += this.ContextMenu_MenuItem_Click;

			// Disable item if it's an Hydrus file
			if (singleSelected) {
				Result result = this.SelectedResult;

				if (result != null && this.IsHydrusOwnedFolder(result.ImagePath)) {
					item.IsEnabled = false;
					item.ToolTip = "Can't delete this file as it's one of the Hydrus client files.";
				}
			}

			context.Items.Add(item);

			this.ListBox_Files.ContextMenu = context;
		}

		/// <summary>
		/// Called when the Tags ListBox's context menu is oppened.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Tags_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			int countSelected = this.ListBox_Tags.SelectedItems.Count;

			bool hasSelecteds = (countSelected > 0);
			bool singleSelected = (countSelected == 1);

			this.SetContextMenuItemEnabled(this.ListBox_Tags, 0, hasSelecteds);   // "Edit"
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 1, hasSelecteds);   // "Remove"
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 2, hasSelecteds);   // "Ignore"
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 3, hasSelecteds); // "Copy to clipboard"
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 4, singleSelected); // "Search on Danbooru"
														   // 5 is a separator
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 6, this.ListBox_Files.SelectedItems.Count > 0); // "Add tags"
		}

		/// <summary>
		/// Called when the UnknownTags ListBox's context menu is oppened.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_UnknownTags_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			int countSelected = this.ListBox_Ignoreds.SelectedItems.Count;

			bool hasSelecteds = (countSelected > 0);
			bool singleSelected = (countSelected == 1);

			this.SetContextMenuItemEnabled(this.ListBox_Ignoreds, 0, hasSelecteds);   // "Unignore"
															   // 1 is a separator
			this.SetContextMenuItemEnabled(this.ListBox_Ignoreds, 2, hasSelecteds); // "Copy to clipboard"
			this.SetContextMenuItemEnabled(this.ListBox_Ignoreds, 3, singleSelected); // "Search on Danbooru"
		}

		/// <summary>
		/// Delete all the generated thumbnails.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_DeleteThumbs_Click(object sender, RoutedEventArgs e)
		{
			this.DeleteFilesInFolder(App.ThumbsDirPath);
		}

		/// <summary>
		/// Delete all images in the "temp" subfolder.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_DeleteTemporary_Click(object sender, RoutedEventArgs e)
		{
			this.DeleteFilesInFolder(App.TempDirPath, true);
		}

		/// <summary>
		/// Called when clicking on the menubar's Reload known tags button, reload the tags from the text files.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_ReloadIgnoreds_Click(object sender, RoutedEventArgs e)
		{
			this.LoadIgnoredTags();
		}

		/// <summary>
		/// Remove duplicates entries in the txt files.
		/// Also remove the tags if they are in the ignoreds list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_CleanIgnoreds_Click(object sender, RoutedEventArgs e)
		{
			this.LoadIgnoredTags();
			this.SetStatus("Cleaning known tag lists...");

			int unecessary = 0;

			unecessary += this.CleanIgnoredsTxt();

			this.LoadIgnoredTags();
			this.SetStatus(unecessary + " unecessary tags removed from the lists.");
		}

		/// <summary>
		/// Open a window listing the ignored tags allowing to remove some of them or add new ones.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_ManageIgnoreds_Click(object sender, RoutedEventArgs e)
		{
			Manage window = new Manage(false);

			this.LoadIgnoredTags();

			if (this.ignoreds != null) {
				foreach (string ignored in this.ignoreds) {
					if (String.IsNullOrWhiteSpace(ignored)) {
						continue;
					}

					string[] parts = ignored.Split(':');
					string value = parts[0];
					string nameSpace = null;

					if (parts.Length > 1) {
						value = parts[1];
						nameSpace = parts[0];
					}

					window.AddTag(value, nameSpace);
				}
			}

			window.ShowDialog();

			if (window.OkClicked) {
				this.WriteTagsToTxt(this.IgnoredsTxtPath, window.Tags, false);
			}
		}

		/// <summary>
		/// Called when clicking on the "Add files" menubar item, open a dialog to select files to be added to the list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_AddFiles_Click(object sender, RoutedEventArgs e)
		{
			this.AddFiles();
		}

		/// <summary>
		/// Called when clicking on the "Add files" menubar item, open a dialog to select a folder to add all its files to the list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_AddFolder_Click(object sender, RoutedEventArgs e)
		{
			this.AddFolder();
		}

		/// <summary>
		/// Add an image from the clipboard.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_AddFromClipboard_Click(object sender, RoutedEventArgs e)
		{
			this.ImportImageFromClipboard();
		}

		/// <summary>
		/// Add an image from capturing an area on the screen.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_AddFromArea_Click(object sender, RoutedEventArgs e)
		{
			Capture captureWindow = new Capture();

			if (captureWindow.FilePath == null) {
				return;
			}

			this.AddFileToList(captureWindow.FilePath, null, null, true);

			this.UpdateLabels();
			this.ChangeStartButtonEnabledValue();
		}

		/// <summary>
		/// Opens a window allowing to enter a Hydrus query and import the returned files.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void MenuItem_QueryHydrus_Click(object sender, RoutedEventArgs e)
		{
			HydrusQuery window = new HydrusQuery();
			List<HydrusMetadata> hydrusMetadataList = window.HydrusMetadataList;

			if (hydrusMetadataList == null || hydrusMetadataList.Count < 1) {
				return;
			}

			int count = 0;

			foreach (HydrusMetadata hydrusMetadata in hydrusMetadataList) {
				count++;

				string thumbnailPath = await App.hydrusApi.DownloadThumbnailAsync(hydrusMetadata, App.ThumbsDirPath);

				this.SetStatus("Adding query file " + count + " / " + hydrusMetadataList.Count);
				this.AddFileToList(thumbnailPath, null, hydrusMetadata);
			}


			this.SetStatus(hydrusMetadataList.Count + " files added from Hydrus query.");
			this.UpdateLabels();
			this.ChangeStartButtonEnabledValue();
		}

		/// <summary>
		/// Called when clicking on the "Open program folder" menu item, opens the folder where the program is located.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_OpenProgramFolder_Click(object sender, RoutedEventArgs e)
		{
			this.StartProcess(App.appDir);
		}

		/// <summary>
		/// Called when clicking the "Open matched URLs" menu item, opens the matched URLs text file.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_OpenMatchedUrls_Click(object sender, RoutedEventArgs e)
		{
			string filePath = App.appDir + @"\" + TXT_MATCHED_URLS;

			if (!File.Exists(filePath)) {
				File.CreateText(filePath);
			}

			this.StartProcess(filePath);
		}

		/// <summary>
		/// Called when dropping files onto the Files ListBox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Files_Drop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

			if (files == null) {
				return;
			}

			// Ask for tags if enabled
			List<Tag> tags = this.AskForNewTags();

			// Add images to the list
			foreach (string file in files) {
				if (this.IsCorrespondingToFilter(file, imagesFilesExtensions)) {
					this.AddFileToList(file, tags);
				}
			}

			this.UpdateLabels();
			this.ChangeStartButtonEnabledValue();
		}

		/// <summary>
		/// Called when draging files over the Files ListBox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Files_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				e.Effects = DragDropEffects.All;
			} else {
				e.Effects = DragDropEffects.None;
			}
		}

		/// <summary>
		/// Called each second by the timer.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Timer_Tick(object sender, EventArgs e)
		{
			if (this.timer == null) {
				return;
			}

			this.delay--;

			// The delay has reached the end, start the next search
			if (this.delay <= 0) {
				this.timer.Stop();
				this.timer.Tick -= new EventHandler(Timer_Tick);
				this.timer = new Timer();
				this.NextSearch();
			} else {
				this.SetStatus("Next search in " + this.delay + " seconds");
			}
		}

		/// <summary>
		/// Called when the user try to close the window (by clicking on the close button for example).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void CustomClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			int count = 0;

			// Count results
			for (int i=0; i<this.ListBox_Files.Items.Count; i++) {
				if (this.HasFoundResult(i)) count++;
			}

			if (count > 0) {
				MessageBoxResult result = MessageBox.Show(
					"Some files have search results. Do you realy want to close?",
					"Are you sure?",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning
				);

				e.Cancel = (result == MessageBoxResult.No);
			}
		}

		/// <summary>
		/// Called by clicking on the Original image, open the image in the default viewer.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Original_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ChangedButton == System.Windows.Input.MouseButton.Right) {
				this.OpenCompareWindowForResult(this.SelectedResult);

				return;
			}

			string filepath = this.SelectedResult.ImagePath;

			if (File.Exists(filepath)) {
				Process.Start(filepath);
			}
		}

		/// <summary>
		/// Called by clicking on the Match image, open the booru page for the matching image.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Match_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Result result = this.SelectedResult;

			if (e.ChangedButton == System.Windows.Input.MouseButton.Right) {
				this.OpenCompareWindowForResult(result);

				return;
			}

			if (result == null || String.IsNullOrEmpty(result.Url)) {
				return;
			}

			try {
				Process.Start(result.Url);
			} catch (System.ComponentModel.Win32Exception ex) {
				MessageBox.Show(ex.Message);
			}
		}

		/// <summary>
		/// Remove a tag in the list by double clicking it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Tags_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Result result = this.SelectedResult;

			if (result == null) {
				return;
			}

			result.Tags.Remove((Tag)this.ListBox_Tags.SelectedItem);
			this.ListBox_Tags.Items.Refresh();
		}

		/// <summary>
		/// Called when selecting a different match in the combobox under the match image.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_Matches_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.ComboBox_Matches.SelectedItem == null) {
				return;
			}

			Result result = this.SelectedResult;
			Match match = (Match)this.ComboBox_Matches.SelectedItem;

			// This match is already selected, nothing to do
			if (match == result.Match) {
				return;
			}

			result.Match = match;
			result.ClearWarnings();

			if (Options.Default.ParseTags) {
				this.ParseBooruPage(result);
			}

			this.PortTagsOfMatchToResult(result);
			this.UpdateRightView(result);

			// Refresh the items to update the foreground color from the Result objets
			this.ListBox_Files.Items.Refresh();
		}

		private void MenuItem_Github_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(App.RepositoryUrl);
		}

		private async void MenuItem_CheckForUpdate_Click(object sender, RoutedEventArgs e)
		{
			await Task.Run(() => this.CheckForNewRelease(true));
		}

		private void MenuItem_About_Click(object sender, RoutedEventArgs e)
		{
			new About().ShowDialog();
		}

		// Triggered when the MainWindow is fully loaded.
		private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// Check for new release
			if (Options.Default.StartupReleaseCheck) {
				await Task.Run(() => this.CheckForNewRelease(false));
			}
		}

		/// <summary>
		/// Callend when the Compare window is closed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CompareWindow_Closed(object sender, EventArgs e)
		{
			this.compareWindow = null;
		}

		/// <summary>
		/// Callend when pasting in the MainWindow by having it focused and hitting Ctrl + V.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainWindow_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
		{
			this.ImportImageFromClipboard();

			e.Handled = true;
		}

		/// <summary>
		/// Called when checking one of the radio buttons in the View menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_View_ParenthesisValue_Checked(object sender, RoutedEventArgs e)
		{
			RadioButton radioButton = sender as RadioButton;

			Options.Default.SearchedParenthesisValue = (byte)(ParenthesisValue)radioButton.Tag;
			Options.Default.Save();

			this.ListBox_Files.Items.Refresh();
		}

		/// <summary>
		/// Called when clicking on a tag source checkbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Checkbox_TagSource_Click(object sender, RoutedEventArgs e)
		{
			Result selectedResult = this.SelectedResult;

			if (selectedResult == null) {
				return;
			}

			// Clear current tag sources
			selectedResult.SelectedTagSources.Clear();

			// Add new tag sources
			if ((bool)this.Checkbox_TagSource_User.IsChecked) {
				selectedResult.SelectedTagSources.Add(Enum.TagSource.User);
			}

			if ((bool)this.Checkbox_TagSource_Booru.IsChecked) {
				selectedResult.SelectedTagSources.Add(Enum.TagSource.Booru);
			}

			if ((bool)this.Checkbox_TagSource_SearchEngine.IsChecked) {
				selectedResult.SelectedTagSources.Add(Enum.TagSource.SearchEngine);
			}

			if ((bool)this.Checkbox_TagSource_Hatate.IsChecked) {
				selectedResult.SelectedTagSources.Add(Enum.TagSource.Hatate);
			}

			this.RefreshTagsListBox(selectedResult);

			// Set-as-default button status
			if (this.ListBox_Files.SelectedItems.Count == 1) {
				this.Button_TagSources_SetAsDefault.IsEnabled = this.SetAsDefaultButtonEnabledStatus();
			}
		}

		/// <summary>
		/// Called when clicking on the button under the tag source checkboxes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_TagSources_SetAsDefault_Click(object sender, RoutedEventArgs e)
		{
			System.Collections.IList results;

			if (this.ListBox_Files.SelectedItems.Count > 1) { // "apply for selection"
				results = this.ListBox_Files.SelectedItems;
			} else { // "set as default"
				results = this.ListBox_Files.Items;

				Options.Default.TagSource_User = (bool)this.Checkbox_TagSource_User.IsChecked;
				Options.Default.TagSource_Booru = (bool)this.Checkbox_TagSource_Booru.IsChecked;
				Options.Default.TagSource_SearchEngine = (bool)this.Checkbox_TagSource_SearchEngine.IsChecked;
				Options.Default.TagSource_Hatate = (bool)this.Checkbox_TagSource_Hatate.IsChecked;

				Options.Default.Save();

				// Ask if we want to apply the selected sources for all the other images in the list
				if (!App.AskUser("Also apply those tag sources for all the " + this.ListBox_Files.Items.Count + " images in the list?")) {
					return;
				}
			}

			// Update selected sources and hidden tags for all results in the list (or just the selected ones)
			foreach (Result result in results) {
				result.SelectedTagSources.Clear();

				if ((bool)this.Checkbox_TagSource_User.IsChecked) result.SelectedTagSources.Add(Enum.TagSource.User);
				if ((bool)this.Checkbox_TagSource_Booru.IsChecked) result.SelectedTagSources.Add(Enum.TagSource.Booru);
				if ((bool)this.Checkbox_TagSource_SearchEngine.IsChecked) result.SelectedTagSources.Add(Enum.TagSource.SearchEngine);
				if ((bool)this.Checkbox_TagSource_Hatate.IsChecked) result.SelectedTagSources.Add(Enum.TagSource.Hatate);

				result.UpdateHiddenTagsFromSelectedSources();
			}

			this.Button_TagSources_SetAsDefault.IsEnabled = false;
		}

		/// <summary>
		/// Called when the tag sources dropdown is opened.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_TagSources_DropDownOpened(object sender, EventArgs e)
		{
			if (this.ListBox_Files.SelectedItems.Count > 1) {
				this.Button_TagSources_SetAsDefault.Content = "apply for selection";
				this.Button_TagSources_SetAsDefault.IsEnabled = true;
			} else {
				this.Button_TagSources_SetAsDefault.Content = "set as default";
				this.Button_TagSources_SetAsDefault.IsEnabled = this.SetAsDefaultButtonEnabledStatus();
			}
		}

		#endregion Event
	}
}
