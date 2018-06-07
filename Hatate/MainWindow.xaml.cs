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
using System.Security.Cryptography;
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

		const string DIR_TAGS      = @"\tags\";
		const string DIR_THUMBS    = @"\thumbs\";
		const string DIR_IMGS      = @"\imgs\";
		const string DIR_NOT_FOUND = @"notfound\";
		const string DIR_TAGGED    = @"tagged\";

		const string TXT_IGNOREDS = "ignoreds.txt";

		// Tags list
		private List<string> ignoreds;

		private int lastSearchedInSeconds = 0;
		private int found = 0;
		private int notFound = 0;
		private int delay = 0;
		private Timer timer;

		// List of accepted image extentions
		private string[]  imagesFilesExtensions = new string[] { ".png", ".jpg", ".jpeg", ".bmp" };


		public MainWindow()
		{
			InitializeComponent();

			this.LoadIgnoredTags();

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
			string ignored = this.GetTxtPath(TXT_IGNOREDS);

			if (File.Exists(ignored)) {
				this.ignoreds = new List<string>(File.ReadAllLines(ignored));
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

			// It already exists
			if (File.Exists(output)) {
				return output;
			}

			Directory.CreateDirectory(thumbsDir);

			System.Drawing.Image image = null;

			try {
				image = System.Drawing.Image.FromFile(filepath);
			} catch (OutOfMemoryException) {
				// Cannot open file, we will upload the original file
				return filepath;
			}

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
			} catch (Exception e) { // Cannot save thumbnail, we will upload the original file
				output = filepath;
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

			await this.SearchFile(progress);

			// Refresh the items to update the foreground color from the Result objets
			this.ListBox_Files.Items.Refresh();

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
			Result result = this.GetResultAt(index);

			// Remove non existant file
			if (!File.Exists(result.ImagePath)) {
				this.ListBox_Files.Items.Remove(result);

				return;
			}

			// Generate a smaller image for uploading
			this.SetStatus("Generating thumbnail...");
			result.ThumbPath = this.GenerateThumbnail(result.ImagePath);

			// Search the image on IQDB
			this.SetStatus("Searching file on IQDB...");
			await this.RunIqdbApi(result);

			result.Searched = true;

			// We have tags
			if (result.Greenlight) {
				this.SetStatus("File found.");

				// Move or update the color
				if (Options.Default.AutoMove && result.HasTags) {
					this.WriteTagsForResult(result);
				}

				this.found++;
			} else { // No tags were found
				this.SetStatus("File not found.");

				// Move or update the color
				if (Options.Default.AutoMove) {
					this.MoveRowToNotFoundFolder(result);
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
			} catch (IOException e) {
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

				// Match found
				result.Source = match.Source;
				result.Rating = match.Rating;
				result.PreviewUrl = "http://iqdb.org" + match.PreviewUrl;
				result.Url = match.Url;

				bool success = this.ParseBooruPage(result);

				// Failed to parse the booru page 
				if (!success) {
					continue;
				}

				// Check ignored tags
				for (int i=result.Tags.Count-1; i>=0; i--) {
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
				&& !result.Tags.Exists(t => t.Namespace == "rating"
				)) {
					result.Tags.Add(new Tag(result.Rating.ToString().ToLower(), "rating"));
				}

				this.ListBox_Tags.Items.Refresh();
				this.ListBox_Ignoreds.Items.Refresh();

				return;
			}
		}

		/// <summary>
		/// Parse a booru page to obtain namespaced tags.
		/// </summary>
		private bool ParseBooruPage(Result result)
		{
			Parser.IParser booru = null;
			string urlPrefix = "https";

			switch (result.Source) {
				case IqdbApi.Enums.Source.Danbooru:
					booru = new Parser.Danbooru();
				break;
				case IqdbApi.Enums.Source.Gelbooru:
					booru = new Parser.Gelbooru();
				break;
				case IqdbApi.Enums.Source.Konachan:
					booru = new Parser.Konachan();
				break;
				case IqdbApi.Enums.Source.Yandere:
					booru = new Parser.Yandere();
				break;
				case IqdbApi.Enums.Source.SankakuChannel:
					booru = new Parser.SankakuChannel();
				break;
				case IqdbApi.Enums.Source.Eshuushuu:
					booru = new Parser.Eshuushuu();
					urlPrefix = "http";
				break;
				case IqdbApi.Enums.Source.TheAnimeGallery:
					booru = new Parser.TheAnimeGallery();
				break;
				case IqdbApi.Enums.Source.Zerochan:
					booru = new Parser.Zerochan();
				break;
				case IqdbApi.Enums.Source.AnimePictures:
					booru = new Parser.AnimePictures();
				break;
				default: return false;
			}

			bool success = booru.FromUrl(urlPrefix + ":" + result.Url);

			if (success) {
				result.Tags = booru.Tags;
			}

			return success;
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

			item = new MenuItem();
			item.Header = "Add tags";
			item.Tag = "addTagsForSelectedResults";
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
			item.Header = "Ignore";
			item.Tag = "ignore";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Add tags";
			item.Tag = "addTagsForSelectedResult";
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
		/// Get path to move a file to while taking into account the 260 chars limit.
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="folder"></param>
		/// <param name="filename"></param>
		/// <param name="reserveTxt">If true, will reserve 4 more chars into the path for adding ".txt" to it latter</param>
		/// <returns></returns>
		private string GetDestinationPath(string filepath, string folder, string filename, bool reserveTxt=false)
		{
			// We want to rename the file using the MD5 only when needed
			if (!Properties.Settings.Default.RenameMd5) {
				string destination = folder + filename;
				int length = destination.Length;

				if (reserveTxt) {
					length += 4;
				}

				// Keep the original file name only if not too long and not already existing
				if (length <= MAX_PATH_LENGTH && !File.Exists(destination)) {
					return destination;
				}
			}

			// Create a filename to have a path under 260 chars
			string md5 = this.CalculateMd5(filepath);
			string extension = this.Extension(filename);

			// Check if the MD5 allows to have a short enough path
			int maxMd5Length = MAX_PATH_LENGTH - (folder.Length + extension.Length);
			int md5Length = md5.Length;

			if (reserveTxt) {
				maxMd5Length -= 4;
			}

			// Shrink the MD5 for the path to not exceed 260 chars
			if (md5Length > maxMd5Length) {
				int exceed = md5Length - maxMd5Length;

				md5 = md5.Substring(exceed, md5Length - exceed);
			}

			return folder + md5 + extension;
		}

		/// <summary>
		/// Calculate a file's MD5 hash.
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		private string CalculateMd5(string filepath)
		{
			using (var md5 = MD5.Create()) {
				using (var stream = File.OpenRead(filepath)) {
					var hash = md5.ComputeHash(stream);

					return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}
			}
		}

		/// <summary>
		/// Move a given item's file to the tagged folder with the tags in a .txt file and remove the row.
		/// </summary>
		/// <param name="item"></param>
		private void WriteTagsForResult(Result result)
		{
			// No searched result, remove the item from the selection
			if (result == null || !result.Searched) {
				this.ListBox_Files.SelectedItems.Remove(result);

				return;
			}

			if (result.Tags.Count > 0 && File.Exists(result.ImagePath)) {
				// Move the file to the tagged folder and write tags
				string destination = this.MoveFile(result.ImagePath, this.TaggedDirPath, true);

				if (destination != null) {
					this.WriteTagsToTxt(destination + ".txt", result.Tags);
				}
			}

			// Write the ignored tags to txt
			if (result.Ignoreds.Count > 0) {
				this.WriteIgnoredsTags(result.Ignoreds);
			}

			// Remove the row
			this.ListBox_Files.Items.Remove(result);
		}


		/// <summary>
		/// Add the ignoreds tags from a result to the txt list.
		/// </summary>
		/// <param name="ignoredTags"></param>
		private void WriteIgnoredsTags(List<Tag> ignoredTags)
		{
			StreamWriter file = new StreamWriter(this.GetTxtPath(TXT_IGNOREDS), true);

			foreach (Tag tag in ignoredTags) {
				string namespaced = tag.Namespaced;

				// Tag isn't already in the txt list, add it
				if (!this.ignoreds.Contains(namespaced)) {
					this.ignoreds.Add(namespaced);
					file.WriteLine(namespaced);
				}
			}

			file.Close();
		}


		/// <summary>
		/// Move a files to the not "notfound" folder and remove the row.
		/// </summary>
		/// <param name="index"></param>
		private void MoveRowToNotFoundFolder(Result result)
		{
			if (!File.Exists(result.ImagePath)) {
				return;
			}

			this.MoveFile(result.ImagePath, this.NotfoundDirPath);
			this.ListBox_Files.Items.Remove(result);
		}

		/// <summary>
		/// Move a file into another directory and return its new filepath.
		/// </summary>
		/// <returns></returns>
		private string MoveFile(string filepath, string targetFolder, bool reserveTxt=false)
		{
			string destination = this.GetDestinationPath(filepath, targetFolder, this.GetFilenameFromPath(filepath), reserveTxt);

			try {
				File.Move(filepath, destination);
			} catch (Exception e) {
				MessageBox.Show("Unable to move file\n" + filepath + "\n\n" + e.Message);

				return null;
			}

			return destination;
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
			if (index < 0) {
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
		/// Extract the filename (eg. something.jpg) from a full path (eg. c:/somedir/something.jpg).
		/// </summary>
		private string GetFilenameFromPath(string filepath)
		{
			return filepath.Substring(filepath.LastIndexOf(@"\") + 1, filepath.Length - filepath.LastIndexOf(@"\") - 1);
		}

		/// <summary>
		/// Remove all the selected files from the Files listbox.
		/// </summary>
		private void RemoveSelectedFiles()
		{
			while (this.ListBox_Files.SelectedItems.Count > 0) {
				this.ListBox_Files.Items.Remove(this.ListBox_Files.SelectedItems[0]);
			}
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

		/// <summary>
		/// Remove result and background color of all the selected files.
		/// </summary>
		private void ResetSelectedFilesResult()
		{
			foreach (Result result in this.ListBox_Files.SelectedItems) {
				result.Searched = false;
				result.PreviewUrl = null;
				result.Url = null;
				result.Source = 0;
				result.Rating = 0;

				result.Tags.Clear();
				result.Ignoreds.Clear();
			}

			this.RefreshListboxes();
		}

		/// <summary>
		/// Refresh the tags and ignored listboxes to update their content from the selected result.
		/// </summary>
		private void RefreshListboxes()
		{
			this.ListBox_Tags.Items.Refresh();
			this.ListBox_Ignoreds.Items.Refresh();
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

			this.LoadIgnoredTags();
		}

		/// <summary>
		/// Rewrite tags in a txt file without duplicates.
		/// </summary>
		/// <param name="txt"></param>
		/// <param name="tags"></param>
		private int CleanIgnoredsTxt()
		{
			string path = this.GetTxtPath(TXT_IGNOREDS);

			if (!File.Exists(path)) {
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

			this.WriteTagsToTxt(path, copies, false);

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
			if (filepath.Length > MAX_PATH_LENGTH) {
				return;
			}

			string filename = this.GetFilenameFromPath(filepath);
			Result result = new Result(filepath);

			if (//this.ListBox_Files.Items.Contains(result)
			this.ListBox_Files.Items.Cast<Result>().Any(r => r.ImagePath == filepath)
			|| File.Exists(this.TaggedDirPath + filename)
			|| File.Exists(this.NotfoundDirPath + filename)) {
				return;
			}

			// Add tags to the result
			if (tags != null && tags.Count > 0) {
				foreach (Tag tag in tags) {
					result.Tags.Add(tag);
				}
			}

			this.ListBox_Files.Items.Add(result);
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
		private void IngnoreSelectItems()
		{
			Result result = this.SelectedResult;

			while (this.ListBox_Tags.SelectedItems.Count > 0) {
				Tag tag = (Tag)this.ListBox_Tags.SelectedItems[0];

				result.Tags.Remove(tag);
				result.Ignoreds.Add(tag);

				this.RefreshListboxes();
			}
		}

		/// <summary>
		/// Remove the selected ignoreds tags from the txt and from the list and move it to the tags list.
		/// </summary>
		/// <param name="from"></param>
		private void UningnoreSelectItems()
		{
			Result result = this.SelectedResult;

			while (this.ListBox_Ignoreds.SelectedItems.Count > 0) {
				Tag tag = (Tag)this.ListBox_Ignoreds.SelectedItems[0];

				this.ignoreds.Remove(tag.Namespaced);

				result.Ignoreds.Remove(tag);
				result.Tags.Add(tag);

				this.RefreshListboxes();
			}

			// Rewrite the ignoreds tags list since we removed some items from it
			this.WriteTagsToTxt(this.GetTxtPath(TXT_IGNOREDS), this.ignoreds, false);
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
		/// Same as IsTagInList() but useq a Tag object.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="list"></param>
		/// <returns></returns>
		private bool IsTagInIgnoreds(Tag tag)
		{
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

			this.ListBox_Tags.Items.Refresh();
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

			this.ListBox_Tags.Items.Refresh();
		}

		/// <summary>
		/// Add new tags into a result's tags list while preventing duplicates.
		/// </summary>
		private void AddTagsToResult(List<Tag> tags, Result result)
		{
			foreach (Tag tag in tags) {
				// Append the new tag to the list
				if (!result.Tags.Contains(tag)) {
					result.Tags.Add(tag);
				}
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
				NewTags newTags = new NewTags();
				tags = newTags.Tags;
			}

			return tags;
		}

		/// <summary>
		/// Move all the selected files to the "notfound" folder.
		/// </summary>
		private void MoveSelectedFilesToNotFound()
		{
			bool asked = false;

			while (this.ListBox_Files.SelectedItems.Count > 0) {
				Result result = this.GetSelectedResultAt(0);

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

				this.MoveRowToNotFoundFolder(result);
			}
		}

		/// <summary>
		/// Create a non-locked BitmapImage from a file path.
		/// </summary>
		/// <param name="filepath"></param>
		private BitmapImage CreateBitmapImage(string filepath)
		{
			BitmapImage bitmap = new BitmapImage();

			try {
				// Specifying those options does not lock the file on disk (meaning it can be deleted or overwritten)
				bitmap.BeginInit();
				bitmap.UriSource = new Uri(filepath);
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.EndInit();
			} catch (IOException e) {
				return null;
			}

			return bitmap;
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
		private void MenuItem_ReloadIgnoredTags_Click(object sender, RoutedEventArgs e)
		{
			this.LoadIgnoredTags();
		}

		/// <summary>
		/// Called when selecting a row in the files list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListBox_Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.Label_Match.Content = "Match";
			this.Image_Original.Source = null;
			this.Image_Match.Source = null;

			if (this.ListBox_Files.SelectedIndex < 0) {
				return;
			}

			Result result = this.SelectedResult;

			if (result == null) {
				return;
			}

			// Generate and set the thumbnail
			result.ThumbPath = this.GenerateThumbnail(result.ImagePath);
			this.Image_Original.Source = this.CreateBitmapImage(result.ThumbPath);

			// Set the image
			if (result.PreviewUrl != null) {
				try {
					this.Image_Match.Source = new BitmapImage(new Uri(result.PreviewUrl));
				} catch (Exception) {
					// UriFormatException may happen if the uri is incorrect
				}
			}

			// Add tags to the list
			result.Tags.Sort();
			result.Ignoreds.Sort();

			this.SetListBoxItemsSource(this.ListBox_Tags, result.Tags);
			this.SetListBoxItemsSource(this.ListBox_Ignoreds, result.Ignoreds);

			// The following need the result to be found
			if (!result.Greenlight) {
				return;
			}

			// Set the source name
			this.Label_Match.Content = result.Source.ToString();
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
				case "writeTags": {
					while (this.ListBox_Files.SelectedItems.Count > 0) {
						this.WriteTagsForResult(this.GetSelectedResultAt(0));
					}
				}
				break;
				case "notFound":
					this.MoveSelectedFilesToNotFound();
				break;
				case "unignore":
					this.UningnoreSelectItems();
				break;
				case "removeFiles":
					this.RemoveSelectedFiles();
				break;
				case "removeTags":
					this.RemoveSelectedTags();
				break;
				case "ignore":
					this.IngnoreSelectItems();
				break;
				case "copyFilePath":
					Clipboard.SetText(this.SelectedResult.ImagePath);
				break;
				case "launchFile":
					if (this.ListBox_Files.SelectedItem != null) {
						this.StartProcess(this.SelectedResult.ImagePath);
					}
				break;
				case "copyTag":
					this.CopySelectedTagToClipboard(this.ListBox_Tags);
				break;
				case "copyUnknownTag":
					this.CopySelectedTagToClipboard(this.ListBox_Ignoreds);
				break;
				case "helpTag":
					this.OpenHelpForSelectedTag(this.ListBox_Tags);
				break;
				case "helpUnknownTag":
					this.OpenHelpForSelectedTag(this.ListBox_Ignoreds);
				break;
				case "searchAgain":
					this.SearchFile(this.ListBox_Files.SelectedIndex);
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
			bool searched = true;

			if (singleSelected) {
				searched = this.SelectedResult.Searched;
			}

			this.SetContextMenuItemEnabled(this.ListBox_Files, 0, hasSelecteds && searched);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 1, hasSelecteds && searched);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 2, hasSelecteds);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 3, hasSelecteds);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 4, hasSelecteds);
															// 5 is a separator
			this.SetContextMenuItemEnabled(this.ListBox_Files, 6, singleSelected);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 7, singleSelected);
			this.SetContextMenuItemEnabled(this.ListBox_Files, 8, singleSelected);
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
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 2, this.ListBox_Files.SelectedItems.Count > 0); // "Add tags"
														   // 3 is a separator
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 4, singleSelected); // "Copy to clipboard"
			this.SetContextMenuItemEnabled(this.ListBox_Tags, 5, singleSelected); // "Search on Danbooru"
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
			this.SetContextMenuItemEnabled(this.ListBox_Ignoreds, 2, singleSelected); // "Copy to clipboard"
			this.SetContextMenuItemEnabled(this.ListBox_Ignoreds, 3, singleSelected); // "Search on Danbooru"
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
			this.LoadIgnoredTags();
			this.SetStatus("Cleaning known tag lists...");

			int unecessary = 0;

			unecessary += this.CleanIgnoredsTxt();

			this.LoadIgnoredTags();
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
		/// Called when clicking on the "Open folder... > Program" menubar item, open the folder where the program is located.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_OpenProgramFolder_Click(object sender, RoutedEventArgs e)
		{
			this.StartProcess(App.appDir);
		}

		/// <summary>
		/// Called when clicking on the "Open folder... > Tagged images" menubar item, open the "imgs/tagged" folder.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_OpenTaggedFolder(object sender, RoutedEventArgs e)
		{
			this.StartProcess(this.TaggedDirPath);
		}

		/// <summary>
		/// Called when clicking on the "Open folder... > Tagged images" menubar item, open the "imgs/notfound" folder.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_OpenNotFoundFolder(object sender, RoutedEventArgs e)
		{
			this.StartProcess(this.NotfoundDirPath);
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

		/// <summary>
		/// Called by clicking on the Original image, open the image in the default viewer.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Original_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
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

			if (result == null || String.IsNullOrEmpty(result.Url)) {
				return;
			}

			// All the known sources supports HTTPS except Eshuushuu
			bool supportsHttps = (result.Source != IqdbApi.Enums.Source.Eshuushuu);

			Process.Start("http" + (supportsHttps ? "s" : "") + ":" + result.Url);
		}

		#endregion Event
	}
}
