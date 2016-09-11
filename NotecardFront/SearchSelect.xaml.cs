using NotecardLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NotecardFront
{
	/// <summary>
	/// Interaction logic for SearchSelect.xaml
	/// </summary>
	public partial class SearchSelect : UserControl
	{
		#region Members

		/// <summary>A comma-delimited list of card types to search. null to search all types.</summary>
		public string CardTypes
		{
			get { return txtSearch.CardTypes; }
			set { txtSearch.CardTypes = value; }
		}

		/// <summary>Gets or sets the cursor that displays when the mouse pointer is over this element.</summary>
		public new Cursor Cursor
		{
			get { return txtSearch.Cursor; }
			set { txtSearch.Cursor = value; }
		}

		/// <summary>Whether or not to show the text overlay.</summary>
		public bool ShowOverlay
		{
			get { return txtSearch.ShowOverlay; }
			set { txtSearch.ShowOverlay = value; }
		}

		/// <summary>The text block to display over the search box.</summary>
		public TextBlock lblOverlay
		{
			get { return txtSearch.lblOverlay; }
		}

		/// <summary>Fired when the user selects a card.</summary>
		public event SearchSelectEventHandler SelectionMade;

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the SearchSelect class.</summary>
		public SearchSelect()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region Events

		/// <summary>Clears the search box.</summary>
		public void clear()
		{
			txtSearch.clear();
		}

		/// <summary>Displays search results.</summary>
		private void txtSearch_SearchPerformed(UserControl sender, SearchTextBoxEventArgs e)
		{
			lstSearchResults.ItemsSource = e.Results;
			popSearchResults.IsOpen = e.Results.Length > 0;
		}

		/// <summary>Fires selection event.</summary>
		private void lstSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			popSearchResults.IsOpen = false;

			if (e.AddedItems.Count > 0)
			{
				string id = ((SearchResult)e.AddedItems[0]).ID;
				clear();
				SelectionMade?.Invoke(this, new SearchSelectEventArgs(id));
			}
		}

		#endregion Events
	}
}
