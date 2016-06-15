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
}
