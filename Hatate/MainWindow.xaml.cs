using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Directory = System.IO.Directory;
using Path = System.IO.Path;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Options = Hatate.Properties.Settings;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const int INTERVAL = 60000; // 60 seconds
		const string DIR_IMGS = @"imgs\";
		const string DIR_THUMBS = @"thumbs\";

		private string[] tags;
		private string[] series;
		private string[] characters;
		private string[] creators;

		private static string appFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
		private int lastSearchedInSeconds = 0;
		private string[] files;
		private int searched = 0;

		public MainWindow()
		{
			InitializeComponent();

			this.GetFileList();
			
			if (Options.Default.KnownTags) {
				this.GetKnownTags();
			}
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Load known tags from text files.
		/// </summary>
		private void GetKnownTags()
		{
			string tag = @"\tags\tags.txt";
			string serie = @"\tags\series.txt";
			string chara = @"\tags\characters.txt";
			string creator = @"\tags\creators.txt";

			if (File.Exists(tag)) {
				this.tags = File.ReadAllLines(appFolder + tag);
			}

			if (File.Exists(serie)) {
				this.series = File.ReadAllLines(appFolder + serie);
			}

			if (File.Exists(chara)) {
				this.characters = File.ReadAllLines(appFolder + chara);
			}

			if (File.Exists(creator)) {
				this.creators = File.ReadAllLines(appFolder + creator);
			}

			this.Label_Action.Content = "Tags loaded.";
		}

		/// <summary>
		/// Get the files, store them in the list and display them in the view.
		/// </summary>
		private void GetFileList()
		{
			string path = appFolder + @"\imgs";

			if (!Directory.Exists(path)) {
				Directory.CreateDirectory(path);
			}

			this.files = "*.jpg|*.jpeg|*.png".Split('|').SelectMany(filter => System.IO.Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly)).ToArray();
			this.searched = this.files.Length;

			this.ListBox_Files.Items.Clear();

			foreach (string file in this.files) {
				this.ListBox_Files.Items.Add(file);
			}

			this.Label_Action.Content = "Ready.";
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
			if (this.files.Length < 1) {
				return;
			}

			IqdbApi.IqdbApi api = new IqdbApi.IqdbApi();

			foreach (string filepath in this.files) {
				string filename = filepath.Substring(filepath.LastIndexOf(@"\") + 1, filepath.Length - filepath.LastIndexOf(@"\") - 1);

				// Skip file if a txt with the same name already exists
				if (File.Exists(filepath + ".txt")) {
					Console.WriteLine("Already searched: " + filepath + ".txt");
					continue;
				}

				// Generate a smaller image for uploading
				this.Label_Action.Content = "Generating thumbnail...";
				string thumb = this.GenerateThumbnail(filepath, filename);

				// Search the image on IQDB
				this.Label_Action.Content = "Searching file on IQDB...";
				await this.RunIqdbApi(api, thumb, filename);

				this.searched--;

				if (this.searched > 0) {
					this.Label_Action.Content = "Next search in " + (INTERVAL / 1000) + " seconds";
				} else {
					this.Label_Action.Content = "Finished.";

					// Ready for next batch
					this.GetFileList();
					this.Button_Start.IsEnabled = true;
				}

				this.UpdateLabels();

				// Wait some time until the next search
				await PutTaskDelay();
			}
		}

		/// <summary>
		/// Make an async task wait.
		/// </summary>
		/// <returns></returns>
		private async Task PutTaskDelay()
		{
			await Task.Delay(INTERVAL);
		}

		/// <summary>
		/// Update the labels with some useful informations.
		/// </summary>
		private void UpdateLabels()
		{
			int remainSeconds = (INTERVAL / 1000 + lastSearchedInSeconds) * this.searched;
			int remainMinutes = remainSeconds / 60;

			this.Label_Remaining.Content = this.searched + " files remaining, approximatly " + remainSeconds + " seconds (" + remainMinutes + " minutes) until completion";
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
			using (var fs = new System.IO.FileStream(thumbPath, System.IO.FileMode.Open)) {
				IqdbApi.Models.SearchResult result = await api.SearchFile(fs);

				this.lastSearchedInSeconds = (int)result.SearchedInSeconds;
				bool found = false;

				foreach (IqdbApi.Models.Match match in result.Matches) {
					// Check minimum similarity and number of tags
					if (match.Similarity < Options.Default.Similarity || match.Tags.Count < Options.Default.TagsCount) {
						continue;
					}

					// Check match type if enabled
					if (Options.Default.CheckMatchType && match.MatchType != (IqdbApi.Enums.MatchType)Options.Default.MatchType) {
						continue;
					}

					Console.WriteLine("| -------------------------------");
					Console.WriteLine("| Similarity: " + match.Similarity);
					Console.WriteLine("| Source: " + match.Source);
					Console.WriteLine("| Score: " + match.Score);
					Console.WriteLine("| MatchType: " + match.MatchType);
					Console.WriteLine("| PreviewUrl: " + match.PreviewUrl);
					Console.WriteLine("| Rating: " + match.Rating);
					Console.WriteLine("| Resolution: " + match.Resolution);
					Console.WriteLine("| Url: " + match.Url);
					Console.WriteLine("| -------------------------------");

					if (Options.Default.Compare) {
						Compare compare = new Compare(thumbPath, "http://iqdb.org" + match.PreviewUrl);

						if (!compare.IsGood) {
							continue;
						}
					}

					this.WriteTagsToTxt(filename, match.Tags);

					found = true;
				}

				if (found) {
					Console.WriteLine("Tags found for " + filename);

					File.Move(this.ImgsDirPath + filename, this.ImgsDirPath + @"tagged\" + filename);
				} else {
					Console.WriteLine("Nothing found for " + filename);

					File.Move(this.ImgsDirPath + filename, this.ImgsDirPath + @"notfound\" + filename);
				}
			}
		}

		/// <summary>
		/// Take the tag list and write it into a text file with the same name as the image.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="tags"></param>
		private void WriteTagsToTxt(string filename, System.Collections.Immutable.ImmutableList<string> tags)
		{
			string txtPath = this.ImgsDirPath + @"tagged\" + filename + ".txt";

			using (System.IO.StreamWriter file = new System.IO.StreamWriter(txtPath)) {
				foreach (string tag in tags) {
					string tmp = this.FormatTag(tag);

					if (tmp != null) {
						file.WriteLine(tmp);
					}
				}
			}
		}

		/// <summary>
		/// Remove unwanted characters and compare the tag with the known ones if enabled.
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		private string FormatTag(string tag)
		{
			tag = tag.Replace("_", " ");
			tag = tag.Replace(",", "");
			tag = tag.Trim();

			if (!Options.Default.KnownTags) {
				return tag;
			}

			if (this.tags.Contains(tag)) {
				return tag;
			} else if (this.series.Contains(tag)) {
				return "series:" + tag;
			} else if (this.characters.Contains(tag)) {
				return "characters:" + tag;
			} else if (this.creators.Contains(tag)) {
				return "creators:" + tag;
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
		/// Get the full path to the imgs folder.
		/// </summary>
		private string ImgsDirPath
		{
			get { return appFolder + @"\" + DIR_IMGS; }
		}

		/// <summary>
		/// Get the full path to the thumbs folder.
		/// </summary>
		private string ThumbsDirPath
		{
			get { return appFolder + @"\" + DIR_THUMBS; }
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
			this.Button_Start.IsEnabled = false;

			this.StartSearch();
		}

		/// <summary>
		/// Called when clicking on the menubar's refresh button, refresh the files list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_Refresh_Click(object sender, RoutedEventArgs e)
		{
			this.GetFileList();
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

		#endregion Event
	}
}
