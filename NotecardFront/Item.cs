using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardFront
{
	class Item
	{
		public string Text { get; set; }
		public string Value { get; set; }

		public Item(string text, string value)
		{
			this.Text = text;
			this.Value = value;
		}
	}
}
