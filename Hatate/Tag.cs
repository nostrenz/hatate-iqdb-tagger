using IComparable = System.IComparable;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Hatate.Properties;

namespace Hatate
{
	public class Tag : IComparable
	{
		public Tag(string value, string nameSpace=null, bool exclude=false)
		{
			this.Value = value;
			this.Namespace = nameSpace;
			this.Exclude = exclude;
			this.Source = Enum.TagSource.Undefined;
		}

		public Tag(string namespaced, bool parseNamespace)
		{
			if (namespaced.StartsWith("-")) {
				namespaced = namespaced.Substring(1);
				this.Exclude = true;
			}

			this.Value = namespaced;
			this.Namespace = null;
			this.Source = Enum.TagSource.Undefined;

			if (!parseNamespace) {
				return;
			}

			int index = namespaced.IndexOf(':');

			if (index == -1) {
				return;
			}

			this.Namespace = namespaced.Substring(0, index);
			this.Value = namespaced.Substring(index + 1);
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// This method allow comparison functions to be used on a List<Tag> like Sort().
		/// Tag's value will be sorted alphabeticaly but namespaced tags will be put before the others.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>
		/// -1: this.Value will be placed before tag.Value (example: a / b)
		///  1: this.Value will be placed after tag.Value (example: b / a)
		/// </returns>
		int IComparable.CompareTo(object obj)
		{
			Tag tag = (Tag)obj;

			if (!string.IsNullOrEmpty(this.Namespace) || !string.IsNullOrEmpty(tag.Namespace)) {
				return string.Compare(tag.Namespace, this.Namespace);
			}

			return string.Compare(this.Value, tag.Value);
		}

		/// <summary>
		/// Allows to asset equality between two Tag objects.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			Tag item = obj as Tag;

			if (item == null) {
				return false;
			}

			return this.Namespaced.Equals(item.Namespaced);
		}

		/// <summary>
		/// Allows to asset equality between two Tag objects.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return this.Namespaced.GetHashCode();
		}

		#endregion Public

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public bool Exclude { get; set; }
		public string Value { get; set; }
		public string Namespace { get; set; }
		public Enum.TagSource Source { get; set; }

		/// <summary>
		/// A tag can be set as hidden when its Source doesn't match the ones selected in the GUI.
		/// Hidden tags won't show up in the GUI and won't be sent to Hydrus.
		/// </summary>
		public bool Hidden { get; set; }

		/// <summary>
		/// Return in format "namespace:value".
		/// </summary>
		public string Namespaced
		{
			get
			{
				if (string.IsNullOrEmpty(this.Namespace)) {
					return (this.Exclude ? "-" : "") + this.Value;
				}

				return (this.Exclude ? "-" : "") + this.Namespace + ":" + this.Value;
			}
		}

		/// <summary>
		/// Return in format "the_value".
		/// </summary>
		public string Underscored
		{
			get { return this.Value.Replace(" ", "_");  }
		}

		/// <summary>
		/// Return in format "the value".
		/// </summary>
		public string Whitespaced
		{
			get { return this.Value.Replace("_", " "); }
		}

		/// <summary>
		/// Foreground brush depending on the namespace.
		/// </summary>
		public Brush Foreground
		{
			get
			{
				switch (this.Namespace) {
					case "series": return Brushes.DeepPink;
					case "character": return Brushes.LimeGreen;
					case "creator": return Brushes.Brown;
					case "meta": return Brushes.DarkOrange;
					case "rating": return Brushes.LightSlateGray;
					default: {
						if (this.Namespace == null) {
							return Brushes.CadetBlue;
						}

						// Namespaced tag
						return new SolidColorBrush(Color.FromRgb(114, 160, 193));
					};
				}
			}
		}

		/// <summary>
		/// A hidden tag should not appear in the listbox.
		/// </summary>
		public System.Windows.Visibility Visibility
		{
			get { return this.Hidden ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible; }
		}

		public bool IsHitTestVisible
		{
			get { return !this.Hidden; }
		}

		public int Height
		{
			get { return this.Hidden ? 0 : 18; }
		}

		#endregion Accessor
	}
}
