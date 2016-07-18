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

		/// <summary>What is displayed when no item is selected.</summary>
		private const string EmptyString = "(Empty)";

		/// <summary>The path of the current database.</summary>
		private string path;

		/// <summary>The path of the current database.</summary>
		public string Path
		{
			get { return path; }
			set
			{
				path = value;
				txtSearch.Path = value;
			}
		}

		/// <summary>The ID of the selected card.</summary>
		private string selectedValue;

		/// <summary>The ID of the selected card.</summary>
		public string SelectedValue
		{
			get { return selectedValue; }
			set
			{
				selectedValue = value;
				string tempError = string.Empty;
				updateValue(ref tempError);
			}
		}

		/// <summary>A comma-delimited list of card types to search. null to search all types.</summary>
		public string CardTypes
		{
			get { return txtSearch.CardTypes; }
			set { txtSearch.CardTypes = value; }
		}

		/// <summary>Fired when the user selects a card.</summary>
		public event SearchBoxEventHandler SelectionMade;

		#endregion Members

		#region Constructors

		public SearchSelect()
		{
			InitializeComponent();

			lblSelection.Text = EmptyString;
		}

		#endregion Constructors

		#region Methods

		/// <summary>Updates the selection display.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void updateValue(ref string userMessage)
		{
			if (string.IsNullOrEmpty(SelectedValue))
			{
				lblSelection.Text = EmptyString;
			}
			else
			{
				List<string[]> name = CardManager.getCardNames(new string[] { SelectedValue }, string.IsNullOrEmpty(CardTypes), Path, ref userMessage);
				lblSelection.Text = name[0][1];
			}
		}

		/// <summary>Displays the selection.</summary>
		private void txtSearch_SelectionMade(UserControl sender, SearchBoxEventArgs e)
		{
			string userMessage = string.Empty;

			selectedValue = e.SelectedValue;
			updateValue(ref userMessage);

			SelectionMade?.Invoke(this, e);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Displays the search box.</summary>
		private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
		{
			txtSearch.Opacity = 1d;
			lblSelection.Visibility = Visibility.Collapsed;
		}

		/// <summary>Displays the selection.</summary>
		private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
		{
			txtSearch.Opacity = 0d;
			lblSelection.Visibility = Visibility.Visible;
		}

		#endregion Methods
	}
}
