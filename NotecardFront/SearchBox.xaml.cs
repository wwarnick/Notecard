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
	/// Interaction logic for SearchBox.xaml
	/// </summary>
	public partial class SearchBox : UserControl
	{
		#region Members

		/// <summary>The path of the current database.</summary>
		public string Path { get; set; }

		/// <summary>A comma-delimited list of card types to search. null to search all types.</summary>
		public string CardTypes { get; set; }

		/// <summary>Gets or sets the cursor that displays when the mouse pointer is over this element.</summary>
		public new Cursor Cursor
		{
			get { return txtSearch.Cursor; }
			set { txtSearch.Cursor = value; }
		}

		/// <summary>Whether or not to show the text overlay.</summary>
		public bool ShowOverlay
		{
			get { return lblOverlay != null; }
			set
			{
				if (value && lblOverlay == null)
				{
					// TODO: THIS!
					lblOverlay = new TextBlock()
					{
						IsHitTestVisible = false,
						Margin = new Thickness(5d, 0d, 0d, 0d),
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Left,
						Foreground = Brushes.Gray,
						Text = "Search..."
					};
					grdMain.Children.Add(lblOverlay);

					lblOverlay.Visibility = string.IsNullOrEmpty(txtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;

					txtSearch.GotFocus += txtSearch_GotFocus;
					txtSearch.LostFocus += txtSearch_LostFocus;
				}
				else if (!value && lblOverlay != null)
				{
					grdMain.Children.Remove(lblOverlay);
					lblOverlay = null;
					txtSearch.GotFocus -= txtSearch_GotFocus;
					txtSearch.LostFocus -= txtSearch_LostFocus;
				}
			}
		}

		private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
		{
			lblOverlay.Visibility = string.IsNullOrEmpty(txtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
		}

		private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
		{
			lblOverlay.Visibility = Visibility.Collapsed;
		}

		/// <summary>The text block to display over the search box.</summary>
		public TextBlock lblOverlay { get; private set; }

		/// <summary>Fired when the user selects a card.</summary>
		public event SearchBoxEventHandler SelectionMade;

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the SearchBox class.</summary>
		public SearchBox()
		{
			InitializeComponent();

			Path = null;
			CardTypes = null;
		}

		#endregion Constructors

		#region Events

		/// <summary>Clears the search box.</summary>
		public void clear()
		{
			txtSearch.Text = string.Empty;
		}

		/// <summary>Performs search and displays results.</summary>
		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			string userMessage = string.Empty;

			List<string> ids = null;

			if (!string.IsNullOrEmpty(txtSearch.Text))
				ids = CardManager.search(txtSearch.Text, CardTypes, Path, ref userMessage);

			if (ids == null || ids.Count == 0)
			{
				lstSearchResults.ItemsSource = new Item<string>[0];
				popSearchResults.IsOpen = false;
			}
			else
			{
				List<string[]> names = CardManager.getCardNames(ids, string.IsNullOrEmpty(CardTypes), Path, ref userMessage);

				Item<string>[] items = new Item<string>[names.Count];

				for (int i = 0; i < names.Count; i++)
				{
					items[i] = new Item<string>(names[i][1], names[i][0]);
				}

				lstSearchResults.ItemsSource = items;
				popSearchResults.IsOpen = true;
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Fires selection event.</summary>
		private void lstSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			popSearchResults.IsOpen = false;

			if (e.AddedItems.Count > 0)
			{
				string id = ((Item<string>)e.AddedItems[0]).Value;
				txtSearch.Text = string.Empty;
				SelectionMade?.Invoke(this, new SearchBoxEventArgs(id));
			}
		}

		#endregion Events
	}
}
