namespace Hatate
{
	public class Source
	{
		public Source()
		{
		}

		public Source(SourceItem sourceItem)
		{
			this.Value = sourceItem.Value;
			this.Ordering = sourceItem.Ordering;
			this.Enabled = sourceItem.Enabled;
			this.GetTags = sourceItem.GetTags;
		}

		public Source(Enum.Source source, sbyte ordering)
		{
			this.Value = source;
			this.Ordering = (byte)System.Math.Abs(ordering);
			this.Enabled = ordering >= 0;
			this.GetTags = true;
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
			get; set;
		}

		public bool GetTags
		{
			get; set;
		}
	}
}
