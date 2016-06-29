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
	/// Interaction logic for ListField.xaml
	/// </summary>
	public partial class ListField : UserControl
	{
		/// <summary>The path of the current database.</summary>
		public string Path { get; set; }

		/// <summary>The database ID of the owning card.</summary>
		public string CardID { get; set; }

		/// <summary>The database ID of the card type field.</summary>
		public string CardTypeFieldID { get; set; }

		/// <summary>The database ID of the arrangement card.</summary>
		public string ArrangementCardID { get; set; }

		/// <summary>The index of the field in the card type.</summary>
		public int FieldIndex { get; set; }

		/// <summary>The list item data.</summary>
		public List<Card> Value { get; set; }

		/// <summary>The list field's type.</summary>
		public CardType ListType { get; set; }

		/// <summary>The text to show in the label.</summary>
		public string LabelText
		{
			get { return lblName.Text; }
			set { lblName.Text = value; }
		}

		/// <summary>Called after the field is changed.</summary>
		public event EventHandler Changed;

		public ListField()
		{
			InitializeComponent();

			this.Tag = DataType.List;
		}

		/// <summary>Refreshes the interface.</summary>
		/// <param name="arrangementSettings">The arrangement settings</param>
		/// <param name="listFieldIndex">The index of this field's first item in the arrangement settings.</param>
		/// <param name="userMessage">Any user messages.</param>
		public void refresh(ArrangementCard arrangementSettings, ref int listFieldIndex, ref string userMessage)
		{
			// remove all but the label
			stkMain.Children.RemoveRange(1, stkMain.Children.Count - 1);

			foreach (Card c in this.Value)
			{
				ArrangementCardList l = (arrangementSettings == null) ? null : ((ArrangementCardStandalone)arrangementSettings).ListItems[listFieldIndex];
				CardControl item = newListItem(c.ID, this.ListType, l, ref userMessage);
				item.ArrangementCardID = (l != null) ? l.ID : CardManager.getArrangementListCardID(ArrangementCardID, item.CardID, Path, ref userMessage);
				stkMain.Children.Add(item);

				listFieldIndex++;
			}

			refreshListItemBackColors();
			stkMain.Children.Add(btnAddItem);
		}

		/// <summary>Adds a new list item to a list field.</summary>
		/// <param name="itemID">The database ID of the list item.</param>
		/// <param name="listType">The list item's type.</param>
		/// <returns>Any error messages.</returns>
		private CardControl newListItem(string itemID, CardType listType, ArrangementCardList arrangementCard, ref string userMessage)
		{
			CardControl item = new CardControl()
			{
				Path = this.Path,
				CardID = itemID
			};

			item.refreshUI(new List<CardType>() { listType }, arrangementCard, ref userMessage);

			return item;
		}

		/// <summary>Sets the background colors of list items.</summary>
		private void refreshListItemBackColors()
		{
			for (int i = 1; i < stkMain.Children.Count - 1; i++)
			{
				CardControl c = (CardControl)stkMain.Children[i];

				if (i % 2 == 0)
					c.Background = Brushes.White;
				else
					c.Background = Brushes.LightGray;
			}
		}

		/// <summary>Adds a new list item.</summary>
		private void btnAddItem_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			string id = CardManager.newListItem(CardID, CardTypeFieldID, this.ListType, Path, ref userMessage);

			Card newItem = CardManager.getCard(id, Path, new List<CardType>() { this.ListType }, ref userMessage);

			this.Value.Add(newItem);

			CardControl c = newListItem(newItem.ID, this.ListType, null, ref userMessage);
			c.ArrangementCardID = CardManager.getArrangementListCardID(ArrangementCardID, id, Path, ref userMessage);
			c.updateFieldArrangementIDs(ref userMessage);

			stkMain.Children.Remove(btnAddItem);

			stkMain.Children.Add(c);
			stkMain.Children.Add(btnAddItem);

			refreshListItemBackColors();

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Updates the arrangement card IDs of all list items.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public void updateArrangementIDs(List<string> ids, ref int itemIndex, ref string userMessage)
		{
			for (int i = 1; i < stkMain.Children.Count - 1; i++)
			{
				((CardControl)stkMain.Children[i]).ArrangementCardID = ids[itemIndex];
				itemIndex++;
			}
		}
	}
}
