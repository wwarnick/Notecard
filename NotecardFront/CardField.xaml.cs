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
	/// Interaction logic for TextField.xaml
	/// </summary>
	public partial class CardField : UserControl
	{
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

		/// <summary>The database ID of the owning card.</summary>
		public string CardID { get; set; }

		/// <summary>The database ID of the card type field.</summary>
		public string CardTypeFieldID { get; set; }

		/// <summary>The database ID of the arrangement card.</summary>
		public string ArrangementCardID { get; set; }

		/// <summary>The index of the field in the card type.</summary>
		public int FieldIndex { get; set; }

		/// <summary>The database ID of the referred card.</summary>
		public string Value { get; set; }

		/// <summary>The text to show in the label.</summary>
		public string LabelText
		{
			get { return lblName.Text; }
			set { lblName.Text = value; }
		}

		/// <summary>A comma-delimited list of card types to search. null to search all types.</summary>
		public string FilterCardTypes
		{
			get { return txtSearch.CardTypes; }
			set { txtSearch.CardTypes = value; }
		}

		/// <summary>Called after the value is changed.</summary>
		public event EventHandler ValueChanged;

		public CardField()
		{
			InitializeComponent();

			this.Tag = DataType.Card;
		}

		/// <summary>Refreshes the interface.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public void refresh(ref string userMessage)
		{
			txtSearch.clear();
			lblSelection.Text = this.Value;

			if (string.IsNullOrEmpty(this.Value))
			{
				lblSelection.Visibility = Visibility.Collapsed;
				lblRemove.Visibility = Visibility.Collapsed;
				txtSearch.Opacity = 1d;
				txtSearch.Cursor = Cursors.IBeam;
				lblSelection.Text = string.Empty;
			}
			else
			{
				lblSelection.Text = CardManager.getCardNames(new string[] { this.Value }, Path, ref userMessage)[0];
				lblSelection.Visibility = Visibility.Visible;
				lblRemove.Visibility = Visibility.Visible;
				txtSearch.Opacity = 0d;
				txtSearch.Cursor = Cursors.Hand;
			}
		}

		/// <summary>Displays the selection.</summary>
		private void txtSearch_SelectionMade(UserControl sender, SearchBoxEventArgs e)
		{
			string userMessage = string.Empty;

			if (this.Value != e.SelectedValue)
			{
				CardManager.saveCardCardField(e.SelectedValue, CardID, CardTypeFieldID, Path, ref userMessage);

				this.Value = e.SelectedValue;
				refresh(ref userMessage);

				ValueChanged?.Invoke(this, e);
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Displays the search box.</summary>
		private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (lblSelection.Visibility == Visibility.Visible)
			{
				txtSearch.Opacity = 1d;
				txtSearch.Cursor = Cursors.IBeam;
				lblSelection.Visibility = Visibility.Collapsed;
				lblRemove.Visibility = Visibility.Collapsed;

				this.Value = null;
				CardManager.saveCardCardField(this.Value, CardID, CardTypeFieldID, path, ref userMessage);
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Displays the selection.</summary>
		private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty(this.Value))
			{
				txtSearch.Opacity = 0d;
				txtSearch.Cursor = Cursors.Hand;
				lblSelection.Visibility = Visibility.Visible;
				lblRemove.Visibility = Visibility.Visible;
			}
		}
	}
}
