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
using Directory = System.IO.Directory;
using Options = Hatate.Properties.Settings;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Timer = System.Windows.Forms.Timer;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const string DIR_TAGS      = @"\tags\";
		const string DIR_THUMBS    = @"\thumbs\";
		const string DIR_IMGS      = @"\imgs\";
		const string DIR_NOT_FOUND = @"notfound\";
		const string DIR_TAGGED    = @"tagged\";

		const string TXT_UNNAMESPACEDS = "unnamespaceds.txt";
		const string TXT_SERIES        = "series.txt";
		const string TXT_CHARACTERS    = "characters.txt";
		const string TXT_CREATORS      = "creators.txt";
		const string TXT_IGNOREDS      = "ignoreds.txt";

		// Tags list
		private string[] unnamespaceds;
		private string[] series;
		private string[] characters;
		private string[] creators;
		private string[] ignoreds;

		private int lastSearchedInSeconds = 0;
		private int found = 0;
		private int notFound = 0;
		private int delay = 0;
		private Timer timer;

		public MainWindow()
		{
			InitializeComponent();

			if (Options.Default.KnownTags) {
				this.LoadKnownTags();
			}

			this.CreateFilesListContextMenu();
			this.CreateTagsListContextMenu();
			this.CreateUnknownTagsListContextMenu();

			// Prevent closing the window if we have some search results left
			this.Closing += new System.ComponentModel.CancelEventHandler(CustomClosing);
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
			dlg.Filter = "Image files|*.jpg;*.png;*.bmp;*.jpeg";
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
				this.GetImagesFromFolder(fbd.SelectedPath + @"\");
			}

			this.UpdateLabels();
			this.ChangeStartButtonEnabledValue();
		}

		/// <summary>
		/// Load known tags from text files.
		/// </summary>
		private void LoadKnownTags()
		{
			string unnamespaced = this.GetTxtPath(TXT_UNNAMESPACEDS);
			string series = this.GetTxtPath(TXT_SERIES);
			string character = this.GetTxtPath(TXT_CHARACTERS);
			string creator = this.GetTxtPath(TXT_CREATORS);
			string ignored = this.GetTxtPath(TXT_IGNOREDS);

			if (File.Exists(unnamespaced)) {
				this.unnamespaceds = File.ReadAllLines(unnamespaced);
			}

			if (File.Exists(series)) {
				this.series = File.ReadAllLines(series);
			}

			if (File.Exists(character)) {
				this.characters = File.ReadAllLines(character);
			}

			if (File.Exists(creator)) {
				this.creators = File.ReadAllLines(creator);
			}

			if (File.Exists(ignored)) {
				this.ignoreds = File.ReadAllLines(ignored);
			}

			this.SetStatus("Tags loaded.");
		}

		/// <summary>
		/// Get all the images in the working directory and add them to the list.
		/// </summary>
		private void GetImagesFromFolder(string path)
		{
			string[] files = "*.jpg|*.jpeg|*.png|*.bmp".Split('|').SelectMany(filter => Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly)).ToArray();

			this.ListBox_Files.Items.Clear();
			this.ListBox_Tags.Items.Clear();
			this.ListBox_UnknownTags.Items.Clear();

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
		/// Generate a smaller image.
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="filename"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		private string GenerateThumbnail(string filepath, int width=150)
		{
			string thumbsDir = this.ThumbsDirPath;
			string output = thumbsDir + this.GetFilenameFromPath(filepath);

			if (File.Exists(output)) {
				return output;
			}

			Directory.CreateDirectory(thumbsDir);

			System.Drawing.Image image = System.Drawing.Image.FromFile(filepath);
			int srcWidth = image.Width;
			int srcHeight = image.Height;
			Decimal sizeRatio = ((Decimal)srcHeight / srcWidth);
			int thumbHeight = Decimal.ToInt32(sizeRatio * width);
			Bitmap bmp = new Bitmap(width, thumbHeight);
			Graphics gr = Graphics.FromImage(bmp);
			gr.SmoothingMode = SmoothingMode.HighQuality;
			gr.CompositingQuality = CompositingQuality.HighQuality;
			gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
			Rectangle rectDestination = new Rectangle(0, 0, width, thumbHeight);
			gr.DrawImage(image, rectDestination, 0, 0, srcWidth, srcHeight, GraphicsUnit.Pixel);

			try {
				bmp.Save(output, ImageFormat.Jpeg);
			} catch (Exception e) {
				Console.WriteLine("Error during thumbnail creation: " + e.Message);
			}

			// Liberate resources
			image.Dispose();
			bmp.Dispose();
			gr.Dispose();

			return output;
		}

		/// <summary>
		/// Get the next row index in the list.
		/// </summary>
		/// <returns></returns>
		private int GetNextIndex()
		{
			int progress = 0;

			// Will run until the list is empty or every files in it has been searched
			// NOTE: progress is incremented each time the loop doesn't reach the end where it is reset to 0
			while (this.ListBox_Files.Items.Count > 0) {
				// No more files
				if (progress >= this.ListBox_Files.Items.Count) {
					return -1;
				}

				Result result = this.GetResultFromIndex(progress);

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

			await this.SearchFile(progress);

			// This is the last search, end here
			if (progress >= this.ListBox_Files.Items.Count - 1) {
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

			this.timer = new Timer();
			this.timer.Interval = 1000;
			this.timer.Tick += new EventHandler(Timer_Tick);
			this.timer.Start();
		}

		/// <summary>
		/// Search a single file using its index in the Files list.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private async Task SearchFile(int index)
		{
			var item = this.ListBox_Files.Items[index];
			string filepath = item.ToString();

			// Remove non existant file
			if (!File.Exists(filepath)) {
				this.ListBox_Files.Items.Remove(item);

				return;
			}

			Result result = this.GetResultFromItem(item);
			result.Searched = true;

			// Generate a smaller image for uploading
			this.SetStatus("Generating thumbnail...");
			result.ThumbPath = this.GenerateThumbnail(filepath);

			// Search the image on IQDB
			this.SetStatus("Searching file on IQDB...");
			await this.RunIqdbApi(result);

			// Attach result to the row
			this.GetFilesListBoxItemFromItem(item).Tag = result;

			// We have tags
			if (result.Greenlight) {
				this.SetStatus("File found.");

				// Move or update the color
				if (Options.Default.AutoMove && result.UnknownTags.Count == 0) {
					this.WriteTagsForItem(item);
				} else {
					this.UpdateFileItemColor(item, result.HasKnownTags ? Brushes.LimeGreen : Brushes.Orange);
				}

				this.found++;
			} else { // No tags were found
				this.SetStatus("File not found.");
				
				// Move or update the color
				if (Options.Default.AutoMove) {
					this.MoveRowToNotFoundFolder(item);
				} else {
					this.UpdateFileItemColor(item, Brushes.Red);
				}

				this.notFound++;
			}

			// Update counters (remaining, found, not found)
			this.UpdateLabels();
		}

		/// <summary>
		/// Run the IQDB search.
		/// </summary>
		/// <param name="api"></param>
		/// <param name="thumbPath"></param>
		/// <param name="filename"></param>
		/// <returns></returns>
		private async Task RunIqdbApi(Result result)
		{
			FileStream fs = null;

			try {
				fs = new FileStream(result.ThumbPath, FileMode.Open);
			} catch (IOException) {
				return; // May happen if the file is in use
			}

			IqdbApi.Models.SearchResult searchResult = null;

			try {
				searchResult = await new IqdbApi.IqdbApi().SearchFile(fs);
			} catch (Exception) {
				// FormatException may happen in case of an invalid HTML response where no tags can be parsed
			}

			// No result found
			if (searchResult != null && searchResult.Matches != null) {
				this.lastSearchedInSeconds = (int)searchResult.SearchedInSeconds;

				// If found, check for matching results
				this.CheckMatches(searchResult.Matches, result);
			}

			fs.Close();
			fs.Dispose();
		}

		/// <summary>
		/// Check the various matches to find the best one.
		/// </summary>
		private void CheckMatches(System.Collections.Immutable.ImmutableList<IqdbApi.Models.Match> matches, Result result)
		{
			foreach (IqdbApi.Models.Match match in matches) {
				// Check minimum similarity and number of tags
				if (match.Similarity < Options.Default.Similarity || match.Tags == null || match.Tags.Count < Options.Default.TagsCount) {
					continue;
				}

				// Check match type if enabled
				if (Options.Default.CheckMatchType && match.MatchType > Options.Default.MatchType) {
					continue;
				}

				// Check source
				if (!this.CheckSource(match.Source)) {
					continue;
				}

				result.PreviewUrl = "http://iqdb.org" + match.PreviewUrl;
				result.Source = match.Source;
				result.Rating = match.Rating;

				this.FilterTags(result, match.Tags.ToList());

				return;
			}
		}

		/// <summary>
		/// Takes the tag list and keep only the ones that are valid and are present in the text files if enabled.
		/// </summary>
		/// <returns></returns>
		private void FilterTags(Result result, List<string> tags)
		{
			result.UnknownTags.Clear();

			// Write each tags
			foreach (string tag in tags) {
				// Format the tag
				string formated = tag;

				formated = formated.Replace("_", " ");
				formated = formated.Replace(",", "");
				formated = formated.ToLower().Trim();

				if (String.IsNullOrWhiteSpace(formated)) {
					continue;
				}

				Tag found = this.FindTag(formated);
				bool isIgnored = this.IsTagInList(formated, this.ignoreds);

				// Tag not found in the known tags
				if (found == null) {
					if (Options.Default.KnownTags && !isIgnored) {
						Tag formatedTag = new Tag(formated);

						if (!result.UnknownTags.Contains(formatedTag)) {
							result.UnknownTags.Add(formatedTag);
						}
					}

					continue;
				}

				if (!isIgnored && !result.KnownTags.Contains(found)) {
					result.KnownTags.Add(found);
				}
			}

			// Add rating
			if (Options.Default.AddRating && result.Rating != IqdbApi.Enums.Rating.Unrated) {
				string rating = result.Rating.ToString().ToLower();

				result.KnownTags.Add(new Tag(rating, "rating"));
			}
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
		/// Update the color of a file row using its index.
		/// </summary>
		/// <param name="index"></param>
		private void UpdateFileItemColor(object item, Brush brush)
		{
			ListBoxItem lbItem = this.GetFilesListBoxItemFromItem(item);

			lbItem.Background = brush;
			lbItem.Foreground = Brushes.White;
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
		/// Check if the given source checked in the options.
		/// </summary>
		/// <param name=""></param>
		/// <returns></returns>
		private bool CheckSource(IqdbApi.Enums.Source source)
		{
			switch (source) {
				case IqdbApi.Enums.Source.Danbooru:
					return Options.Default.Source_Danbooru;
				case IqdbApi.Enums.Source.Konachan:
					return Options.Default.Source_Konachan;
				case IqdbApi.Enums.Source.Yandere:
					return Options.Default.Source_Yandere;
				case IqdbApi.Enums.Source.Gelbooru:
					return Options.Default.Source_Gelbooru;
				case IqdbApi.Enums.Source.SankakuChannel:
					return Options.Default.Source_SankakuChannel;
				case IqdbApi.Enums.Source.Eshuushuu:
					return Options.Default.Source_Eshuushuu;
				case IqdbApi.Enums.Source.TheAnimeGallery:
					return Options.Default.Source_TheAnimeGallery;
				case IqdbApi.Enums.Source.Zerochan:
					return Options.Default.Source_Zerochan;
				case IqdbApi.Enums.Source.AnimePictures:
					return Options.Default.Source_AnimePictures;
			}

			return false;
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
					file.WriteLine(tag.Value);
				}
			}
		}

		/// <summary>
		/// Write a list of Tag objects to the txt files.
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="tags"></param>
		/// <param name="append"></param>
		private void WriteTagsToTxt(List<Tag> tags)
		{
			foreach (Tag tag in tags) {
				string txt = this.GetTxtFromNamespace(tag.Namespace);

				using (StreamWriter file = new StreamWriter(this.GetTxtPath(txt), true)) {
					file.WriteLine(tag.Value);
				}
			}
		}

		/// <summary>
		/// Find a tag in one of the known tags list.
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		private Tag FindTag(string tag)
		{
			if (!Options.Default.KnownTags) {
				return new Tag(tag);
			}

			if (this.IsTagInList(tag, this.unnamespaceds)) {
				return new Tag(tag);
			} else if (this.IsTagInList(tag, this.series)) {
				return new Tag(tag, "series");
			} else if (this.IsTagInList(tag, this.characters)) {
				return new Tag(tag, "character");
			} else if (this.IsTagInList(tag, this.creators)) {
				return new Tag(tag, "creator");
			}

			return null;
		}

		/// <summary>
		/// Create the context menu for the Files ListBox.
		/// </summary>
		private void CreateFilesListContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item = new MenuItem();
			item.Header = "Write tags";
			item.Tag = "writeTags";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Not found";
			item.Tag = "notFound";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Reset result";
			item.Tag = "resetResult";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Remove";
			item.Tag = "removeFiles";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			context.Items.Add(new Separator());

			item = new MenuItem();
			item.Header = "Search again";
			item.Tag = "searchAgain";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Copy path";
			item.Tag = "copyFilePath";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Open file";
			item.Tag = "launchFile";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			this.ListBox_Files.ContextMenu = context;
		}

		/// <summary>
		/// Create the context menu for the Tags ListBox.
		/// </summary>
		private void CreateTagsListContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item = new MenuItem();
			item.Header = "Remove";
			item.Tag = "removeTags";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Remove and ignore";
			item.Tag = "removeAndIgnore";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			context.Items.Add(new Separator());

			item = new MenuItem();
			item.Header = "Copy to clipboard";
			item.Tag = "copyTag";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Search on Danbooru";
			item.Tag = "helpTag";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Add new tag";
			item.Tag = "addNew";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			this.ListBox_Tags.ContextMenu = context;
		}

		/// <summary>
		/// Create the context menu for the UnknownTags ListBox.
		/// </summary>
		private void CreateUnknownTagsListContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			item = new MenuItem();
			item.Header = "Add as unnamespaced";
			item.Tag = "addUnnamespaced";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Add as series";
			item.Tag = "addSeries";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Add as character";
			item.Tag = "addCharacter";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Add as creator";
			item.Tag = "addCreator";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Add as ignored";
			item.Tag = "addIgnored";
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

			this.ListBox_UnknownTags.ContextMenu = context;
		}

		/// <summary>
		/// Move a given item's file to the tagged folder with the tags in a .txt file and remove the row.
		/// </summary>
		/// <param name="item"></param>
		private void WriteTagsForItem(object item)
		{
			ListBoxItem listBoxItem = this.ListBox_Files.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
			Result result = (Result)listBoxItem.Tag;

			// No result or no tags to write
			if (result == null || result.KnownTags.Count == 0) {
				return;
			}

			string filepath = item.ToString();

			if (File.Exists(filepath)) {
				string filename = this.GetFilenameFromPath(filepath);
				string taggedDirPath = this.TaggedDirPath;

				// Move the file to the tagged folder and write tags
				File.Move(filepath, taggedDirPath + filename);
				this.WriteTagsToTxt(taggedDirPath + filename + ".txt", result.KnownTags);
			}

			// Remove the row
			this.ListBox_Files.Items.Remove(item);
		}

		/// <summary>
		/// Move a files to the not "notfound" folder and remove the row.
		/// </summary>
		/// <param name="index"></param>
		private void MoveRowToNotFoundFolder(object item)
		{
			string filepath = item.ToString();
			string destination = this.NotfoundDirPath + this.GetFilenameFromPath(filepath);

			// Windows does not support longer file paths, causing a PathTooLongException
			if (destination.Length >= 260) {
				return;
			}

			if (File.Exists(filepath)) {
				File.Move(filepath, destination);
			}

			this.ListBox_Files.Items.Remove(item);
		}

		/// <summary>
		/// Check if a row in the Files ListBox has an associated result.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private bool HasFoundResult(int index)
		{
			Result result = this.GetResultFromIndex(index);

			return result != null && result.Found;
		}

		/// <summary>
		/// Get a ListBoxItem from the Files ListBox using one of its items.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		private ListBoxItem GetFilesListBoxItemFromItem(object item)
		{
			return this.ListBox_Files.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
		}

		/// <summary>
		/// Get a ListBoxItem from the Files ListBox using an index.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		private ListBoxItem GetFilesListBoxItemFromIndex(int index)
		{
			return this.ListBox_Files.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;
		}

		/// <summary>
		/// Get the result object attached to an item in the Files ListBox.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private Result GetResultFromIndex(int index)
		{
			if (index < 0) {
				return null;
			}

			return this.GetResultFromItem(this.ListBox_Files.Items[index]);
		}

		/// <summary>
		/// Get the result object attached to an item from the Files ListBox.
		/// </summary>
		private Result GetResultFromItem(object item)
		{
			if (this.ListBox_Files.Items.Count < 1) {
				return null;
			}

			ListBoxItem listBoxItem = this.GetFilesListBoxItemFromItem(item);

			return listBoxItem != null ? (Result)listBoxItem.Tag : null;
		}

		/// <summary>
		/// Extract the filename (eg. something.jpg) from a full path (eg. c:/somedir/something.jpg).
		/// </summary>
		private string GetFilenameFromPath(string filepath)
		{
			return filepath.Substring(filepath.LastIndexOf(@"\") + 1, filepath.Length - filepath.LastIndexOf(@"\") - 1);
		}

		/// <summary>
		/// Move an item from a list to another one.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		private void MoveSelectedItemsToList(ListBox from, ListBox to, string nameSpace=null)
		{
			while (from.SelectedItems.Count > 0) {
				Tag tag = (Tag)from.SelectedItems[0];

				from.Items.Remove(tag);

				if (nameSpace != null) {
					tag.Namespace = nameSpace;
				}

				to.Items.Add(tag);
			}
		}

		/// <summary>
		/// Remove all the selected items from a given list.
		/// </summary>
		private void RemoveSelectedItemsFromList(ListBox from)
		{
			while (from.SelectedItems.Count > 0) {
				from.Items.Remove(from.SelectedItems[0]);
			}
		}

		/// <summary>
		/// Remove result and background color of all the selected files.
		/// </summary>
		private void ResetSelectedFilesResult()
		{
			foreach (string item in this.ListBox_Files.SelectedItems) {
				ListBoxItem lbItem = this.GetFilesListBoxItemFromItem(item);

				lbItem.Tag = null;
				lbItem.Background = null;
				lbItem.Foreground = this.GetBrushFromString("#FFD2D2D2");
			}
		}

		/// <summary>
		/// Write all the selected items from the given list into a txt file.
		/// </summary>
		private void WriteSelectedItemsToTxt(string filepath, ListBox from)
		{
			using (StreamWriter file = new StreamWriter(filepath, true)) {
				foreach (Tag item in from.SelectedItems) {
					file.WriteLine(item.Value);
				}
			}

			this.LoadKnownTags();
		}

		/// <summary>
		/// Add an unknown tags to the tag list.
		/// </summary>
		private void MoveSelectedUnknownTagsToKnownTags(string txt, string nameSpace=null)
		{
			if (this.ListBox_UnknownTags.Items.Count < 1) {
				return;
			}

			this.WriteSelectedItemsToTxt(this.GetTxtPath(txt), this.ListBox_UnknownTags);
			this.MoveSelectedItemsToList(this.ListBox_UnknownTags, this.ListBox_Tags, nameSpace);

			this.Button_Apply.IsEnabled = true;
		}

		/// <summary>
		/// Rewrite tags in a txt file without duplicates.
		/// Also remove the tags if they are in the ignoreds list.
		/// </summary>
		/// <param name="txt"></param>
		/// <param name="tags"></param>
		private int CleanKnownTagList(string txt, string[] tags, bool excludeIgnored=true)
		{
			string path = this.GetTxtPath(txt);

			if (!File.Exists(path)) {
				return 0;
			}

			List<string> copies = new List<string>();
			int unecessary = 0;

			foreach (string tag in tags) {
				if (copies.Contains(tag) || (excludeIgnored && this.IsTagInList(tag, this.ignoreds))) {
					unecessary++;
				} else {
					copies.Add(tag);
				}
			}

			this.WriteTagsToTxt(path, copies, false);

			return unecessary;
		}

		/// <summary>
		/// Write tags in a txt file without the ones in the exclude list.
		/// </summary>
		private void WriteTagsToTxtWithout(string txt, string[] tags, List<string> excludes)
		{
			if (!File.Exists(App.appDir + txt)) {
				return;
			}

			List<string> includes = new List<string>();

			foreach (string tag in tags) {
				if (!excludes.Contains(tag)) {
					includes.Add(tag);
				}
			}

			this.WriteTagsToTxt(App.appDir + txt, includes, false);
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
		/// Create a directory if it doesn't exists yet. 
		/// </summary>
		/// <returns></returns>
		private void CreateDirIfNeeded(string path)
		{
			if (!Directory.Exists(path)) {
				Directory.CreateDirectory(path);
			}
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
		private void AddFileToList(string filepath, List<Tag> tags=null)
		{
			// Windows does not support longer file paths, causing a PathTooLongException
			if (filepath.Length >= 260) {
				return;
			}

			string filename = this.GetFilenameFromPath(filepath);

			if (this.ListBox_Files.Items.Contains(filepath)
			|| File.Exists(this.TaggedDirPath + filename)
			|| File.Exists(this.NotfoundDirPath + filename)) {
				return;
			}

			this.ListBox_Files.Items.Add(filepath);

			Result result = new Result();

			// Add tags to the result
			if (tags != null && tags.Count > 0) {
				foreach (Tag tag in tags) {
					result.KnownTags.Add(tag);
				}
			}

			// Attach result to the row
			int count = this.ListBox_Files.Items.Count;
			this.GetFilesListBoxItemFromIndex(count-1).Tag = result;
		}

		/// <summary>
		/// Get the path to one of the text file.
		/// </summary>
		/// <returns></returns>
		private string GetTxtPath(string txt)
		{
			return App.appDir + DIR_TAGS + txt;
		}

		/// <summary>
		/// Add all the selected items in a given list to he ignoreds list and remove them from the listbox.
		/// </summary>
		private void IngnoreSelectItemsFromList(ListBox from)
		{
			this.WriteSelectedItemsToTxt(this.GetTxtPath(TXT_IGNOREDS), from);
			this.RemoveSelectedItemsFromList(from);
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
			Process myProcess = Process.Start(new ProcessStartInfo(path));
		}

		/// <summary>
		/// Copy the selected item of a given listbox to the clipboard.
		/// </summary>
		/// <param name="from"></param>
		private void CopySelectedTagToClipboard(ListBox from)
		{
			if (from.SelectedItem != null) {
				Tag item = from.SelectedItem as Tag;

				Clipboard.SetText(item.Underscored);
			}
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
			((MenuItem)listBox.ContextMenu.Items[index]).IsEnabled = enabled;
		}

		/// <summary>
		/// Enable or disable the Start button depending if the list is empty or if the search loop is already running.
		/// </summary>
		private void ChangeStartButtonEnabledValue()
		{
			this.Button_Start.IsEnabled = this.ListBox_Files.Items.Count > 0;
		}

		/// <summary>
		/// Check if a tag is in the given list.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="list"></param>
		/// <returns></returns>
		private bool IsTagInList(string tag, string[] list)
		{
			if (list == null) {
				return false;
			}

			return list.Contains(tag);
		}

		/// <summary>
		/// Open a window to input a new tag and save it for the selected file and into the known tags.
		/// </summary>
		private void AddNewTags()
		{
			List<Tag> tags = this.AskForNewTags(true);

			if (tags.Count == 0) {
				return;
			}

			foreach (Tag tag in tags) {
				string txt = this.GetTxtFromNamespace(tag.Namespace);

				// Write the new tag to the txt
				using (StreamWriter file = new StreamWriter(this.GetTxtPath(txt), true)) {
					file.WriteLine(tag.Value);
				}

				// Append the new tag to the list
				if (!this.ListBox_Tags.Items.Contains(tag)) {
					this.ListBox_Tags.Items.Add(tag);
				}
			}

			this.Button_Apply.IsEnabled = true;
		}

		/// <summary>
		/// Get path of one of the txt files depending on a namespace.
		/// </summary>
		/// <param name="nameSpace"></param>
		/// <returns></returns>
		private string GetTxtFromNamespace(string nameSpace)
		{
			if (String.IsNullOrEmpty(nameSpace)) {
				return TXT_UNNAMESPACEDS;
			}

			switch (nameSpace) {
				case "series": return TXT_UNNAMESPACEDS;
				case "character": return TXT_UNNAMESPACEDS;
				case "creator": return TXT_UNNAMESPACEDS;
			}

			return TXT_UNNAMESPACEDS;
		}

		/// <summary>
		/// Open a window asking for new tags, write them to the txt files then return them.
		/// </summary>
		/// <param name="bypassOption">If true, open the ask tag window even if the AskTags option is disabled</param>
		private List<Tag> AskForNewTags(bool bypassOption=false)
		{
			List<Tag> tags = new List<Tag>();

			if (bypassOption || Options.Default.AskTags) {
				NewTags newTags = new NewTags();
				tags = newTags.Tags;

				this.WriteTagsToTxt(tags);
			}

			return tags;
		}

		/// <summary>
		/// Move all the selected files to the "notfound" folder.
		/// </summary>
		private void MoveAllSelectedsToNotFound()
		{
			bool asked = false;

			while (this.ListBox_Files.SelectedItems.Count > 0) {
				var item = this.ListBox_Files.SelectedItems[0];
				Result result = this.GetResultFromItem(item);

				// Warn when trying to move to notfound with a result
				if (!asked && result != null && result.Greenlight) {
					asked = true;

					System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.MessageBox.Show(
						"Some of the selected files were found with tags, do you want to continue moving to the notfound folder?",
						"Attention", System.Windows.Forms.MessageBoxButtons.YesNo
					);

					if (dialogResult == System.Windows.Forms.DialogResult.No) {
						return;
					}
				}

				this.MoveRowToNotFoundFolder(item);
			}
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// Get the full path to the thumbs folder under the application directory.
		/// </summary>
		private string ThumbsDirPath
		{
			get {
				string path = App.appDir + DIR_THUMBS;

				this.CreateDirIfNeeded(path);

				return path;
			}
		}

		/// <summary>
		/// Get the full path to the tagged subfolder under the working directory.
		/// </summary>
		private string TaggedDirPath
		{
			get
			{
				string path = App.appDir + DIR_IMGS + DIR_TAGGED;

				this.CreateDirIfNeeded(path);

				return path;
			}
		}

		/// <summary>
		/// Get the full path to the notfound subfolder under the working directory.
		/// </summary>
		private string NotfoundDirPath
		{
			get
			{
				string path = App.appDir + DIR_IMGS + DIR_NOT_FOUND;

				if (!Directory.Exists(path)) {
					Directory.CreateDirectory(path);
				}

				return path;
			}
		}

		/// <summary>
		/// List of accepted image extentions.
		/// </summary>
		private string[] ImagesFilesExtensions
		{
			get
			{
				return new string[] { ".png", ".jpg", ".jpeg", ".bmp" };
			}
		}

		/// <summary>
		/// Check if the search process is currently running.
		/// </summary>
		private bool IsRunning
		{
			get { return this.timer != null && this.timer.Enabled; }
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
				this.SetStartButton("Stop", "#FFE82B0D");
				this.NextSearch();
			} else { // Stop the search
				this.timer.Stop();

				this.SetStatus("Stopped.");
				this.SetStartButton("Start", "#FF3CB21A");
			}
		}

		/// <summary>
		/// Called when clicking on the menubar's Options button, open the options window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_Options_Click(object sender, RoutedEventArgs e)
		{
			Option potion = new Option();
		}

		/// <summary>
		/// Called when clicking on the menubar's Reload known tags button, reload the tags from the text files.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_ReloadKnownTags_Click(object sender, RoutedEventArgs e)
		{
			this.LoadKnownTags();
		}

		/// <summary>
		/// Called when selecting a row in the files list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.ListBox_Tags.Items.Clear();
			this.ListBox_UnknownTags.Items.Clear();

			this.Label_Match.Content = "Match";
			this.Image_Original.Source = null;
			this.Image_Match.Source = null;
			this.Button_Apply.IsEnabled = false;

			if (this.ListBox_Files.SelectedIndex < 0) {
				return;
			}

			Result result = this.GetResultFromIndex(this.ListBox_Files.SelectedIndex);

			if (result == null) {
				return;
			}

			// Set the images
			try {
				if (result.ThumbPath != null) {
					// Regenerate the thumbnail if it don't exists anymore
					if (!File.Exists(result.ThumbPath)) {
						result.ThumbPath = this.GenerateThumbnail(this.ListBox_Files.Items[this.ListBox_Files.SelectedIndex].ToString());
					}

					this.Image_Original.Source = new BitmapImage(new Uri(result.ThumbPath));
				}

				if (result.PreviewUrl != null) {
					this.Image_Match.Source = new BitmapImage(new Uri(result.PreviewUrl));
				}
			} catch (Exception) {
				// UriFormatException may happen if the uri is incorrect
			}

			// Add known tags to the list
			result.KnownTags.Sort();

			foreach (Tag tag in result.KnownTags) {
				this.ListBox_Tags.Items.Add(tag);
			}

			// The following need the result to be found
			if (!result.Greenlight) {
				return;
			}

			// Set the source name
			this.Label_Match.Content = result.Source.ToString();

			// Add unknown tags to the list
			result.UnknownTags.Sort();

			foreach (Tag tag in result.UnknownTags) {
				this.ListBox_UnknownTags.Items.Add(tag);
			}
		}

		/// <summary>
		/// Called after clicking on an option from the Files ListBox's context menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ContextMenu_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			if (mi == null) {
				return;
			}

			switch (mi.Tag) {
				case "writeTags":
					while (this.ListBox_Files.SelectedItems.Count > 0) {
						this.WriteTagsForItem(this.ListBox_Files.SelectedItems[0]);
					}
				break;
				case "notFound":
					this.MoveAllSelectedsToNotFound();
				break;
				case "addUnnamespaced":
					this.MoveSelectedUnknownTagsToKnownTags(TXT_UNNAMESPACEDS);
				break;
				case "addSeries":
					this.MoveSelectedUnknownTagsToKnownTags(TXT_SERIES, "series");
				break;
				case "addCharacter":
					this.MoveSelectedUnknownTagsToKnownTags(TXT_CHARACTERS, "character");
				break;
				case "addCreator":
					this.MoveSelectedUnknownTagsToKnownTags(TXT_CREATORS, "creator");
				break;
				case "addIgnored":
					this.IngnoreSelectItemsFromList(this.ListBox_UnknownTags);
				break;
				case "removeFiles":
					this.RemoveSelectedItemsFromList(this.ListBox_Files);
				break;
				case "removeTags":
					// Will need to use WriteTagsToTxtWithout() here
					this.MoveSelectedItemsToList(this.ListBox_Tags, this.ListBox_UnknownTags);
					this.Button_Apply.IsEnabled = true;
				break;
				case "removeAndIgnore":
					// Will need to use WriteTagsToTxtWithout() here
					this.IngnoreSelectItemsFromList(this.ListBox_Tags);
					this.Button_Apply.IsEnabled = true;
				break;
				case "copyFilePath":
					Clipboard.SetText(this.ListBox_Files.SelectedItem.ToString());
					break;
				case "launchFile":
					if (this.ListBox_Files.SelectedItem != null) {
						this.StartProcess(this.ListBox_Files.SelectedItem.ToString());
					}
				break;
				case "copyTag":
					this.CopySelectedTagToClipboard(this.ListBox_Tags);
				break;
				case "copyUnknownTag":
					this.CopySelectedTagToClipboard(this.ListBox_UnknownTags);
				break;
				case "helpTag":
					this.OpenHelpForSelectedTag(this.ListBox_Tags);
				break;
				case "helpUnknownTag":
					this.OpenHelpForSelectedTag(this.ListBox_UnknownTags);
				break;
				case "searchAgain":
					this.SearchFile(this.ListBox_Files.SelectedIndex);
				break;
				case "resetResult":
					this.ResetSelectedFilesResult();
				break;
				case "addNew":
					this.AddNewTags();
				break;
			}
		}

		/// <summary>
		/// Called when the Files ListBox's context menu is oppened.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Files_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			int countSelected = this.ListBox_Files.SelectedItems.Count;

			bool hasSelecteds = (countSelected > 0);
			bool singleSelected = (countSelected == 1);

			this.SetContextMenuItemEnabled(this.ListBox_Files, 0, hasSelecteds);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 1, hasSelecteds);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 2, hasSelecteds);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 3, hasSelecteds);
															// 4 is a separator
			this.SetContextMenuItemEnabled(this.ListBox_Files, 5, singleSelected);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 6, singleSelected);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 7, singleSelected);
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

			this.SetContextMenuItemEnabled(this.ListBox_Tags, 0, hasSelecteds);   // "Remove"
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 1, hasSelecteds);   // "Remove and ignore"
														   // 2 is a separator
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 3, singleSelected); // "Copy to clipboard"
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 4, singleSelected); // "Search on Danbooru"
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 5, this.HasFoundResult(this.ListBox_Files.SelectedIndex)); // "Add new tag"
		}

		/// <summary>
		/// Called when the UnknownTags ListBox's context menu is oppened.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_UnknownTags_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			int countSelected = this.ListBox_UnknownTags.SelectedItems.Count;

			bool hasSelecteds = (countSelected > 0);
			bool singleSelected = (countSelected == 1);

			this.SetContextMenuItemEnabled(this.ListBox_UnknownTags, 0, hasSelecteds);   // "Add as unnamespaced"
			this.SetContextMenuItemEnabled(this.ListBox_UnknownTags, 1, hasSelecteds);   // "Add as series"
			this.SetContextMenuItemEnabled(this.ListBox_UnknownTags, 2, hasSelecteds);   // "Add as character"
			this.SetContextMenuItemEnabled(this.ListBox_UnknownTags, 3, hasSelecteds);   // "Add as creator"
			this.SetContextMenuItemEnabled(this.ListBox_UnknownTags, 4, hasSelecteds);   // "Add as ignored"
																  // 5 is a separator
			this.SetContextMenuItemEnabled(this.ListBox_UnknownTags, 6, singleSelected); // "Copy to clipboard"
			this.SetContextMenuItemEnabled(this.ListBox_UnknownTags, 7, singleSelected); // "Search on Danbooru"
		}

		/// <summary>
		/// Called when clicking on the "Apply" button.
		/// Update a result from the changes made in the right panel.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Apply_Click(object sender, RoutedEventArgs e)
		{
			this.Button_Apply.IsEnabled = false;

			int index = this.ListBox_Files.SelectedIndex;
			Result result = this.GetResultFromIndex(index);

			result.KnownTags.Clear();
			result.UnknownTags.Clear();

			foreach (Tag item in this.ListBox_Tags.Items) {
				result.KnownTags.Add(item);
			}

			foreach (Tag item in this.ListBox_UnknownTags.Items) {
				result.UnknownTags.Add(item);
			}

			// Reload known tags
			this.LoadKnownTags();

			// Re-filter tags for all the other files so when we add a new known tag it will be applied for all the other files
			for (int i = 0; i < this.ListBox_Files.Items.Count; i++) {
				// Don't redo what we've just done
				if (i == index) {
					continue;
				}

				Result otherResult = this.GetResultFromIndex(i);

				if (otherResult == null || !result.Found) {
					continue;
				}

				// Build a list of tags to compare
				List<string> list = new List<string>();

				if (otherResult.HasKnownTags) {
					// Prevent the rating from being added as an unknown tag by removing it from the known tags
					Tag ratingTag = otherResult.KnownTags.Find(t => t.Namespace != null && t.Namespace.Equals("rating"));

					if (ratingTag != null) {
						otherResult.KnownTags.Remove(ratingTag);
					}

					foreach (Tag tag in otherResult.KnownTags) {
						list.Add(tag.Value);
					}
				}

				if (otherResult.UnknownTags != null) {
					foreach (Tag tag in otherResult.UnknownTags) {
						list.Add(tag.Value);
					}
				}

				this.FilterTags(otherResult, list);
			}
		}

		/// <summary>
		/// Delete all the generated thumbnails.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_DeleteThumbs_Click(object sender, RoutedEventArgs e)
		{
			this.SetStatus("Deleting thumbnails...");

			string[] files = Directory.GetFiles(this.ThumbsDirPath);
			int deleted = 0;
			int locked = 0;

			foreach (string file in files) {
				try {
					File.Delete(file);

					deleted++;
				} catch (Exception) {
					locked++;
				}
			}

			this.SetStatus(deleted + " thumbnails deleted (" + locked + " in use).");
		}

		/// <summary>
		/// Remove duplicates entries in the txt files.
		/// Also remove the tags if they are in the ignoreds list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_CleanLists_Click(object sender, RoutedEventArgs e)
		{
			this.LoadKnownTags();
			this.SetStatus("Cleaning known tag lists...");

			int unecessary = 0;

			unecessary += this.CleanKnownTagList(TXT_UNNAMESPACEDS, this.unnamespaceds);
			unecessary += this.CleanKnownTagList(TXT_SERIES, this.series);
			unecessary += this.CleanKnownTagList(TXT_CHARACTERS, this.characters);
			unecessary += this.CleanKnownTagList(TXT_CREATORS, this.creators);
			unecessary += this.CleanKnownTagList(TXT_IGNOREDS, this.ignoreds, false);

			this.LoadKnownTags();
			this.SetStatus(unecessary + " unecessary tags removed from the lists.");
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
		/// Called when clicking on the "Open app folder" menubar item, open the current working folder.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_OpenAppFolder_Click(object sender, RoutedEventArgs e)
		{
			this.StartProcess(App.appDir);
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
				if (this.IsCorrespondingToFilter(file, ImagesFilesExtensions)) {
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
			this.delay--;

			// The delay has reached the end, start the next search
			if (this.delay <= 0) {
				this.timer.Stop();
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

		#endregion Event
	}
}
