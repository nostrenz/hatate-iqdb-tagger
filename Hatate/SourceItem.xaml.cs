using System;
using System.Windows.Controls;

namespace Hatate
{
	/// <summary>
	/// Interaction logic for SourceItem.xaml
	/// </summary>
	public partial class SourceItem : UserControl
	{
		public event EventHandler MoveUpRequested;
		public event EventHandler MoveDownRequested;

		public SourceItem(Source source)
		{
			InitializeComponent();

			this.Value = source.Value;
			this.Ordering = source.Ordering;
			this.Enabled = source.Enabled;
			this.GetTags = source.GetTags;

			switch (source.Value) {
				case Enum.Source.NicoNicoSeiga: this.Title = "Nico Nico Seiga"; break;
				case Enum.Source.Other: this.Title = "Other sources"; break;
				default: this.Title = source.Value.ToString(); break;
			}

			this.EnableOrDisable();
		}

		/*
		============================================
		Private
		============================================
		*/

		private void EnableOrDisable()
		{
			this.Opacity = this.Enabled ? 1.0 : 0.5;
			this.Checkbox_GetTags.IsHitTestVisible = this.Enabled;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public Enum.Source Value
		{
			get; set;
		}

		public byte Ordering
		{
			get; set;
		}

		public bool Enabled
		{
			get { return (bool)this.Checkbox_Enabled.IsChecked; }
			set { this.Checkbox_Enabled.IsChecked = value; }
		}

		public string Title
		{
			get { return this.Label_Title.Content.ToString(); }
			set { this.Label_Title.Content = value; }
		}

		public bool GetTags
		{
			get { return (bool)this.Checkbox_GetTags.IsChecked; }
			set { this.Checkbox_GetTags.IsChecked = value; }
		}

		/*
		============================================
		Event
		============================================
		*/

		private void Checkbox_Enabled_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			this.EnableOrDisable();
		}

		private void Button_Up_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			MoveUpRequested(this, e);
		}

		private void Button_Down_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			MoveDownRequested(this, e);
		}
	}
}
