using System.Windows;
using System.Net;
using System.IO;
using System.Windows.Controls;
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

			App.hydrusApi.UpdateApiHostString();

			this.TextBox_ApiHost.Text = Settings.Default.HydrusApiHost;
			this.TextBox_ApiAccessKey.Text = Settings.Default.HydrusApiAccessKey;
			this.CheckBox_DeleteImported.IsChecked = Settings.Default.DeleteImported;
			this.CheckBox_AssociateUrl.IsChecked = Settings.Default.AssociateUrl;
			this.CheckBox_SendUrlWithTags.IsChecked = Settings.Default.SendUrlWithTags;
			this.CheckBox_AddImagesToHydrusPage.IsChecked = Settings.Default.AddImagesToHydrusPage;
			this.TextBox_HydrusPageName.Text = Settings.Default.HydrusPageName;
			this.CheckBox_FocusHydrusPage.IsChecked = Settings.Default.FocusHydrusPage;

			if (this.HasValidConnectionInfos()) {
				this.RetrieveTagServices();
			}

			this.AddAutoSendBehaviourComboBoxItem(Enum.HydrusAutoSendBehaviour.Never, "Never");
			this.AddAutoSendBehaviourComboBoxItem(Enum.HydrusAutoSendBehaviour.ImportLocal, "Import local image file (and its tags)");
			this.AddAutoSendBehaviourComboBoxItem(Enum.HydrusAutoSendBehaviour.ImportUrl, "Import URL (let Hydrus download the image and its tags)");
			this.AddAutoSendBehaviourComboBoxItem(Enum.HydrusAutoSendBehaviour.ImportUrlIfBetter, "Import URL if remote image is better, otherwise do nothing");
			this.AddAutoSendBehaviourComboBoxItem(Enum.HydrusAutoSendBehaviour.ImportUrlOrLocal, "Import URL if remote image is better, otherwise import local image file");
		}

		private void AddAutoSendBehaviourComboBoxItem(Enum.HydrusAutoSendBehaviour tag, string label)
		{
			ComboBoxItem item = new ComboBoxItem();

			item.Tag = tag;
			item.Content = label;

			this.ComboBox_AutoSendBehaviour.Items.Add(item);

			if (Settings.Default.Hydrus_AutoSendBehaviour == (byte)tag) {
				this.ComboBox_AutoSendBehaviour.SelectedItem = item;
			}
		}

		private async void RetrieveTagServices()
		{
			JObject services = await App.hydrusApi.GetServices();

			this.ComboBox_TagServices.Items.Clear();

			if (services == null) {
				return;
			}

			foreach (var item in services) {
				string key = (string)item.Key;

				// Not a tag service
				if (key != "local_tags" && key != "tag_repositories" && key != "all_known_tags") {
					continue;
				}

				JArray jArray = (JArray)item.Value;

				foreach (JObject service in jArray) {
					string tagServiceName = (string)service.GetValue("name");
					string tagServiceKey = (string)service.GetValue("service_key");

					ComboBoxItem comboBoxItem = new ComboBoxItem();
					comboBoxItem.Content = tagServiceName;
					comboBoxItem.Tag = tagServiceKey;

					this.ComboBox_TagServices.Items.Add(comboBoxItem);

					if (tagServiceName == Settings.Default.HydrusTagService || tagServiceKey == Settings.Default.HydrusTagServiceKey) {
						this.ComboBox_TagServices.SelectedItem = comboBoxItem;
					}
				}
			}

			this.Label_TagService.IsEnabled = true;
			this.ComboBox_TagServices.IsEnabled = true;
			this.Button_Apply.IsEnabled = true;
		}

		private bool HasValidConnectionInfos()
		{
			if (string.IsNullOrWhiteSpace(this.TextBox_ApiHost.Text)) {
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
			Settings.Default.HydrusApiAccessKey = this.TextBox_ApiAccessKey.Text.Trim();

			ComboBoxItem selectedTagService = this.ComboBox_TagServices.SelectedItem as ComboBoxItem;

			Settings.Default.HydrusTagService = selectedTagService == null ? null : (string)selectedTagService.Content;
			Settings.Default.HydrusTagServiceKey = selectedTagService == null ? null : (string)selectedTagService.Tag;
		}

		private void Button_Apply_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.TextBox_ApiHost.Text)) {
				MessageBox.Show("Missing URL");
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

			Settings.Default.DeleteImported = (bool)this.CheckBox_DeleteImported.IsChecked;
			Settings.Default.AssociateUrl = (bool)this.CheckBox_AssociateUrl.IsChecked;
			Settings.Default.SendUrlWithTags = (bool)this.CheckBox_SendUrlWithTags.IsChecked;
			Settings.Default.AddImagesToHydrusPage = (bool)this.CheckBox_AddImagesToHydrusPage.IsChecked;
			Settings.Default.HydrusPageName = this.TextBox_HydrusPageName.Text;
			Settings.Default.FocusHydrusPage = (bool)this.CheckBox_FocusHydrusPage.IsChecked;
			Settings.Default.Hydrus_AutoSendBehaviour = (byte)((ComboBoxItem)this.ComboBox_AutoSendBehaviour.SelectedItem).Tag;

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

			if (string.IsNullOrWhiteSpace(this.TextBox_ApiAccessKey.Text)) {
				MessageBox.Show("Missing access key");
				this.Button_Apply.IsEnabled = false;
				return;
			}

			HttpWebRequest request;

			try {
				request = (HttpWebRequest)WebRequest.Create(this.TextBox_ApiHost.Text + "/verify_access_key");
			} catch (System.UriFormatException) {
				MessageBox.Show("Connection failed");
				this.Button_Apply.IsEnabled = false;
				return;
			}

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
