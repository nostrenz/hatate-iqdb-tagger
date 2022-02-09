using System;
using System.Windows.Controls;

namespace Hatate.View.Control
{
	/// <summary>
	/// Interaction logic for TagNamespaceItem.xaml
	/// </summary>
	public partial class TagNamespaceItem : UserControl
	{
		public TagNamespaceItem(TagNamespace tagNamespace)
		{
			InitializeComponent();

			this.KeyName = tagNamespace.KeyName;
			this.Enabled = tagNamespace.Enabled;
			this.Namespace = tagNamespace.Namespace;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public string KeyName
		{
			get { return (string)this.Label_KeyName.Content; }
			set { this.Label_KeyName.Content = value; }
		}

		public bool Enabled
		{
			get { return (bool)this.Checkbox_Enabled.IsChecked; }
			set { this.Checkbox_Enabled.IsChecked = value; }
		}

		public string Namespace
		{
			get { return this.TextBox_Namespace.Text; }
			set { this.TextBox_Namespace.Text = value; }
		}

		/*
		============================================
		Event
		============================================
		*/

		private void Checkbox_Enabled_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			this.TextBox_Namespace.Opacity = (bool)this.Checkbox_Enabled.IsChecked ? 1.0 : 0.5;
		}
	}
}
