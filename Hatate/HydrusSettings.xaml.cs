using System.Windows;
using System.Net;
using System.IO;
using Hatate.Properties;
using Newtonsoft.Json.Linq;

namespace Hatate
{
	public partial class HydrusSettings : Window
	{
		public HydrusSettings()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.TextBox_ApiHost.Text = Settings.Default.HydrusApiHost;
			this.TextBox_ApiPort.Text = Settings.Default.HydrusApiPort;
			this.TextBox_ApiAccessKey.Text = Settings.Default.HydrusApiAccessKey;
			this.CheckBox_AutoSend.IsChecked = Settings.Default.AutoSend;
			this.CheckBox_DeleteImported.IsChecked = Settings.Default.DeleteImported;
			this.CheckBox_AssociateUrl.IsChecked = Settings.Default.AssociateUrl;
			this.CheckBox_SendUrlWithTags.IsChecked = Settings.Default.SendUrlWithTags;

			if (this.HasValidConnectionInfos()) {
				this.RetrieveTagServices();
			}
		}

		private async void RetrieveTagServices()
		{
			JArray tagServices = await App.hydrusApi.GetTagServices();

			if (tagServices == null) {
				return;
			}

			this.ComboBox_TagServices.Items.Clear();

			foreach (string tagService in tagServices) {
				this.ComboBox_TagServices.Items.Add(tagService);
			}

			this.Label_TagService.IsEnabled = true;
			this.ComboBox_TagServices.IsEnabled = true;
			this.ComboBox_TagServices.SelectedValue = Settings.Default.HydrusTagService;
			this.Button_Apply.IsEnabled = true;
		}

		private bool HasValidConnectionInfos()
		{
			if (string.IsNullOrWhiteSpace(this.TextBox_ApiHost.Text)) {
				return false;
			}

			if (string.IsNullOrWhiteSpace(this.TextBox_ApiPort.Text)) {
				return false;
			}

			if (string.IsNullOrWhiteSpace(this.TextBox_ApiAccessKey.Text)) {
				return false;
			}

			return true;
		}

		private void SetConnectionSettings()
		{
			Settings.Default.HydrusApiHost = this.TextBox_ApiHost.Text.Trim();
			Settings.Default.HydrusApiPort = this.TextBox_ApiPort.Text.Trim();
			Settings.Default.HydrusApiAccessKey = this.TextBox_ApiAccessKey.Text.Trim();
			Settings.Default.HydrusTagService = (string)this.ComboBox_TagServices.SelectedValue;
		}

		private void Button_Apply_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.TextBox_ApiHost.Text)) {
				MessageBox.Show("Missing host URL");
				this.Button_Apply.IsEnabled = false;
				return;
			}

			if (string.IsNullOrWhiteSpace(this.TextBox_ApiPort.Text)) {
				MessageBox.Show("Missing port number");
				this.Button_Apply.IsEnabled = false;
				return;
			}

			if (string.IsNullOrWhiteSpace(this.TextBox_ApiAccessKey.Text)) {
				MessageBox.Show("Missing access key");
				this.Button_Apply.IsEnabled = false;
				return;
			}

			if (this.ComboBox_TagServices.Items.Count > 0 && this.ComboBox_TagServices.SelectedItem == null) {
				MessageBox.Show("Please select one of the tag services");
				this.Button_Apply.IsEnabled = false;
				return;
			}

			this.SetConnectionSettings();

			Settings.Default.AutoSend = (bool)this.CheckBox_AutoSend.IsChecked;
			Settings.Default.DeleteImported = (bool)this.CheckBox_DeleteImported.IsChecked;
			Settings.Default.AssociateUrl = (bool)this.CheckBox_AssociateUrl.IsChecked;
			Settings.Default.SendUrlWithTags = (bool)this.CheckBox_SendUrlWithTags.IsChecked;

			Settings.Default.Save();

			this.Close();
		}

		private void Button_Test_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.TextBox_ApiHost.Text)) {
				MessageBox.Show("Missing host URL");
				this.Button_Apply.IsEnabled = false;
				return;
			}

			if (string.IsNullOrWhiteSpace(this.TextBox_ApiPort.Text)) {
				MessageBox.Show("Missing port number");
				this.Button_Apply.IsEnabled = false;
				return;
			}

			if (string.IsNullOrWhiteSpace(this.TextBox_ApiAccessKey.Text)) {
				MessageBox.Show("Missing access key");
				this.Button_Apply.IsEnabled = false;
				return;
			}

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.TextBox_ApiHost.Text + ':' + this.TextBox_ApiPort.Text + "/verify_access_key");

			request.Headers.Add("Hydrus-Client-API-Access-Key: " + this.TextBox_ApiAccessKey.Text);
			request.AutomaticDecompression = DecompressionMethods.GZip;

			try {
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				using (Stream stream = response.GetResponseStream())
				using (StreamReader reader = new StreamReader(stream)) {
					this.SetConnectionSettings();
					MessageBox.Show("Connection successful! Please select a tag service before saving.");
				}
			} catch (WebException exception) {
				App.hydrusApi.ShowApiUnreachableMessage(exception.Message);
				this.Button_Apply.IsEnabled = false;

				return;
			}

			this.RetrieveTagServices();
			this.Button_Apply.IsEnabled = true;
		}
	}
}
