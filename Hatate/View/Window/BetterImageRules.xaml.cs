using Options = Hatate.Properties.Settings;

namespace Hatate.View.Window
{
    public partial class BetterImageRules : System.Windows.Window
    {
        public BetterImageRules(System.Windows.Window owner)
        {
            InitializeComponent();

            this.Owner = owner;

            this.ComboBox_PreferedFileFormat.SelectedItem = Options.Default.BetterImageRules_PreferedFileFormat;
            this.CheckBox_RemoteImageWidthShouldBeGreater.IsChecked = Options.Default.BetterImageRules_RemoteImageWidthShouldBeGreater;
            this.CheckBox_RemoteImageHeightShouldBeGreater.IsChecked = Options.Default.BetterImageRules_RemoteImageHeightShouldBeGreater;
            this.CheckBox_RemoteImageSizeShouldBeGreater.IsChecked = Options.Default.BetterImageRules_RemoteImageSizeShouldBeGreater;
        }

        /*
		============================================
		Event
		============================================
		*/

		private void Button_Save_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            Options.Default.BetterImageRules_PreferedFileFormat = (string)this.ComboBox_PreferedFileFormat.SelectedItem;
            Options.Default.BetterImageRules_RemoteImageWidthShouldBeGreater = (bool)this.CheckBox_RemoteImageWidthShouldBeGreater.IsChecked;
            Options.Default.BetterImageRules_RemoteImageHeightShouldBeGreater = (bool)this.CheckBox_RemoteImageHeightShouldBeGreater.IsChecked;
            Options.Default.BetterImageRules_RemoteImageSizeShouldBeGreater = (bool)this.CheckBox_RemoteImageSizeShouldBeGreater.IsChecked;

            Options.Default.Save();

            this.Close();
		}
	}
}
