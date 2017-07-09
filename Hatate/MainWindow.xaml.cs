using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IqdbApi;
using Directory = System.IO.Directory;
using Path = System.IO.Path;
using System.Threading;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

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
			this.GetKnownTags();
		}

		private void GetKnownTags()
		{
			this.tags = File.ReadAllLines(appFolder + @"\tags\tags.txt");
			this.series = File.ReadAllLines(appFolder + @"\tags\series.txt");
			this.characters = File.ReadAllLines(appFolder + @"\tags\characters.txt");
			this.creators = File.ReadAllLines(appFolder + @"\tags\creators.txt");

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

		private string GenerateThumbnail(string filepath, string filename, int width = 150)
		{
			string thumbsDir = this.GetThumbsDirPath();
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

		private string GetImgsDirPath()
		{
			return appFolder + @"\" + DIR_IMGS;
		}

		private string GetThumbsDirPath()
		{
			return appFolder + @"\" + DIR_THUMBS;
		}

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

				this.Label_Action.Content = "Generating thumbnail...";
				string thumb = this.GenerateThumbnail(filepath, filename);

				this.Label_Action.Content = "Searching file on IQDB...";
				await this.Run(api, thumb, filename);

				this.searched--;

				if (this.searched > 0) {
					this.Label_Action.Content = "Next search in " + (INTERVAL/1000) + " seconds";
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

		async Task PutTaskDelay()
		{
			await Task.Delay(INTERVAL);
		}

		private void UpdateLabels()
		{
			int remainSeconds = (INTERVAL/1000 + lastSearchedInSeconds) * this.searched;
			int remainMinutes = remainSeconds / 60;

			this.Label_Remaining.Content = this.searched + " files remaining, approximatly " + remainSeconds + " seconds (" + remainMinutes + " minutes) until completion";
		}

		private async Task Run(IqdbApi.IqdbApi api, string thumbPath, string filename)
		{
			using (var fs = new System.IO.FileStream(thumbPath, System.IO.FileMode.Open)) {
				IqdbApi.Models.SearchResult result = await api.SearchFile(fs);

				this.lastSearchedInSeconds = (int)result.SearchedInSeconds;
				bool found = false;

				foreach (IqdbApi.Models.Match match in result.Matches) {
					if (match.Similarity < 90 || match.MatchType != IqdbApi.Enums.MatchType.Best || match.Tags.Count == 0) {
						continue;
					}

					Console.WriteLine("| -------------------------------");
					Console.WriteLine("| Similarity: " + match.Similarity);
					Console.WriteLine("| Source: "     + match.Source);
					Console.WriteLine("| Score: "      + match.Score);
					Console.WriteLine("| MatchType: "  + match.MatchType);
					Console.WriteLine("| PreviewUrl: " + match.PreviewUrl);
					Console.WriteLine("| Rating: "     + match.Rating);
					Console.WriteLine("| Resolution: " + match.Resolution);
					Console.WriteLine("| Url: "        + match.Url);
					Console.WriteLine("| -------------------------------");

					if (Properties.Settings.Default.Compare) {
						Compare compare = new Compare(thumbPath, "http://iqdb.org" + match.PreviewUrl);

						if (!compare.IsGood()) {
							continue;
						}
					}

					this.WriteTagsToTxt(filename, match.Tags);

					found = true;
				}

				if (found) {
					Console.WriteLine("Tags found for " + filename);

					File.Move(this.GetImgsDirPath() + filename, this.GetImgsDirPath() + @"tagged\" + filename);
				} else {
					Console.WriteLine("Nothing found for " + filename);

					File.Move(this.GetImgsDirPath() + filename, this.GetImgsDirPath() + @"notfound\" + filename);
				}
			}
		}

		private void WriteTagsToTxt(string filename, System.Collections.Immutable.ImmutableList<string> tags)
		{
			string txtPath = this.GetImgsDirPath() + @"tagged\" + filename + ".txt";

			using (System.IO.StreamWriter file = new System.IO.StreamWriter(txtPath)) {
				foreach (string tag in tags) {
					string tmp = this.formatTag(tag);

					if (tmp != null) {
						file.WriteLine(tmp);
					}
				}
			}
		}

		private string formatTag(string tag)
		{
			tag = tag.Replace("_", " ");
			tag = tag.Replace(",", "");
			tag = tag.Trim();

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

		private void Button_Start_Click(object sender, RoutedEventArgs e)
		{
			this.Button_Start.IsEnabled = false;

			this.StartSearch();
		}

		private void MenuItem_Refresh_Click(object sender, RoutedEventArgs e)
		{
			this.GetFileList();
		}

		private void MenuItem_Options_Click(object sender, RoutedEventArgs e)
		{
			Option potion = new Option();
		}
	}
}
