using System.Windows.Controls;
using Brushes = System.Windows.Media.Brushes;
using Options = Hatate.Properties.Settings;

namespace Hatate.View.Window
{
    public partial class BetterImageRules : System.Windows.Window
    {
        private Image localImage = new Image();
        private Image remoteImage = new Image();

        public BetterImageRules(System.Windows.Window owner)
        {
            InitializeComponent();

            this.Owner = owner;

            this.AddPreferedImageFileFormatComboBoxItem(Enum.ImageFileFormat.PNG, "PNG");
            this.AddPreferedImageFileFormatComboBoxItem(Enum.ImageFileFormat.JPEG, "JPEG");

            this.ComboBox_LocalImageFormat.SelectedIndex = 0;
            this.ComboBox_RemoteImageFormat.SelectedIndex = 0;

            this.AddComparisonOperatorComboBoxItem(Enum.ComparisonOperator.LessThan, "less than");
            this.AddComparisonOperatorComboBoxItem(Enum.ComparisonOperator.GreaterThan, "greater than");
            this.AddComparisonOperatorComboBoxItem(Enum.ComparisonOperator.Equal, "equal to");
            this.AddComparisonOperatorComboBoxItem(Enum.ComparisonOperator.LessOrEqualThan, "less than or equal to");
            this.AddComparisonOperatorComboBoxItem(Enum.ComparisonOperator.GreaterOrEqualThan, "greater than or equal to");
        }

        /*
		Private
		*/

        private void AddPreferedImageFileFormatComboBoxItem(Enum.ImageFileFormat format, string label)
        {
            ComboBoxItem preferedItem = new ComboBoxItem();
            preferedItem.Content = label;
            preferedItem.Tag = format;

            ComboBoxItem localItem = new ComboBoxItem();
            localItem.Content = label;
            localItem.Tag = format;

            ComboBoxItem remoteItem = new ComboBoxItem();
            remoteItem.Content = label;
            remoteItem.Tag = format;

            this.ComboBox_PreferedFileFormat.Items.Add(preferedItem);
            this.ComboBox_LocalImageFormat.Items.Add(localItem);
            this.ComboBox_RemoteImageFormat.Items.Add(remoteItem);

            if (Options.Default.BetterImageRules_PreferedFileFormat == (byte)format) {
                this.ComboBox_PreferedFileFormat.SelectedItem = preferedItem;
			} else if (this.ComboBox_PreferedFileFormat.SelectedItem == null) {
                this.ComboBox_PreferedFileFormat.SelectedIndex = 0;
			}
		}

        private void AddComparisonOperatorComboBoxItem(Enum.ComparisonOperator op, string label)
        {
            ComboBoxItem widthItem = new ComboBoxItem();
            widthItem.Content = label;
            widthItem.Tag = op;

            ComboBoxItem heightItem = new ComboBoxItem();
            heightItem.Content = label;
            heightItem.Tag = op;

            ComboBoxItem sizeItem = new ComboBoxItem();
            sizeItem.Content = label;
            sizeItem.Tag = op;

            this.ComboBox_WidthOperator.Items.Add(widthItem);
            this.ComboBox_HeightOperator.Items.Add(heightItem);
            this.ComboBox_SizeOperator.Items.Add(sizeItem);

            if (Options.Default.BetterImageRules_WidthComparisonOperator == (byte)op) {
                this.ComboBox_WidthOperator.SelectedItem = widthItem;
			} else if (this.ComboBox_WidthOperator.SelectedItem == null) {
                this.ComboBox_WidthOperator.SelectedIndex = 0;
			}

            if (Options.Default.BetterImageRules_HeightComparisonOperator == (byte)op) {
                this.ComboBox_HeightOperator.SelectedItem = heightItem;
			} else if (this.ComboBox_HeightOperator.SelectedItem == null) {
                this.ComboBox_HeightOperator.SelectedIndex = 0;
			}

            if (Options.Default.BetterImageRules_SizeComparisonOperator == (byte)op) {
                this.ComboBox_SizeOperator.SelectedItem = sizeItem;
			} else if (this.ComboBox_SizeOperator.SelectedItem == null) {
                this.ComboBox_SizeOperator.SelectedIndex = 0;
			}
		}

        private void TestLocalImageAgainstRemoteImage()
        {
            // UI not fully loaded yet
            if (!this.IsLoaded) {
                return;
			}

            int localImageWidth = 0;
            int localImageHeight = 0;
            long localImageSize = 0;

            int remoteImageWidth = 0;
            int remoteImageHeight = 0;
            long remoteImageSize = 0;

            int.TryParse(this.TextBox_LocalImageWidth.Text, out localImageWidth);
            int.TryParse(this.TextBox_LocalImageHeight.Text, out localImageHeight);
            long.TryParse(this.TextBox_LocalImageSize.Text, out localImageSize);

            int.TryParse(this.TextBox_RemoteImageWidth.Text, out remoteImageWidth);
            int.TryParse(this.TextBox_RemoteImageHeight.Text, out remoteImageHeight);
            long.TryParse(this.TextBox_RemoteImageSize.Text, out remoteImageSize);

            this.localImage.Format = (Enum.ImageFileFormat)((ComboBoxItem)this.ComboBox_LocalImageFormat.SelectedItem).Tag;
            this.localImage.Width = localImageWidth;
            this.localImage.Height = localImageHeight;
            this.localImage.SizeInBytes = localImageSize;

            this.remoteImage.Format = (Enum.ImageFileFormat)((ComboBoxItem)this.ComboBox_RemoteImageFormat.SelectedItem).Tag;
            this.remoteImage.Width = remoteImageWidth;
            this.remoteImage.Height = remoteImageHeight;
            this.remoteImage.SizeInBytes = remoteImageSize;

            bool remoteIsBetter = this.remoteImage.IsBetterThan(
                this.localImage,
                (Enum.ImageFileFormat)((ComboBoxItem)this.ComboBox_PreferedFileFormat.SelectedItem).Tag,
                (Enum.ComparisonOperator)((ComboBoxItem)this.ComboBox_WidthOperator.SelectedItem).Tag,
                (Enum.ComparisonOperator)((ComboBoxItem)this.ComboBox_HeightOperator.SelectedItem).Tag,
                (Enum.ComparisonOperator)((ComboBoxItem)this.ComboBox_SizeOperator.SelectedItem).Tag
            );

            if (remoteIsBetter) {
                this.Label_TestResult.Content = "Remote image is better than the local image";
                this.Label_TestResult.Foreground = Brushes.Yellow;
			} else {
                this.Label_TestResult.Content = "Remote image is as good or worse than the local image";
                this.Label_TestResult.Foreground = Brushes.LimeGreen;
			}
		}

        /*
		Event
		*/

        /// <summary>
        /// Called by clicking on the "Save" button, saves the settings then closes the window.
        /// </summary>
		private void Button_Save_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            Options.Default.BetterImageRules_PreferedFileFormat = (byte)((ComboBoxItem)this.ComboBox_WidthOperator.SelectedItem).Tag;
            Options.Default.BetterImageRules_WidthComparisonOperator = (byte)((ComboBoxItem)this.ComboBox_WidthOperator.SelectedItem).Tag;
            Options.Default.BetterImageRules_HeightComparisonOperator = (byte)((ComboBoxItem)this.ComboBox_HeightOperator.SelectedItem).Tag;
            Options.Default.BetterImageRules_SizeComparisonOperator = (byte)((ComboBoxItem)this.ComboBox_SizeOperator.SelectedItem).Tag;

            if (!(bool)this.CheckBox_Format.IsChecked) {
                Options.Default.BetterImageRules_PreferedFileFormat = (byte)Enum.ImageFileFormat.Unknown;
			}

            if (!(bool)this.CheckBox_Width.IsChecked) {
                Options.Default.BetterImageRules_WidthComparisonOperator = (byte)Enum.ComparisonOperator.None;
			}

            if (!(bool)this.CheckBox_Height.IsChecked) {
                Options.Default.BetterImageRules_HeightComparisonOperator = (byte)Enum.ComparisonOperator.None;
			}

            if (!(bool)this.CheckBox_Size.IsChecked) {
                Options.Default.BetterImageRules_SizeComparisonOperator = (byte)Enum.ComparisonOperator.None;
			}

            Options.Default.Save();

            this.Close();
		}

		private void ComboBox_TestImageFormat_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
            this.TestLocalImageAgainstRemoteImage();
		}

		private void TextBox_TestImage_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
             this.TestLocalImageAgainstRemoteImage();
		}

		private void CheckBox_Test_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            this.TestLocalImageAgainstRemoteImage();
		}

		private void CheckBox_Format_StateChanged(object sender, System.Windows.RoutedEventArgs e)
		{
            this.ComboBox_LocalImageFormat.IsEnabled = (bool)this.CheckBox_Format.IsChecked;
            this.ComboBox_PreferedFileFormat.IsEnabled = (bool)this.CheckBox_Format.IsChecked;
            this.ComboBox_RemoteImageFormat.IsEnabled = (bool)this.CheckBox_Format.IsChecked;

            this.TestLocalImageAgainstRemoteImage();
		}

		private void CheckBox_Width_StateChanged(object sender, System.Windows.RoutedEventArgs e)
		{
            this.TextBox_LocalImageWidth.IsEnabled = (bool)this.CheckBox_Width.IsChecked;
            this.ComboBox_WidthOperator.IsEnabled = (bool)this.CheckBox_Width.IsChecked;
            this.TextBox_RemoteImageWidth.IsEnabled = (bool)this.CheckBox_Width.IsChecked;

            this.TestLocalImageAgainstRemoteImage();
		}

        private void CheckBox_Height_StateChanged(object sender, System.Windows.RoutedEventArgs e)
		{
            this.TextBox_LocalImageHeight.IsEnabled = (bool)this.CheckBox_Height.IsChecked;
            this.ComboBox_HeightOperator.IsEnabled = (bool)this.CheckBox_Height.IsChecked;
            this.TextBox_RemoteImageHeight.IsEnabled = (bool)this.CheckBox_Height.IsChecked;

            this.TestLocalImageAgainstRemoteImage();
		}

		private void CheckBox_Size_StateChanged(object sender, System.Windows.RoutedEventArgs e)
		{
            this.TextBox_LocalImageSize.IsEnabled = (bool)this.CheckBox_Size.IsChecked;
            this.ComboBox_SizeOperator.IsEnabled = (bool)this.CheckBox_Size.IsChecked;
            this.TextBox_RemoteImageSize.IsEnabled = (bool)this.CheckBox_Size.IsChecked;

            this.TestLocalImageAgainstRemoteImage();
		}

		private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
            this.ComboBox_PreferedFileFormat.SelectedItem = Options.Default.BetterImageRules_PreferedFileFormat;

            this.CheckBox_Format.IsChecked = (Options.Default.BetterImageRules_PreferedFileFormat != (byte)Enum.ImageFileFormat.Unknown);
            this.CheckBox_Width.IsChecked = (Options.Default.BetterImageRules_WidthComparisonOperator != (byte)Enum.ComparisonOperator.None);
            this.CheckBox_Height.IsChecked = (Options.Default.BetterImageRules_HeightComparisonOperator != (byte)Enum.ComparisonOperator.None);
            this.CheckBox_Size.IsChecked = (Options.Default.BetterImageRules_SizeComparisonOperator != (byte)Enum.ComparisonOperator.None);

            this.ComboBox_LocalImageFormat.IsEnabled = (bool)this.CheckBox_Format.IsChecked;
            this.ComboBox_PreferedFileFormat.IsEnabled = (bool)this.CheckBox_Format.IsChecked;
            this.ComboBox_RemoteImageFormat.IsEnabled = (bool)this.CheckBox_Format.IsChecked;

            this.TextBox_LocalImageWidth.IsEnabled = (bool)this.CheckBox_Width.IsChecked;
            this.ComboBox_WidthOperator.IsEnabled = (bool)this.CheckBox_Width.IsChecked;
            this.TextBox_RemoteImageWidth.IsEnabled = (bool)this.CheckBox_Width.IsChecked;

            this.TextBox_LocalImageHeight.IsEnabled = (bool)this.CheckBox_Height.IsChecked;
            this.ComboBox_HeightOperator.IsEnabled = (bool)this.CheckBox_Height.IsChecked;
            this.TextBox_RemoteImageHeight.IsEnabled = (bool)this.CheckBox_Height.IsChecked;

            this.TextBox_LocalImageSize.IsEnabled = (bool)this.CheckBox_Size.IsChecked;
            this.ComboBox_SizeOperator.IsEnabled = (bool)this.CheckBox_Size.IsChecked;
            this.TextBox_RemoteImageSize.IsEnabled = (bool)this.CheckBox_Size.IsChecked;

            this.TestLocalImageAgainstRemoteImage();
		}
	}
}
