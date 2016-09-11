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
		#region Members

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

		/// <summary>The field's label.</summary>
		public TextBlock lblName { get; set; }

		/// <summary>Whether or not the label should be displayed.</summary>
		public bool ShowLabel
		{
			get { return lblName != null; }
			set
			{
				if (value && lblName == null)
				{
					lblName = new TextBlock()
					{
						Margin = new Thickness(5d),
						VerticalAlignment = VerticalAlignment.Center
					};

					grdMain.Children.Add(lblName);

					txtSearch.Margin = new Thickness(0d, 5d, 10d, 5d);
					btnSelection.Margin = new Thickness(0d, 0d, 0d, 0d);
				}
				else if (!value && lblName != null)
				{
					grdMain.Children.Remove(lblName);
					lblName = null;

					txtSearch.Margin = new Thickness(10d, 5d, 10d, 5d);
					btnSelection.Margin = new Thickness(10d, 0d, 0d, 0d);
				}
			}
		}

		/// <summary>The text to show in the label.</summary>
		public string LabelText
		{
			get { return lblName.Text; }
			set { if (ShowLabel) lblName.Text = value; }
		}

		/// <summary>A comma-delimited list of card types to search. null to search all types.</summary>
		public string FilterCardTypes
		{
			get { return txtSearch.CardTypes; }
			set { txtSearch.CardTypes = value; }
		}

		/// <summary>Called after the value is changed.</summary>
		public event EventHandler ValueChanged;

		/// <summary>Called when btnSelection is clicked.</summary>
		public event EventHandler OpenCard;

		#endregion Members

		#region Constructors

		public CardField()
		{
			InitializeComponent();

			this.Tag = DataType.Card;
		}

		#endregion Constructors

		#region Methods

		/// <summary>Refreshes the interface.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public void refresh(ref string userMessage)
		{
			txtSearch.clear();
			btnSelection.Content = this.Value;

			if (string.IsNullOrEmpty(this.Value))
			{
				btnSelection.Visibility = Visibility.Collapsed;
				lblRemove.Visibility = Visibility.Collapsed;
				txtSearch.Opacity = 1d;
				txtSearch.Cursor = Cursors.IBeam;
				btnSelection.Content = string.Empty;
			}
			else
			{
				btnSelection.Content = CardManager.getCardTitle(this.Value, string.IsNullOrEmpty(FilterCardTypes), ref userMessage);
				btnSelection.Visibility = Visibility.Visible;
				lblRemove.Visibility = Visibility.Visible;
				txtSearch.Opacity = 0d;
				txtSearch.Cursor = Cursors.Hand;
			}
		}

		/// <summary>Displays the selection.</summary>
		private void txtSearch_SelectionMade(UserControl sender, SearchSelectEventArgs e)
		{
			string userMessage = string.Empty;

			if (this.Value != e.SelectedValue)
			{
				CardManager.saveCardCardField(e.SelectedValue, CardID, CardTypeFieldID, ref userMessage);

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

			if (btnSelection.Visibility == Visibility.Visible)
			{
				txtSearch.Opacity = 1d;
				txtSearch.Cursor = Cursors.IBeam;
				btnSelection.Visibility = Visibility.Collapsed;
				lblRemove.Visibility = Visibility.Collapsed;

				this.Value = null;
				CardManager.saveCardCardField(this.Value, CardID, CardTypeFieldID, ref userMessage);
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
				btnSelection.Visibility = Visibility.Visible;
				lblRemove.Visibility = Visibility.Visible;
			}
		}

		/// <summary>Opens the selected card.</summary>
		private void btnSelection_Click(object sender, RoutedEventArgs e)
		{
			OpenCard?.Invoke(this, EventArgs.Empty);
		}

		#endregion Methods
	}
}
