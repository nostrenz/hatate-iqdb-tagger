using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Directory = System.IO.Directory;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using Options = Hatate.Properties.Settings;

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
		private string[] files;
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
			using (FolderBrowserDialog fbd = new FolderBrowserDialog()) {
				DialogResult result = fbd.ShowDialog();

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
			this.files = "*.jpg|*.jpeg|*.png".Split('|').SelectMany(filter => System.IO.Directory.GetFiles(this.workingFolder, filter, SearchOption.TopDirectoryOnly)).ToArray();
			this.ListBox_Files.Items.Clear();

			foreach (string file in this.files) {
				this.ListBox_Files.Items.Add(file);
			}

			int remaining = this.files.Length;
			this.Label_Status.Content = (remaining > 0 ? "Ready." : "No images found.");
			this.Button_Start.IsEnabled = (remaining > 0);

			this.UpdateLabels(Options.Default.Delay);
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
			System.Drawing.Rectangle rectDestination = new System.Drawing.Rectangle(0, 0, width, thumbHeight);
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
			if (this.ListBox_Files.Items.Count < 1) {
				return;
			}

			this.MenuItem_OpenFolder.IsEnabled = false;
			this.Button_Start.IsEnabled = false;

			IqdbApi.IqdbApi api = new IqdbApi.IqdbApi();
			int count = this.files.Length;

			for (int i = 0; i < count; i++) {
				string filepath = this.files[i];
				string filename = filepath.Substring(filepath.LastIndexOf(@"\") + 1, filepath.Length - filepath.LastIndexOf(@"\") - 1);

				// Skip file if a txt with the same name already exists
				if (File.Exists(filepath + ".txt")) {
					continue;
				}

				// Generate a smaller image for uploading
				this.Label_Status.Content = "Generating thumbnail...";
				string thumb = this.GenerateThumbnail(filepath, filename);

				// Search the image on IQDB
				this.Label_Status.Content = "Searching file on IQDB...";
				await this.RunIqdbApi(api, thumb, filename);

				// Remove the file from list and delete the thumbnail 
				this.ListBox_Files.Items.Remove(filepath);
				File.Delete(thumb);

				int delay = Options.Default.Delay;

				// If the delay is 60 seconds, this will randomly change to between 30 and 90 seconds
				if (Options.Default.Randomize) {
					int half = delay / 2;

					delay += new Random().Next(half*-1, half);
				}

				this.UpdateLabels(delay);

				// Wait some time until the next search
				if (i < count - 1) {
					this.Label_Status.Content = "Next search in " + delay + " seconds";

					await Task.Delay(delay * 1000);
				}
			}

			this.Label_Status.Content = "Finished.";
			this.MenuItem_OpenFolder.IsEnabled = true;
			this.Button_Start.IsEnabled = true;
		}

		/// <summary>
		/// Update the labels with some useful informations.
		/// </summary>
		private void UpdateLabels(int lastDelay)
		{
			int remaining = this.ListBox_Files.Items.Count;
			int remainSeconds = (lastDelay + lastSearchedInSeconds) * remaining;
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
		private async Task RunIqdbApi(IqdbApi.IqdbApi api, string thumbPath, string filename)
		{
			using (var fs = new FileStream(thumbPath, FileMode.Open)) {
				IqdbApi.Models.SearchResult result = null;

				try {
					result = await api.SearchFile(fs);
				} catch (FormatException) {
					// FormatException may happen in cas of an invalid HTML response where no tags could be parsed
				}

				// Result found
				if (result != null) {
					this.lastSearchedInSeconds = (int)result.SearchedInSeconds;

					// If found, move the image to the tagged folder
					if (this.CheckMatches(result.Matches, filename, thumbPath)) {
						File.Move(this.workingFolder + filename, this.TaggedDirPath + filename);
						this.found++;

						return;
					}
				}

				// The search produced not result, move the image to the notfound folder
				File.Move(this.workingFolder + filename, this.NotfoundDirPath + filename);
				this.notFound++;
			}
		}

		/// <summary>
		/// Check the various matches to find the best one.
		/// </summary>
		private bool CheckMatches(System.Collections.Immutable.ImmutableList<IqdbApi.Models.Match> matches, string filename, string thumbPath)
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

				List<string> tagList = this.FilterTags(match);

				if (Options.Default.Compare) {
					Compare compare = new Compare(thumbPath, "http://iqdb.org" + match.PreviewUrl, tagList);

					if (!compare.IsGood) {
						continue;
					}
				}

				this.WriteTagsToTxt(this.TaggedDirPath + filename + ".txt", tagList);

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
		private List<string> FilterTags(IqdbApi.Models.Match match)
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

				if (found == null) {
					if (Options.Default.ShowUnknownTags) {
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

			// Check the unknown tags
			if (Options.Default.ShowUnknownTags && unknownTags.Count > 0) {
				UnknownTags ut = new UnknownTags(unknownTags, match.Source);
				ut.ShowDialog();

				// Add to the tag list
				ut.Unnamespaceds.ForEach(tag => tagList.Add(tag));
				ut.Series.ForEach(tag => tagList.Add("series:" + tag));
				ut.Characters.ForEach(tag => tagList.Add("character:" + tag));
				ut.Creators.ForEach(tag => tagList.Add("creator:" + tag));

				// Add to the text files
				this.WriteTagsToTxt(App.appDir + TXT_UNNAMESPACEDS, ut.Unnamespaceds, true);
				this.WriteTagsToTxt(App.appDir + TXT_SERIES, ut.Series, true);
				this.WriteTagsToTxt(App.appDir + TXT_CHARACTERS, ut.Characters, true);
				this.WriteTagsToTxt(App.appDir + TXT_CREATORS, ut.Creators, true);
			}

			return tagList;
		}

		/// <summary>
		/// Take the tag list and write it into a text file with the same name as the image.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="tags"></param>
		private void WriteTagsToTxt(string filename, List<string> tags, bool append=false)
		{
			using (StreamWriter file = new StreamWriter(filename, append)) {
				// Write each tags
				foreach (string tag in tags) {
					file.WriteLine(tag);
				}
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

		#endregion Event
	}
}
