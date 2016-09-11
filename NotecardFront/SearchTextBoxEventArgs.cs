using NotecardLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardFront
{
	public delegate void SearchTextBoxEventHandler(System.Windows.Controls.UserControl sender, SearchTextBoxEventArgs e);

	public class SearchTextBoxEventArgs : EventArgs
	{
		public SearchResult[] Results { get; private set; }

		public SearchTextBoxEventArgs(SearchResult[] results)
		{
			this.Results = results;
		}
	}
}
