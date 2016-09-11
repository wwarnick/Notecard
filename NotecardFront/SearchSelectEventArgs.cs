using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardFront
{
	public delegate void SearchSelectEventHandler(System.Windows.Controls.UserControl sender, SearchSelectEventArgs e);

	public class SearchSelectEventArgs : EventArgs
	{
		public string SelectedValue { get; private set; }

		public SearchSelectEventArgs(string selectedValue)
		{
			this.SelectedValue = selectedValue;
		}
	}
}
