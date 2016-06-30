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

			bool notFirst = false;
			foreach (Card c in this.Value)
			{
				// add switch button
				if (notFirst)
					stkMain.Children.Add(getNewSwitchButton());

				ArrangementCardList l = (arrangementSettings == null) ? null : ((ArrangementCardStandalone)arrangementSettings).ListItems[listFieldIndex];
				CardControl item = newListItem(c.ID, this.ListType, l, ref userMessage);
				item.ArrangementCardID = (l != null) ? l.ID : CardManager.getArrangementListCardID(ArrangementCardID, item.CardID, Path, ref userMessage);
				stkMain.Children.Add(item);

				listFieldIndex++;
				notFirst = true;
			}

			refreshListItemBackColors();
			stkMain.Children.Add(btnAddItem);
		}

		/// <summary>Returns a new switch button.</summary>
		/// <returns>A new switch button.</returns>
		public Button getNewSwitchButton()
		{
			Button btnSwitch = new Button()
			{
				Content = "<>",
				Tag = "switch"
			};

			btnSwitch.Click += btnSwitch_Click;

			return btnSwitch;
		}

		/// <summary>Switches the list items around it.</summary>
		private void btnSwitch_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			CardControl lastListItem = null;
			CardControl nextListItem = null;
			bool foundSwitch = false;

			FrameworkElement[] reAdd = null;
			int startIndex = 0;

			
			for (int i = 0; i < stkMain.Children.Count; i++)
			{
				FrameworkElement ui = (FrameworkElement)stkMain.Children[i];

				if (reAdd == null || reAdd[0] == null)
				{
					switch ((string)ui.Tag)
					{
						case "label":
						case "add":
							// do nothing
							break;
						case "switch":
							if (ui == sender)
							{
								foundSwitch = true;
								startIndex = i - 1; // start with the item before the switch button
								reAdd = new FrameworkElement[stkMain.Children.Count - startIndex];
								reAdd[1] = ui;
								reAdd[2] = nextListItem;
							}
							break;
						default: // list item
							lastListItem = nextListItem;
							nextListItem = (CardControl)ui;

							if (foundSwitch)
							{
								reAdd[0] = ui;

								// preserve background colors
								Brush temp = lastListItem.Background;
								lastListItem.Background = nextListItem.Background;
								nextListItem.Background = temp;
							}
							break;
					}
				}
				else
				{
					reAdd[i - startIndex] = ui;
				}
			}

			// put them back in
			stkMain.Children.RemoveRange(startIndex, reAdd.Length);
			foreach (FrameworkElement ui in reAdd)
			{
				stkMain.Children.Add(ui);
			}

			// apply change to database
			CardManager.swapListItems(lastListItem.CardID, nextListItem.CardID, this.Path, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
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
			int listItemIndex = 0;
			for (int i = 0; i < stkMain.Children.Count; i++)
			{
				string tag = (string)((FrameworkElement)stkMain.Children[i]).Tag;
				if (tag == "switch" || tag == "label" || tag == "add")
					continue;

				CardControl c = (CardControl)stkMain.Children[i];

				if (listItemIndex % 2 == 0)
					c.Background = Brushes.LightGray;
				else
					c.Background = Brushes.White;

				listItemIndex++;
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

			if (this.Value.Count > 1)
				stkMain.Children.Add(getNewSwitchButton());

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
			int listItemIndex = 0;
			for (int i = 0; i < stkMain.Children.Count; i++)
			{
				string tag = (string)((FrameworkElement)stkMain.Children[i]).Tag;
				if (tag == "switch" || tag == "label" || tag == "add")
					continue;

				((CardControl)stkMain.Children[listItemIndex]).ArrangementCardID = ids[itemIndex];
				itemIndex++;
				listItemIndex++;
			}
		}
	}
}
