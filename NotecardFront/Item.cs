using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardFront
{
	class Item<T>
	{
		public string Text { get; set; }
		public T Value { get; set; }

		public Item(string text, T value)
		{
			this.Text = text;
			this.Value = value;
		}
	}

	class Item<T, T2>
	{
		public string Text { get; set; }
		public T Value { get; set; }
		public T2 Tag { get; set; }

		public Item(string text, T value, T2 tag)
		{
			this.Text = text;
			this.Value = value;
			this.Tag = tag;
		}
	}
}
