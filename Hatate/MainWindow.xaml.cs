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

namespace Hatate
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const string DIR_THUMBS = @"thumbs\";
		const string TXT_UNNAMESPACEDS = @"\tags\unnamespaceds.txt";
		const string TXT_SERIES = @"\tags\series.txt";
		const string TXT_CHARACTERS = @"\tags\characters.txt";
		const string TXT_CREATORS = @"\tags\creators.txt";

		// Tags list
		private string[] unnamespaceds;
		private string[] series;
		private string[] characters;
		private string[] creators;

		private int lastSearchedInSeconds = 0;
		private int found = 0;
		private int notFound = 0;
		private string workingFolder = Options.Default.LastFolder;

		public MainWindow()
		{
			InitializeComponent();

			if (Options.Default.KnownTags) {
				this.LoadKnownTags();
			}

			if (!String.IsNullOrWhiteSpace(this.workingFolder)) {
				this.GetImagesFromFolder();
			}

			this.CreateFilesListContextMenu();
			this.CreateUnknownTagsListContextMenu();
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Select a folder to open.
		/// </summary>
		private void OpenFolder()
		{
			using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog()) {
				System.Windows.Forms.DialogResult result = fbd.ShowDialog();

				if (result != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath)) {
					return;
				}

				Options.Default.LastFolder = this.workingFolder = fbd.SelectedPath + @"\";

				Options.Default.Save();
			}
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

			this.Label_Status.Content = "Tags loaded.";
		}

		/// <summary>
		/// Get all the images in the working directory and add them to the list.
		/// </summary>
		private void GetImagesFromFolder()
		{
			string[] files = "*.jpg|*.jpeg|*.png".Split('|').SelectMany(filter => Directory.GetFiles(this.workingFolder, filter, SearchOption.TopDirectoryOnly)).ToArray();

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
			int count = this.ListBox_Files.Items.Count;

			if (count < 1) {
				return;
			}

			this.MenuItem_OpenFolder.IsEnabled = false;
			this.Button_Start.IsEnabled = false;

			IqdbApi.IqdbApi iqdbApi = new IqdbApi.IqdbApi();

			for (int i = 0; i < count; i++) {
				string filepath = this.ListBox_Files.Items[i].ToString();
				string filename = this.GetFilenameFromPath(filepath);

				// Skip file if a txt with the same name already exists
				if (File.Exists(filepath + ".txt")) {
					continue;
				}

				// Generate a smaller image for uploading
				this.Label_Status.Content = "Generating thumbnail...";
				string thumb = this.GenerateThumbnail(filepath, filename);

				// Search the image on IQDB
				this.Label_Status.Content = "Searching file on IQDB...";
				await this.RunIqdbApi(iqdbApi, i, thumb, filename);

				// The search produced not result, move the image to the notfound folder and remove the row
				if (!this.HasResult(i)) {
					this.MoveToNotFoundFolder(filename);
					this.ListBox_Files.Items.RemoveAt(i);
					this.notFound++;
				}

				this.UpdateFileRowColor(i, this.GetResultFromItem(i).KnownTags.Count > 0 ? Brushes.LimeGreen : Brushes.Orange);
				this.UpdateLabels();

				// Wait some time until the next search
				if (i < count - 1) {
					int delay = Options.Default.Delay;

					// If the delay is 60 seconds, this will randomly change to between 30 and 90 seconds
					if (Options.Default.Randomize) {
						int half = delay / 2;

						delay += new Random().Next(half*-1, half);
					}

					this.Label_Status.Content = "Next search in " + delay + " seconds";

					await Task.Delay(delay * 1000);
				}
			}

			this.Label_Status.Content = "Finished.";
			this.MenuItem_OpenFolder.IsEnabled = true;
			this.Button_Start.IsEnabled = true;
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
					if (Options.Default.KnownTags) {
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

			this.ListBox_Files.ContextMenu = context;
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

			this.ListBox_UnknownTags.ContextMenu = context;
		}

		/// <summary>
		/// Move all the files selected in the Files list to the "tagged" folder and write their tags.
		/// </summary>
		private void WriteTagsForSelectedItems()
		{
			for (int i = 0; i < this.ListBox_Files.Items.Count; i++) {
				int index = this.ListBox_Files.Items.IndexOf(this.ListBox_Files.Items[i]);
				Result result = this.GetResultFromItem(index);

				if (result == null) {
					continue;
				}

				string filename = this.GetFilenameFromPath(this.ListBox_Files.Items[i].ToString());

				// Move the file to the tagged folder and write tags
				File.Move(this.workingFolder + filename, this.TaggedDirPath + filename);
				this.WriteTagsToTxt(this.TaggedDirPath + filename + ".txt", result.KnownTags);

				// Remove the row
				this.ListBox_Files.Items.RemoveAt(index);
			}
		}

		/// <summary>
		/// Move all the files selected in the Files list to the "notfound" folder.
		/// </summary>
		/// <param name="filename"></param>
		private void MoveSelectedItemsToNotFoundFolder()
		{
			for (int i = 0; i < this.ListBox_Files.Items.Count; i++) {
				int index = this.ListBox_Files.Items.IndexOf(this.ListBox_Files.Items[i]);
				Result result = this.GetResultFromItem(index);

				if (result != null) {
					continue;
				}

				// Move the file to the tagged folder and write tags
				this.MoveToNotFoundFolder(this.GetFilenameFromPath(this.ListBox_Files.Items[i].ToString()));

				// Remove the row
				this.ListBox_Files.Items.RemoveAt(index);
			}
		}

		/// <summary>
		/// Move a file to the "notfound" folder using its filename.
		/// </summary>
		/// <param name="filename"></param>
		private void MoveToNotFoundFolder(string filename)
		{
			File.Move(this.workingFolder + filename, this.NotfoundDirPath + filename);
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
			return this.ListBox_Files.ItemContainerGenerator.ContainerFromItem(this.ListBox_Files.Items[index]) as ListBoxItem;
		}

		/// <summary>
		/// Get the result object attached to an item in the Files ListBox.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private Result GetResultFromItem(int index)
		{
			return (Result)this.GetFilesListBoxItemByIndex(index).Tag;
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
		private void MoveToList(ListBox from, ListBox to, string prefix=null)
		{
			foreach (string item in from.SelectedItems) {
				ListBoxItem lbItem = new ListBoxItem();
				string content = prefix + item;

				lbItem.Content = content;
				lbItem.Foreground = this.GetBrushFromTag(content);

				to.Items.Add(lbItem);
			}

			while (from.SelectedItems.Count > 0) {
				from.Items.Remove(from.SelectedItems[0]);
			}
		}

		/// <summary>
		/// Add an unknown tags to the tag list.
		/// </summary>
		private void AddNewTag(string txt, string prefix=null)
		{
			this.AppendTagToTxt(App.appDir + txt, this.ListBox_UnknownTags.SelectedItem.ToString());
			this.MoveToList(this.ListBox_UnknownTags, this.ListBox_Tags, prefix);
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

				if (!Directory.Exists(path)) {
					Directory.CreateDirectory(path);
				}

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
				string path = this.workingFolder + @"tagged\";

				if (!Directory.Exists(path)) {
					Directory.CreateDirectory(path);
				}

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
				string path = this.workingFolder + @"notfound\";

				if (!Directory.Exists(path)) {
					Directory.CreateDirectory(path);
				}

				return path;
			}
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
		/// Called when clicking on the menubar's refresh button, refresh the files list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_Refresh_Click(object sender, RoutedEventArgs e)
		{
			this.GetImagesFromFolder();
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
		/// Called when clicking on the menubar's Open folder button, open a new folder to load images from.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_OpenFolder_Click(object sender, RoutedEventArgs e)
		{
			this.OpenFolder();

			if (this.workingFolder != null) {
				this.GetImagesFromFolder();
			}
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
					this.AddNewTag(TXT_UNNAMESPACEDS);
				break;
				case "addSeries":
					this.AddNewTag(TXT_SERIES, "series:");
				break;
				case "addCharacter":
					this.AddNewTag(TXT_CHARACTERS, "character:");
				break;
				case "addCreator":
					this.AddNewTag(TXT_CREATORS, "creator:");
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
			bool hasResult = this.HasResult(this.ListBox_Files.SelectedIndex);

			this.ListBox_Files.ContextMenu.Visibility = (hasResult ? Visibility.Visible : Visibility.Hidden);
			((MenuItem)this.ListBox_Files.ContextMenu.Items[0]).IsEnabled = ((MenuItem)this.ListBox_Files.ContextMenu.Items[1]).IsEnabled = hasResult;
		}

		/// <summary>
		/// Called when clicking on the "ShowFolder" menubar item, open the current working folder.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_ShowFolder_Click(object sender, RoutedEventArgs e)
		{
			if (Directory.Exists(this.workingFolder)) {
				Process myProcess = Process.Start(new ProcessStartInfo(this.workingFolder));
			}
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
			string[] files = Directory.GetFiles(this.ThumbsDirPath);

			foreach (string file in files) {
				try {
					File.Delete(file);
				} catch (Exception) { }
			}
		}

		#endregion Event
	}
}
