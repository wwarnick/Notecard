using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardFront
{
	public delegate void SearchBoxEventHandler(System.Windows.Controls.UserControl sender, SearchBoxEventArgs e);

	public class SearchBoxEventArgs : EventArgs
	{
		public string SelectedValue { get; private set; }

		public SearchBoxEventArgs(string selectedValue)
		{
			this.SelectedValue = selectedValue;
		}
	}
}
