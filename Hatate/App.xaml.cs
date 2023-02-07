using System;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Directory = System.IO.Directory;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public const ushort RELEASE_NUMBER = 13;

		public const string GITHUB_REPOSITORY     = "/nostrenz/hatate-iqdb-tagger";
		public const string GITHUB_LATEST_RELEASE = "/releases/latest";

		public static string appDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
		public static HydrusApi hydrusApi = new HydrusApi();
		public static Sources sources = new Sources();
		public static TagNamespaces.SauceNao tagNamespaces = new TagNamespaces.SauceNao();

		private const string DIR_THUMBS = @"\thumbs\";
		private const string DIR_TEMP = @"\temp\";

		public App() : base()
		{
			#if !DEBUG
			this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
			#endif

			App.sources.Init();
		}

		/*
		============================================
		Public
		============================================
		*/

		public static bool AskUser(string message)
		{
			return MessageBox.Show(message, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
		}

		/// <summary>
		/// Copy the selected item of a given listbox to the clipboard.
		/// </summary>
		/// <param name="from"></param>
		public static void CopySelectedTagsToClipboard(ListBox from)
		{
			string text = "";

			for (int i = 0; i < from.SelectedItems.Count; i++) {
				text += (from.SelectedItems[i] as Tag).Namespaced;

				if (i < from.SelectedItems.Count - 1) {
					text += "\n";
				}
			}

			Clipboard.SetText(text);
		}

		public static void PasteTags(TextBox textBox, ListBox listBox, DataObjectPastingEventArgs e)
		{
			if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true)) {
				return;
			}

			string text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
			string[] lines = text.Split('\n');

			if (lines.Length <= 1) {
				return;
			}

			// Add each line as a tag
			foreach (string line in lines) {
				Tag tag = new Tag(line.Trim(), true) { Source = Enum.TagSource.User };

				if (!listBox.Items.Contains(tag)) {
					listBox.Items.Add(tag);
				}
			}

			textBox.Clear();
		}

		/// <summary>
		/// Create a non-locked BitmapImage from a file path.
		/// </summary>
		/// <param name="filepath"></param>
		public static BitmapImage CreateBitmapImage(string filepath)
		{
			BitmapImage bitmap = new BitmapImage();

			try {
				// Specifying those options does not lock the file on disk (meaning it can be deleted or overwritten)
				bitmap.BeginInit();
				bitmap.UriSource = new Uri(filepath);
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.EndInit();
			} catch (IOException) {
				return null;
			} catch (NotSupportedException) {
				return null;
			}

			return bitmap;
		}

		/*
		============================================
		Private
		============================================
		*/

		private static string GetFolderPath(string relativePath)
		{
			string path = App.appDir + relativePath;

			App.CreateDirIfNeeded(path);

			return path;
		}

		/// <summary>
		/// Create a directory if it doesn't exists yet.
		/// </summary>
		/// <returns></returns>
		private static void CreateDirIfNeeded(string path)
		{
			if (!Directory.Exists(path)) {
				Directory.CreateDirectory(path);
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		/// <summary>
		/// Get the full path to the thumbs folder under the application directory.
		/// </summary>
		public static string ThumbsDirPath
		{
			get { return App.GetFolderPath(DIR_THUMBS); }
		}

		/// <summary>
		/// Get the full path to the temp folder under the application directory.
		/// </summary>
		public static string TempDirPath
		{
			get { return App.GetFolderPath(DIR_TEMP); }
		}

		/// <summary>
		/// Returns a filename usable for writing a temporary PNG file.
		/// </summary>
		/// <returns></returns>
		public static string TempPngFilePath
		{
			get { return App.TempDirPath + DateTime.Now.ToString("yyyymmddhhmmssffff") + ".png"; }
		}

		/// <summary>
		/// Returns the full URL to the Github repository.
		/// </summary>
		public static string RepositoryUrl
		{
			get { return "https://github.com" + GITHUB_REPOSITORY; }
		}

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// http://stackoverflow.com/questions/1472498/wpf-global-exception-handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			// Create the logs folder if necessary
			string path = appDir + @"\" + "logs";

			if (!Directory.Exists(path)) {
				Directory.CreateDirectory(path);
			}

			string timeStamp = System.DateTime.Now.ToString("yyyyMMddHHmmssffff");

			// Write a new log file
			using (StreamWriter file = new StreamWriter(path + @"\crash_" + timeStamp + ".txt")) {
				file.WriteLine(e.ToString());
				file.WriteLine(e.Exception.Message);
				file.WriteLine(e.Exception.Source);
				file.WriteLine(e.Exception.Data.ToString());
				file.WriteLine(e.Exception.ToString());
				file.WriteLine(e.Exception.StackTrace);
			}

			MessageBox.Show("Hatate just crashed into the ground.\nA report diary has been written in the logs folder.");

			this.Shutdown();

			// Prevent from having a Windows messagebox about the crash
			Process process = Process.GetCurrentProcess();
			process.Kill();
			process.Dispose();
		}

		#endregion Event
	}
}
