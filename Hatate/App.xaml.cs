using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static string appDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));

		public App() : base()
		{
			#if !DEBUG
			this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
			#endif
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
