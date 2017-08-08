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

namespace Hatate
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const string DIR_THUMBS = @"thumbs\";
		const string DIR_NOT_FOUND = @"imgs\notfound\";
		const string DIR_TAGGED = @"imgs\tagged\";

		const string TXT_UNNAMESPACEDS = @"\tags\unnamespaceds.txt";
		const string TXT_SERIES = @"\tags\series.txt";
		const string TXT_CHARACTERS = @"\tags\characters.txt";
		const string TXT_CREATORS = @"\tags\creators.txt";
		const string TXT_IGNOREDS = @"\tags\ignoreds.txt";

		// Tags list
		private string[] unnamespaceds;
		private string[] series;
		private string[] characters;
		private string[] creators;
		private string[] ignoreds;

		private int lastSearchedInSeconds = 0;
		private int found = 0;
		private int notFound = 0;
		private int progress = 0;
		private bool running = false;

		public MainWindow()
		{
			InitializeComponent();

			if (Options.Default.KnownTags) {
				this.LoadKnownTags();
			}

			this.CreateFilesListContextMenu();
			this.CreateTagsListContextMenu();
			this.CreateUnknownTagsListContextMenu();
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

			foreach (string filename in dlg.FileNames) {
				this.ListBox_Files.Items.Add(filename);
			}

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

			this.ChangeStartButtonEnabledValue();
		}

		/// <summary>
		/// Load known tags from text files.
		/// </summary>
		private void LoadKnownTags()
		{
			if (File.Exists(App.appDir + TXT_UNNAMESPACEDS)) {
				this.unnamespaceds = File.ReadAllLines(App.appDir + TXT_UNNAMESPACEDS);
			}

			if (File.Exists(App.appDir + TXT_SERIES)) {
				this.series = File.ReadAllLines(App.appDir + TXT_SERIES);
			}

			if (File.Exists(App.appDir + TXT_CHARACTERS)) {
				this.characters = File.ReadAllLines(App.appDir + TXT_CHARACTERS);
			}

			if (File.Exists(App.appDir + TXT_CREATORS)) {
				this.creators = File.ReadAllLines(App.appDir + TXT_CREATORS);
			}

			if (File.Exists(App.appDir + TXT_IGNOREDS)) {
				this.ignoreds = File.ReadAllLines(App.appDir + TXT_IGNOREDS);
			}

			this.Label_Status.Content = "Tags loaded.";
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

			foreach (string file in files) {
				this.ListBox_Files.Items.Add(file);
			}

			int remaining = files.Length;
			this.Label_Status.Content = (remaining > 0 ? "Ready." : "No images found.");
			this.Button_Start.IsEnabled = (remaining > 0);

			this.UpdateLabels();
		}

		/// <summary>
		/// Generate a smaller image.
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="filename"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		private string GenerateThumbnail(string filepath, string filename, int width = 150)
		{
			string thumbsDir = this.ThumbsDirPath;
			string output = thumbsDir + filename;

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
		/// Start the search operations.
		/// </summary>
		private async void StartSearch()
		{
			this.progress = 0;

			if (this.ListBox_Files.Items.Count < 1) {
				return;
			}

			this.running = true;
			this.Button_Start.IsEnabled = false;

			IqdbApi.IqdbApi iqdbApi = new IqdbApi.IqdbApi();

			// Will run until the list is empty or every files in it has been searched
			// NOTE: this.progress is incremented each time the loop doesn't reach the end where it is reset to 0
			while (this.ListBox_Files.Items.Count > 0) {
				// No more files
				if (this.progress >= this.ListBox_Files.Items.Count) {
					break;
				}

				// Already searched
				if (this.HasResult(this.progress)) {
					this.progress++;

					continue;
				}

				string filepath = this.ListBox_Files.Items[this.progress].ToString();
				string filename = this.GetFilenameFromPath(filepath);

				// Skip file if a txt with the same name already exists
				if (File.Exists(filepath + ".txt")) {
					this.progress++;

					continue;
				}

				// Generate a smaller image for uploading
				this.Label_Status.Content = "Generating thumbnail...";
				string thumb = this.GenerateThumbnail(filepath, filename);

				// Search the image on IQDB
				this.Label_Status.Content = "Searching file on IQDB...";
				await this.RunIqdbApi(iqdbApi, this.progress, thumb, filename);

				// The search produced not result, move the image to the notfound folder and remove the row
				if (!this.HasResult(this.progress)) {
					this.MoveToNotFoundFolder(filepath, filename);
					this.notFound++;
					this.RemoveFileListItemAt(this.progress);
				} else {
					this.UpdateFileRowColor(this.progress, this.CountKnownTagsForItem(this.progress) > 0 ? Brushes.LimeGreen : Brushes.Orange);
					this.UpdateLabels();
				}

				// Wait some time until the next search
				if (this.progress < this.ListBox_Files.Items.Count - 1) {
					int delay = Options.Default.Delay;

					// If the delay is 60 seconds, this will randomly change to between 30 and 90 seconds
					if (Options.Default.Randomize) {
						int half = delay / 2;

						delay += new Random().Next(half*-1, half);
					}

					this.Label_Status.Content = "Next search in " + delay + " seconds";

					await Task.Delay(delay * 1000);
				}

				this.progress = 0;
			}

			this.Label_Status.Content = "Finished.";
			this.Button_Start.IsEnabled = false;
			this.running = false;
		}

		/// <summary>
		/// Count the number of known tags for a row by index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private int CountKnownTagsForItem(int index)
		{
			Result result = this.GetResultFromItem(this.progress);

			return result == null ? 0 : result.KnownTags.Count;
		}

		/// <summary>
		/// Remove a row from the Files list.
		/// </summary>
		private void RemoveFileListItem(object item)
		{
			this.ListBox_Files.Items.Remove(item);
		}

		/// <summary>
		/// Remove a row from the Files list by its index.
		/// </summary>
		private void RemoveFileListItemAt(int index)
		{
			this.ListBox_Files.Items.RemoveAt(index);
		}

		/// <summary>
		/// Update the color of a file row using its index.
		/// </summary>
		/// <param name="index"></param>
		private void UpdateFileRowColor(int index, Brush brush)
		{
			ListBoxItem lbItem = this.GetFilesListBoxItemByIndex(index);

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
			int remainMinutes = remainSeconds / 60;

			this.Label_Remaining.Content = "Remaining: " + remaining + " files (~ " + remainSeconds + " seconds / " + remainMinutes + " minutes)";
			this.Label_Results.Content = "Results: " + this.found + " found, " + this.notFound + " not";
		}

		/// <summary>
		/// Run the IQDB search.
		/// </summary>
		/// <param name="api"></param>
		/// <param name="thumbPath"></param>
		/// <param name="filename"></param>
		/// <returns></returns>
		private async Task RunIqdbApi(IqdbApi.IqdbApi api, int index, string thumbPath, string filename)
		{
			using (var fs = new FileStream(thumbPath, FileMode.Open)) {
				IqdbApi.Models.SearchResult result = null;

				try {
					result = await api.SearchFile(fs);
				} catch (FormatException) {
					// FormatException may happen in cas of an invalid HTML response where no tags can be parsed
				}

				// Result found
				if (result != null) {
					this.lastSearchedInSeconds = (int)result.SearchedInSeconds;

					// If found, move the image to the tagged folder
					if (this.CheckMatches(result.Matches, index, filename, thumbPath)) {
						this.found++;

						return;
					}
				}
			}
		}

		/// <summary>
		/// Check the various matches to find the best one.
		/// </summary>
		private bool CheckMatches(System.Collections.Immutable.ImmutableList<IqdbApi.Models.Match> matches, int index, string filename, string thumbPath)
		{
			foreach (IqdbApi.Models.Match match in matches) {
				// Check minimum similarity and number of tags
				if (match.Similarity < Options.Default.Similarity || match.Tags.Count < Options.Default.TagsCount) {
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

				Result result = this.FilterTags(match);
				result.ThumbPath = thumbPath;

				this.GetFilesListBoxItemByIndex(index).Tag = result;

				return true;
			}

			return false;
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
		/// Takes the tag list and keep only the ones that are valid and are present in the text files if enabled.
		/// </summary>
		/// <returns></returns>
		private Result FilterTags(IqdbApi.Models.Match match)
		{
			List<string> tagList = new List<string>();
			List<string> unknownTags = new List<string>();

			// Write each tags
			foreach (string tag in match.Tags) {
				// Format the tag
				string formated = tag;

				formated = formated.Replace("_", " ");
				formated = formated.Replace(",", "");
				formated = formated.ToLower().Trim();

				if (String.IsNullOrWhiteSpace(tag)) {
					continue;
				}

				string found = this.FindTag(formated);

				// Tag not found in the known tags
				if (found == null) {
					if (Options.Default.KnownTags && !this.ignoreds.Contains(tag)) {
						unknownTags.Add(formated);
					}

					continue;
				}

				tagList.Add(found);
			}

			// Add rating
			if (Options.Default.AddRating) {
				string strRating = match.Rating.ToString().ToLower();

				if (String.IsNullOrWhiteSpace(strRating)) {
					tagList.Add("rating:" + strRating);
				}
			}

			return new Result() {
				KnownTags   = tagList,
				UnknownTags = unknownTags,
				PreviewUrl  = "http://iqdb.org" + match.PreviewUrl,
				Source      = match.Source
			};
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
		/// Add a single tag into a text file.
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="tag"></param>
		private void AppendTagToTxt(string filepath, string tag)
		{
			using (StreamWriter file = new StreamWriter(filepath, true)) {
				file.WriteLine(tag);
			}
		}

		/// <summary>
		/// Find a tag in one of the known tags list.
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		private string FindTag(string tag)
		{
			if (!Options.Default.KnownTags) {
				return tag;
			}

			if (this.unnamespaceds != null && this.unnamespaceds.Contains(tag)) {
				return tag;
			} else if (this.series != null && this.series.Contains(tag)) {
				return "series:" + tag;
			} else if (this.characters != null && this.characters.Contains(tag)) {
				return "character:" + tag;
			} else if (this.creators != null && this.creators.Contains(tag)) {
				return "creator:" + tag;
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
			item.Header = "Set as not found";
			item.Tag = "notFound";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Remove";
			item.Tag = "removeFiles";
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

			this.ListBox_UnknownTags.ContextMenu = context;
		}

		/// <summary>
		/// Move all the files selected in the Files list to the "tagged" folder and write their tags.
		/// </summary>
		private void WriteTagsForSelectedItems()
		{
			while (this.ListBox_Files.SelectedItems.Count > 0) {
				var selected = this.ListBox_Files.SelectedItems[0];
				ListBoxItem item = this.ListBox_Files.ItemContainerGenerator.ContainerFromItem(selected) as ListBoxItem;
				Result result = (Result)item.Tag;

				// Remove the row
				this.RemoveFileListItem(selected);

				if (result == null) {
					continue;
				}

				string filepath = selected.ToString();
				string filename = this.GetFilenameFromPath(filepath);

				// Move the file to the tagged folder and write tags
				File.Move(filepath, this.TaggedDirPath + filename);
				this.WriteTagsToTxt(this.TaggedDirPath + filename + ".txt", result.KnownTags);
			}
		}

		/// <summary>
		/// Move all the files selected in the Files list to the "notfound" folder.
		/// </summary>
		/// <param name="filename"></param>
		private void MoveSelectedItemsToNotFoundFolder()
		{
			while (this.ListBox_Files.SelectedItems.Count > 0) {
				var selected = this.ListBox_Files.SelectedItems[0];
				ListBoxItem item = this.ListBox_Files.ItemContainerGenerator.ContainerFromItem(selected) as ListBoxItem;
				Result result = (Result)item.Tag;

				// Remove the row
				this.RemoveFileListItem(selected);

				if (result == null) {
					continue;
				}

				// Move the file to the tagged folder and write tags
				this.MoveToNotFoundFolder(selected.ToString(), this.GetFilenameFromPath(selected.ToString()));
			}
		}

		/// <summary>
		/// Move a file to the "notfound" folder using its filename.
		/// </summary>
		/// <param name="filepath"></param>
		private void MoveToNotFoundFolder(string filepath, string filename=null)
		{
			File.Move(filepath, this.NotfoundDirPath + filename);
		}

		/// <summary>
		/// Check if a row in the Files ListBox has an associated result.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private bool HasResult(int index)
		{
			return this.GetResultFromItem(index) != null;
		}

		/// <summary>
		/// Get a ListBoxItem from the Files ListBox using an index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private ListBoxItem GetFilesListBoxItemByIndex(int index)
		{
			if (this.ListBox_Files.Items.Count < 1) {
				return null;
			}

			return this.ListBox_Files.ItemContainerGenerator.ContainerFromItem(this.ListBox_Files.Items[index]) as ListBoxItem;
		}

		/// <summary>
		/// Get the result object attached to an item in the Files ListBox.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private Result GetResultFromItem(int index)
		{
			ListBoxItem listBoxItem = this.GetFilesListBoxItemByIndex(index);

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
		/// Get a color for a tag depending on its namespace
		/// </summary>
		/// <returns></returns>
		private Brush GetBrushFromTag(string tag)
		{
			if (tag.StartsWith("series:")) {
				return Brushes.DeepPink;
			}

			if (tag.StartsWith("character:")) {
				return Brushes.LimeGreen;
			}

			if (tag.StartsWith("creator:")) {
				return Brushes.Brown;
			}

			if (tag.StartsWith("rating:")) {
				return Brushes.LightSlateGray;
			}

			return Brushes.CadetBlue;
		}

		/// <summary>
		/// Move an item from a list to another one.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		private void MoveSelectedItemsToList(ListBox from, ListBox to, string prefix=null)
		{
			// Copy items to the destination list
			foreach (string item in from.SelectedItems) {
				ListBoxItem lbItem = new ListBoxItem();
				string content = prefix + item;

				lbItem.Content = content;
				lbItem.Foreground = this.GetBrushFromTag(content);

				to.Items.Add(lbItem);
			}

			// Remove from list
			this.RemoveSelectedItemsFromList(from);
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
		/// Write all the selected items from the given list into a txt file.
		/// </summary>
		private void WriteSelectedItemsToTxt(string filepath, ListBox from, string prefix=null)
		{
			using (StreamWriter file = new StreamWriter(filepath, true)) {
				foreach (var item in from.SelectedItems) {
					ListBoxItem listBoxItem = this.ListBox_Files.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;

					string tag = listBoxItem.Content.ToString();

					if (prefix != null) {
						tag = prefix + tag;
					}

					file.WriteLine(tag);
				}
			}
		}

		/// <summary>
		/// Add an unknown tags to the tag list.
		/// </summary>
		private void MoveSelectedUnknownTagsToKnownTags(string txt, string prefix=null)
		{
			if (this.ListBox_UnknownTags.Items.Count < 1) {
				return;
			}

			this.WriteSelectedItemsToTxt(App.appDir + txt, this.ListBox_UnknownTags);
			this.MoveSelectedItemsToList(this.ListBox_UnknownTags, this.ListBox_Tags, prefix);

			this.Button_Apply.IsEnabled = true;
		}

		/// <summary>
		/// Reset the elements in the right panel (source, preview, etc).
		/// </summary>
		private void ResetRightPanel()
		{
			this.Label_Match.Content = "Match";
			this.Label_UnknownTags.Content = "Unknown tags";
			this.Image_Original.Source = null;
			this.Image_Match.Source = null;
		}

		/// <summary>
		/// Rewrite tags in a txt file without duplicates.
		/// </summary>
		/// <param name="txt"></param>
		/// <param name="tags"></param>
		private void RemoveKnownTagsDuplicates(string txt, string[] tags)
		{
			if (!File.Exists(App.appDir + txt)) {
				return;
			}

			List<string> copies = new List<string>();

			foreach (string tag in tags) {
				if (!copies.Contains(tag)) {
					copies.Add(tag);
				}
			}

			this.WriteTagsToTxt(App.appDir + txt, copies, false);
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
				string path = App.appDir + @"\" + DIR_THUMBS;

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
				string path = App.appDir + DIR_TAGGED;

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
				string path = App.appDir + DIR_NOT_FOUND;

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
		/// Enable or disable the Start button depending if the list is empty or if the search loop is already running.
		/// </summary>
		private void ChangeStartButtonEnabledValue()
		{
			this.Button_Start.IsEnabled = this.ListBox_Files.Items.Count > 0 && !this.running;
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
			this.StartSearch();
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

			this.Button_Apply.IsEnabled = false;

			if (this.ListBox_Files.SelectedIndex < 0) {
				this.ResetRightPanel();

				return;
			}

			Result result = this.GetResultFromItem(this.ListBox_Files.SelectedIndex);

			if (result == null) {
				this.ResetRightPanel();

				return;
			}

			foreach (string tag in result.KnownTags) {
				this.ListBox_Tags.Items.Add(new ListBoxItem() {
					Content = tag,
					Foreground = this.GetBrushFromTag(tag)
				});
			}

			foreach (string tag in result.UnknownTags) {
				this.ListBox_UnknownTags.Items.Add(tag);
			}

			try {
				this.Image_Original.Source = new BitmapImage(new Uri(result.ThumbPath));
				this.Image_Match.Source = new BitmapImage(new Uri(result.PreviewUrl));
			} catch (UriFormatException) { }

			this.Label_Match.Content = result.Source.ToString();
			this.Label_UnknownTags.Content = "Unknown tags from " + result.Source.ToString();
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
					this.WriteTagsForSelectedItems();
				break;
				case "notFound":
					this.MoveSelectedItemsToNotFoundFolder();
				break;
				case "addUnnamespaced":
					this.MoveSelectedUnknownTagsToKnownTags(TXT_UNNAMESPACEDS);
				break;
				case "addSeries":
					this.MoveSelectedUnknownTagsToKnownTags(TXT_SERIES, "series:");
				break;
				case "addCharacter":
					this.MoveSelectedUnknownTagsToKnownTags(TXT_CHARACTERS, "character:");
				break;
				case "addCreator":
					this.MoveSelectedUnknownTagsToKnownTags(TXT_CREATORS, "creator:");
				break;
				case "addIgnored":
					this.WriteSelectedItemsToTxt(App.appDir + TXT_IGNOREDS, this.ListBox_UnknownTags);
					this.RemoveSelectedItemsFromList(this.ListBox_UnknownTags);
				break;
				case "removeFiles":
					this.RemoveSelectedItemsFromList(this.ListBox_Files);
				break;
				case "removeTags":
					this.RemoveSelectedItemsFromList(this.ListBox_Tags);
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
			//this.ListBox_Files.ContextMenu.Visibility = (this.ListBox_Files.Items.Count > 0 ? Visibility.Visible : Visibility.Hidden);

			bool hasResult = this.HasResult(this.ListBox_Files.SelectedIndex);

			((MenuItem)this.ListBox_Files.ContextMenu.Items[0]).IsEnabled = hasResult;
			((MenuItem)this.ListBox_Files.ContextMenu.Items[1]).IsEnabled = hasResult;
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
			Result result = this.GetResultFromItem(index);

			result.KnownTags.Clear();
			result.UnknownTags.Clear();

			foreach (var item in this.ListBox_Tags.Items) {
				result.KnownTags.Add(item.ToString().Replace("System.Windows.Controls.ListBoxItem: ", ""));
			}

			foreach (var item in this.ListBox_UnknownTags.Items) {
				result.UnknownTags.Add(item.ToString().Replace("System.Windows.Controls.ListBoxItem: ", ""));
			}

			// Reload known tags
			this.LoadKnownTags();
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

			foreach (string file in files) {
				try {
					File.Delete(file);
				} catch (Exception) { }
			}

			this.SetStatus("Thumbnails deleted.");
		}

		/// <summary>
		/// Remove duplicates entries in the txt files.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_RemoveDuplicates_Click(object sender, RoutedEventArgs e)
		{
			this.LoadKnownTags();
			this.SetStatus("Removing known tags duplicates...");

			this.RemoveKnownTagsDuplicates(TXT_UNNAMESPACEDS, this.unnamespaceds);
			this.RemoveKnownTagsDuplicates(TXT_SERIES, this.series);
			this.RemoveKnownTagsDuplicates(TXT_CHARACTERS, this.characters);
			this.RemoveKnownTagsDuplicates(TXT_CREATORS, this.creators);
			this.RemoveKnownTagsDuplicates(TXT_IGNOREDS, this.ignoreds);

			this.LoadKnownTags();
			this.SetStatus("Known tags duplicates removed.");
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
			Process myProcess = Process.Start(new ProcessStartInfo(App.appDir));
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

			// Add images to the list
			foreach (string file in files) {
				if (this.IsCorrespondingToFilter(file, ImagesFilesExtensions)) {
					this.ListBox_Files.Items.Add(file);
				}
			}

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

		#endregion Event
	}
}
