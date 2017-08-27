using IComparable = System.IComparable;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Hatate
{
	public class Tag : IComparable
	{
		public Tag(string value, string nameSpace=null)
		{
			this.Value = value;
			this.Namespace = nameSpace;
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
			var item = obj as Tag;

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

		public string Value
		{
			get; set;
		}

		public string Namespace
		{
			get; set;
		}

		/// <summary>
		/// Return in format "namespace:value".
		/// </summary>
		public string Namespaced
		{
			get
			{
				if (string.IsNullOrEmpty(this.Namespace)) {
					return this.Value;
				}

				return this.Namespace + ":" + this.Value;
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
					case "rating": return Brushes.LightSlateGray;
					default: return Brushes.CadetBlue;
				}
			}
		}

		#endregion Accessor
	}
}
