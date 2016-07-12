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
		#region Members

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

		/// <summary>Whether or not the list field is minimized (shows only the label).</summary>
		private bool minimized;

		/// <summary>Whether or not the list field is minimized (shows only the label).</summary>
		public bool Minimized
		{
			get { return minimized; }
			set
			{
				if (minimized != value)
				{
					minimized = value;

					btnMinimize.Content = minimized ? "V" : "^";
					lblName.HorizontalAlignment = minimized ? HorizontalAlignment.Left : HorizontalAlignment.Center;

					Visibility visible = minimized ? Visibility.Collapsed : Visibility.Visible;

					for (int i = 1; i < stkMain.Children.Count; i++)
					{
						stkMain.Children[i].Visibility = visible;
					}
				}
			}
		}

		#endregion Members

		#region Constructors

		public ListField()
		{
			InitializeComponent();

			this.Tag = DataType.List;
		}

		#endregion Constructors

		#region Methods

		/// <summary>Refreshes the interface.</summary>
		/// <param name="arrangementSettings">The arrangement settings</param>
		/// <param name="listFieldIndex">The index of this field's first item in the arrangement settings.</param>
		/// <param name="userMessage">Any user messages.</param>
		public void refresh(ArrangementCard arrangementSettings, ref int listFieldIndex, ref string userMessage)
		{
			// set btnAddItem color
			btnAddItem.Background = Tools.colorToSum(ListType.ColorRed, ListType.ColorGreen, ListType.ColorBlue, 450);

			// remove all but the label
			stkMain.Children.RemoveRange(1, stkMain.Children.Count - 1);

			bool notFirst = false;
			foreach (Card c in this.Value)
			{
				// get arrangement settings
				ArrangementCardList l = (arrangementSettings == null) ? null : ((ArrangementCardStandalone)arrangementSettings).ListItems[listFieldIndex];

				// add switch, minimize, and remove buttons
				ListItemMenu menu = getNewListItemMenu(notFirst);
				menu.Minimized = (l != null) ? l.Minimized : false;
				stkMain.Children.Add(menu);

				CardControl item = newListItem(c.ID, this.ListType, l, ref userMessage);
				item.ArrangementCardID = (l != null) ? l.ID : CardManager.getArrangementListCardID(ArrangementCardID, item.CardID, Path, ref userMessage);
				item.Minimized = menu.Minimized;
				stkMain.Children.Add(item);

				listFieldIndex++;
				notFirst = true;
			}

			refreshListItemBackColors();
			stkMain.Children.Add(btnAddItem);
		}

		/// <summary>Returns a new list item menu.</summary>
		/// <param name="includeSwitch">Whether or not to include the switch button.</param>
		/// <returns>A new list item menu.</returns>
		public ListItemMenu getNewListItemMenu(bool includeSwitch)
		{
			ListItemMenu menu = new ListItemMenu()
			{
				Background = Tools.colorToSum(ListType.ColorRed, ListType.ColorGreen, ListType.ColorBlue, 450),
				ForeColor = Brushes.White,
				Tag = "menu",
				IncludeSwitch = includeSwitch
			};

			menu.Switch += Menu_Switch;
			menu.Minimize += Menu_Minimize;
			menu.RemoveItem += Menu_RemoveItem;

			return menu;
		}

		/// <summary>Switches the list items around it.</summary>
		private void Menu_Switch(object sender, EventArgs e)
		{
			string userMessage = string.Empty;

			CardControl lastListItem = null;
			CardControl nextListItem = null;
			bool foundMenu = false;

			FrameworkElement[] reAdd = null;
			int startIndex = 0;

			
			for (int i = 0; i < stkMain.Children.Count; i++)
			{
				FrameworkElement ui = (FrameworkElement)stkMain.Children[i];

				if (reAdd?[0] == null)
				{
					switch ((string)ui.Tag)
					{
						case "label":
						case "add":
							// do nothing
							break;
						case "menu":
							if (ui == sender)
							{
								foundMenu = true;
								startIndex = i - 1; // start with the item before the switch button
								reAdd = new FrameworkElement[stkMain.Children.Count - startIndex];
								reAdd[1] = ui;
								reAdd[2] = nextListItem;
							}
							break;
						default: // list item
							lastListItem = nextListItem;
							nextListItem = (CardControl)ui;

							if (foundMenu)
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

		/// <summary>Minimizes the list item below it.</summary>
		private void Menu_Minimize(object sender, EventArgs e)
		{
			string userMessage = string.Empty;

			for (int i = 0; i < stkMain.Children.Count; i++)
			{
				if ((FrameworkElement)stkMain.Children[i] == sender)
				{
					CardControl c = (CardControl)(FrameworkElement)stkMain.Children[i + 1];
					c.Minimized = !c.Minimized;

					CardManager.setListItemMinimized(c.ArrangementCardID, c.Minimized, this.Path, ref userMessage);
					break;
				}
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Removes the list item below it.</summary>
		private void Menu_RemoveItem(object sender, EventArgs e)
		{
			string userMessage = string.Empty;

			bool foundMenu = false;
			CardControl toDelete = null;

			FrameworkElement[] reAdd = null;
			int startIndex = 0;

			for (int i = 0; i < stkMain.Children.Count; i++)
			{
				FrameworkElement ui = (FrameworkElement)stkMain.Children[i];

				if (reAdd == null)
				{
					switch ((string)ui.Tag)
					{
						case "label":
						case "add":
							// do nothing
							break;
						case "menu":
							if (ui == sender)
							{
								foundMenu = true;
								startIndex = i; // start delete with the menu
							}
							break;
						default: // list item
							if (foundMenu)
							{
								reAdd = new FrameworkElement[stkMain.Children.Count - startIndex - 2];
								toDelete = (CardControl)ui;
							}
							break;
					}
				}
				else
				{
					reAdd[i - startIndex - 2] = ui;
				}
			}

			// put them back in
			stkMain.Children.RemoveRange(startIndex, stkMain.Children.Count - startIndex);
			foreach (FrameworkElement ui in reAdd)
			{
				stkMain.Children.Add(ui);
			}

			refreshListItemBackColors();

			// apply change to database
			CardManager.deleteCard(toDelete.CardID, this.Path, ref userMessage);

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
			Brush brush1 = Tools.colorToSum(ListType.ColorRed, ListType.ColorGreen, ListType.ColorBlue, 600);
			Brush brush2 = Tools.colorToSum(ListType.ColorRed, ListType.ColorGreen, ListType.ColorBlue, 725);

			int listItemIndex = 0;
			for (int i = 0; i < stkMain.Children.Count; i++)
			{
				string tag = (string)((FrameworkElement)stkMain.Children[i]).Tag;
				if (tag == "menu" || tag == "label" || tag == "add")
					continue;

				CardControl c = (CardControl)stkMain.Children[i];

				if (listItemIndex % 2 == 0)
					c.Background = brush1;
				else
					c.Background = brush2;

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
			CardManager.setListItemMinimized(c.ArrangementCardID, false, this.Path, ref userMessage);

			c.updateFieldArrangementIDs(ref userMessage);

			stkMain.Children.Remove(btnAddItem);

			stkMain.Children.Add(getNewListItemMenu(this.Value.Count > 1));

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
				if (tag == "menu" || tag == "label" || tag == "add")
					continue;

				((CardControl)stkMain.Children[listItemIndex]).ArrangementCardID = ids[itemIndex];
				itemIndex++;
				listItemIndex++;
			}
		}

		/// <summary>Minimizes the list field.</summary>
		private void btnMinimize_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			this.Minimized = !this.Minimized;
			CardManager.setFieldListMinimized(ArrangementCardID, CardTypeFieldID, this.Minimized, this.Path, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		#endregion Methods
	}
}
