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
	/// Interaction logic for CardControl.xaml
	/// </summary>
	public partial class CardControl : UserControl
	{
		#region Members

		/// <summary>The database ID of the card.</summary>
		public string CardID { get; set; }

		/// <summary>The path of the current database.</summary>
		public string Path { get; set; }

		/// <summary>The card's card type and its ancestors.</summary>
		private List<CardType> CardTypes;

		/// <summary>The card's data.</summary>
		private Card cardData;

		/// <summary>The card's data.</summary>
		public Card CardData
		{
			get { return cardData; }
			private set
			{
				cardData = value;
				this.Tag = value.ID;
			}
		}

		/// <summary>The database ID of the card's settings for the current arrangement.</summary>
		public string ArrangementCardID { get; set; }

		/// <summary>How far the cursor is from the upper-left corner of the card.</summary>
		private Point dragOffset;

		/// <summary>Fired when the card is archived.</summary>
		public event EventHandler Archived;

		/// <summary>Fired when the card is moved or resized.</summary>
		public event EventHandler MovedOrResized;

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the CardControl class.</summary>
		public CardControl()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region Methods

		/// <summary>Gets the index of a card type field.</summary>
		/// <param name="cardTypeFieldID">The database ID of the card type field.</param>
		/// <returns>The index of the card type field. -1 if not found.</returns>
		private int getFieldIndex(string cardTypeFieldID)
		{
			int fieldIndex = 0;

			foreach (CardType ct in CardTypes)
			{
				foreach (CardTypeField f in ct.Fields)
				{
					if (f.ID == cardTypeFieldID)
						return fieldIndex;

					fieldIndex++;
				}
			}

			return -1;
		}

		/// <summary>Clears and recreates the UI elements.</summary>
		/// <param name="cardTypes">The card's card type and its ancestors.</param>
		/// <param name="arrangementSettings">The card's settings for the current arrangement.</param>
		/// <returns>Any error messages.</returns>
		public string refreshUI(List<CardType> cardTypes, ArrangementCard arrangementSettings)
		{
			string errorMessage = string.Empty;

			CardTypes = cardTypes;
			ArrangementCardID = (arrangementSettings == null) ? null : arrangementSettings.ID;

			Card cd;
			errorMessage += CardManager.getCard(CardID, Path, out cd, CardTypes);
			CardData = cd;

			stkMain.MouseDown -= stkMain_MouseDown;
			stkMain.Children.Clear();

			SolidColorBrush titleBrush = null;

			// add title bar
			if (CardData.CType.Context == CardTypeContext.Standalone)
			{
				Grid grdTitleBar = new Grid();
				ColumnDefinition col = new ColumnDefinition();
				col.Width = new GridLength(1d, GridUnitType.Star);
				grdTitleBar.ColumnDefinitions.Add(col);
				col = new ColumnDefinition();
				col.Width = new GridLength(20d);
				grdTitleBar.ColumnDefinitions.Add(col);
				col = new ColumnDefinition();
				col.Width = new GridLength(20d);
				grdTitleBar.ColumnDefinitions.Add(col);

				titleBrush = new SolidColorBrush(Color.FromRgb(CardData.CType.ColorRed, CardData.CType.ColorGreen, CardData.CType.ColorBlue));

				TextBlock lblCardType = new TextBlock()
				{
					Background = titleBrush,
					Foreground = Brushes.White,
					FontSize = 10d,
					Padding = new Thickness(3, 0, 0, 0),
					Text = CardData.CType.Name
				};
				grdTitleBar.Children.Add(lblCardType);

				Button btnArchive = new Button();
				btnArchive.Content = "-";
				btnArchive.Click += btnArchive_Click;
				Grid.SetColumn(btnArchive, 1);
				grdTitleBar.Children.Add(btnArchive);

				Button btnDeleteCard = new Button();
				btnDeleteCard.Content = "X";
				btnDeleteCard.Click += btnDeleteCard_Click;
				Grid.SetColumn(btnDeleteCard, 2);
				grdTitleBar.Children.Add(btnDeleteCard);

				stkMain.Children.Add(grdTitleBar);

				// background is a lighter tone of the title color
				stkMain.Background = Tools.colorToSum(CardData.CType.ColorRed, CardData.CType.ColorGreen, CardData.CType.ColorBlue, 675);

				stkMain.MouseDown += stkMain_MouseDown;
			}

			// add fields
			int fieldIndex = 0;
			int textFieldIndex = 0;
			int cardFieldIndex = 0;
			int listFieldIndex = 0;
			foreach (CardType ct in CardTypes)
			{
				foreach (CardTypeField f in ct.Fields)
				{
					Panel newPanel = null;

					TextBlock lbl = new TextBlock();
					lbl.Text = f.Name;
					lbl.VerticalAlignment = VerticalAlignment.Center;
					lbl.Margin = new Thickness(5d);

					switch (f.FieldType)
					{
						case DataType.Text:
							{
								// get height increase
								int heightIncrease = (arrangementSettings != null)
									? arrangementSettings.TextFields[textFieldIndex].HeightIncrease
									: 0;

								// create controls
								newPanel = new Grid();
								newPanel.Tag = DataType.Text;
								Grid grdPanel = (Grid)newPanel;
								grdPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
								grdPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1d, GridUnitType.Star) });
								grdPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1d, GridUnitType.Star) });
								grdPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5d) });
								newPanel.Children.Add(lbl);

								TextBox txt = new TextBox()
								{
									MinHeight = 20d,
									Height = MinHeight + heightIncrease,
									MinWidth = 100d,
									Text = (string)CardData.Fields[fieldIndex],
									TextWrapping = (heightIncrease == 0) ? TextWrapping.NoWrap : TextWrapping.Wrap,
									Tag = f.ID,
									Margin = new Thickness(0d, 5d, 10d, 0d),
									AcceptsReturn = true,
									VerticalScrollBarVisibility = ScrollBarVisibility.Auto
								};

								txt.LostKeyboardFocus += txtField_LostKeyboardFocus;
								Grid.SetColumn(txt, 1);
								newPanel.Children.Add(txt);

								Rectangle textFieldResize = new Rectangle()
								{
									Margin = new Thickness(0d, 0d, 10d, 0d),
									Fill = Brushes.Transparent,
									Tag = txt,
									Cursor = Cursors.SizeNS
								};
								textFieldResize.MouseDown += textFieldResize_MouseDown;
								Grid.SetRow(textFieldResize, 1);
								Grid.SetColumn(textFieldResize, 1);
								newPanel.Children.Add(textFieldResize);

								textFieldIndex++;
							}
							break;
						case DataType.Card:
							{
								newPanel = new DockPanel();
								newPanel.Tag = DataType.Card;
								DockPanel.SetDock(lbl, Dock.Left);
								newPanel.Children.Add(lbl);

								SearchSelect txtCardSearch = new SearchSelect()
								{
									Height = 20d,
									MinWidth = 100d,
									Path = Path,
									SelectedValue = (string)CardData.Fields[fieldIndex],
									Tag = f.ID,
									Margin = new Thickness(0d, 5d, 10d, 5d)
								};
								txtCardSearch.SelectionMade += txtCardSearch_SelectionMade;

								if (!string.IsNullOrEmpty(f.RefCardTypeID))
								{
									string descendents;
									errorMessage += CardManager.getCardTypeDescendents(f.RefCardTypeID, Path, out descendents);
									txtCardSearch.CardTypes = descendents;
								}

								newPanel.Children.Add(txtCardSearch);

								cardFieldIndex++;
							}
							break;
						case DataType.List:
							{
								newPanel = new DockPanel();
								newPanel.Tag = DataType.List;
								DockPanel.SetDock(lbl, Dock.Top);
								newPanel.Children.Add(lbl);

								List<Card> items = (List<Card>)CardData.Fields[fieldIndex];
								StackPanel pnlList = new StackPanel();

								foreach (Card c in items)
								{
									ArrangementCardList l = (arrangementSettings == null) ? null : ((ArrangementCardStandalone)arrangementSettings).ListItems[listFieldIndex];
									CardControl item = newListItem(c.ID, f.ListType, l, ref errorMessage);
									item.ArrangementCardID = CardManager.getArrangementListCardID(ArrangementCardID, item.CardID, Path, ref errorMessage);
									pnlList.Children.Add(item);
								}

								Button btnListAddItem = new Button()
								{
									Content = "+",
									Tag = f
								};

								btnListAddItem.Click += btnAddListItem_Click;
								pnlList.Children.Add(btnListAddItem);

								refreshListItemBackColors(pnlList);

								newPanel.Children.Add(pnlList);

								listFieldIndex++;
							}
							break;
						default:
							MessageBox.Show("Unknown field type: " + f.FieldType.ToString());
							break;
					}

					// title field
					if (fieldIndex == 0 && CardData.CType.Context == CardTypeContext.Standalone)
					{
						newPanel.Background = titleBrush;
						lbl.Foreground = Brushes.White;
						lbl.FontWeight = FontWeights.Bold;
					}

					stkMain.Children.Add(newPanel);

					fieldIndex++;
				}
			}

			return errorMessage;
		}

		/// <summary>Updates the arrangement card IDs of all list items.</summary>
		/// <returns>Any error messages.</returns>
		public string updateListItemArrangementIDs()
		{
			string errorMessage = string.Empty;

			List<string> ids = CardManager.getArrangementListCardIDs(ArrangementCardID, Path, ref errorMessage);

			int itemIndex = 0;
			foreach (FrameworkElement ui in stkMain.Children)
			{
				if (ui.Tag == null || (DataType)ui.Tag != DataType.List)
					continue;

				Panel pnl = (Panel)((Panel)ui).Children[1];

				for (int i = 0; i < pnl.Children.Count - 1; i++)
				{
					((CardControl)pnl.Children[i]).ArrangementCardID = ids[itemIndex];
					itemIndex++;
				}
			}

			return errorMessage;
		}

		/// <summary>Adds a new list item to a list field.</summary>
		/// <param name="itemID">The database ID of the list item.</param>
		/// <param name="listType">The list item's type.</param>
		/// <returns>Any error messages.</returns>
		private CardControl newListItem(string itemID, CardType listType, ArrangementCardList arrangementCard, ref string errorMessage)
		{
			CardControl item = new CardControl()
			{
				Path = this.Path,
				CardID = itemID
			};

			errorMessage += item.refreshUI(new List<CardType>() { listType }, arrangementCard);

			return item;
		}

		/// <summary>Sets the background colors of list items.</summary>
		/// <param name="pnl">The list items' owning panel.</param>
		private void refreshListItemBackColors(StackPanel pnl)
		{
			for (int i = 0; i < pnl.Children.Count - 1; i++)
			{
				CardControl c = (CardControl)pnl.Children[i];

				if (i % 2 == 0)
					c.Background = Brushes.White;
				else
					c.Background = Brushes.LightGray;
			}
		}

		/// <summary>Adds a new list item to a list field.</summary>
		private void btnAddListItem_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			Button btn = (Button)sender;
			CardTypeField field = (CardTypeField)btn.Tag;

			string id;
			errorMessage += CardManager.newListItem(CardData, field, Path, out id);

			Card newItem;
			errorMessage += CardManager.getCard(id, Path, out newItem, new List<CardType>() { field.ListType });

			int index = getFieldIndex(field.ID);
			((List<Card>)CardData.Fields[index]).Add(newItem);
			
			CardControl c = newListItem(newItem.ID, field.ListType, null, ref errorMessage);

			StackPanel pnl = (StackPanel)btn.Parent;
			pnl.Children.Remove(btn);

			pnl.Children.Add(c);
			pnl.Children.Add(btn);

			refreshListItemBackColors(pnl);

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Saves the value of a card field.</summary>
		private void txtCardSearch_SelectionMade(UserControl sender, SearchBoxEventArgs e)
		{
			string errorMessage = string.Empty;

			int index = getFieldIndex((string)sender.Tag);

			if ((string)CardData.Fields[index] != e.SelectedValue)
			{
				CardData.Fields[index] = e.SelectedValue;
				errorMessage += CardManager.saveCardCardField(e.SelectedValue, CardData.ID, (string)sender.Tag, Path);
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Saves the value of a text field.</summary>
		public void txtField_LostKeyboardFocus(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			TextBox txt = (TextBox)sender;

			int index = getFieldIndex((string)txt.Tag);
			if ((string)CardData.Fields[index] != txt.Text)
			{
				CardData.Fields[index] = txt.Text;
				errorMessage += CardManager.saveCardTextField(txt.Text, CardData.ID, (string)txt.Tag, Path);
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Archive the card.</summary>
		private void btnArchive_Click(object sender, RoutedEventArgs e)
		{
			((Canvas)this.Parent).Children.Remove(this);

			Archived?.Invoke(this, null);
		}

		/// <summary>Delete the card.</summary>
		private void btnDeleteCard_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			((Canvas)this.Parent).Children.Remove(this);

			errorMessage += CardManager.deleteCard(CardData.ID, Path);

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		#region Move and Resize

		/// <summary>Prepares the card for moving or resizing.</summary>
		/// <param name="ui">The element being dragged.</param>
		/// <param name="h">The method to call when moving the cursor.</param>
		/// <param name="e">Mouse data.</param>
		private void prepMoveResize(UIElement ui, MouseEventHandler h, MouseEventArgs e)
		{
			Mouse.Capture(null);
			if (ui.CaptureMouse())
			{
				Point mousePos = e.MouseDevice.GetPosition(ui);
				dragOffset = mousePos;

				ui.MouseMove += h;
			}
		}

		/// <summary>Prepare to move the card.</summary>
		private void stkMain_MouseDown(object sender, MouseButtonEventArgs e)
		{
			prepMoveResize(this, CardControl_MouseMove, e);
		}

		/// <summary>Move the card.</summary>
		private void CardControl_MouseMove(object sender, MouseEventArgs e)
		{
			// stop moving the card
			if (e.LeftButton == MouseButtonState.Released)
			{
				this.MouseMove -= CardControl_MouseMove;
				Mouse.Capture(null);

				MovedOrResized?.Invoke(this, null);

				return;
			}

			Point cursor = e.MouseDevice.GetPosition((UIElement)this.Parent);

			Canvas.SetLeft(this, cursor.X - dragOffset.X);
			Canvas.SetTop(this, cursor.Y - dragOffset.Y);
		}

		/// <summary>Prepare to resize the card to the left.</summary>
		private void rResizeL_MouseDown(object sender, MouseButtonEventArgs e)
		{
			prepMoveResize(rResizeL, rResizeL_MouseMove, e);
		}

		/// <summary>Resize the card to the left.</summary>
		private void rResizeL_MouseMove(object sender, MouseEventArgs e)
		{
			// stop resizing the card
			if (e.LeftButton == MouseButtonState.Released)
			{
				rResizeL.MouseMove -= rResizeL_MouseMove;
				Mouse.Capture(null);

				MovedOrResized?.Invoke(this, null);

				return;
			}

			Point cursor = e.MouseDevice.GetPosition(rResizeL);

			double diff = cursor.X - dragOffset.X;

			this.Width = this.ActualWidth - diff;
			Canvas.SetLeft(this, Canvas.GetLeft(this) + diff);
		}

		/// <summary>Prepare to resize the card to the right.</summary>
		private void rResizeR_MouseDown(object sender, MouseButtonEventArgs e)
		{
			prepMoveResize(rResizeR, rResizeR_MouseMove, e);
		}

		/// <summary>Resize the card to the right.</summary>
		private void rResizeR_MouseMove(object sender, MouseEventArgs e)
		{
			// stop resizing the card
			if (e.LeftButton == MouseButtonState.Released)
			{
				rResizeR.MouseMove -= rResizeR_MouseMove;
				Mouse.Capture(null);

				MovedOrResized?.Invoke(this, null);

				return;
			}

			Point cursor = e.MouseDevice.GetPosition(rResizeR);
			this.Width = this.ActualWidth + cursor.X - dragOffset.X;
		}

		/// <summary>Prepare to resize a text field.</summary>
		private void textFieldResize_MouseDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			prepMoveResize((UIElement)sender, textFieldResize_MouseMove, e);
		}

		/// <summary>Resize a text field.</summary>
		private void textFieldResize_MouseMove(object sender, MouseEventArgs e)
		{
			string errorMessage = string.Empty;

			Rectangle textFieldResize = (Rectangle)sender;
			TextBox textBox = (TextBox)textFieldResize.Tag;

			// stop resizing the card
			if (e.LeftButton == MouseButtonState.Released)
			{
				textFieldResize.MouseMove -= textFieldResize_MouseMove;
				Mouse.Capture(null);

				errorMessage += CardManager.setFieldTextHeightIncrease(ArrangementCardID, (string)textBox.Tag, (int)Math.Round(textBox.ActualHeight), Path);

				MovedOrResized?.Invoke(this, null);

				return;
			}

			Point cursor = e.MouseDevice.GetPosition(textFieldResize);

			double newHeight = textBox.ActualHeight + cursor.Y - dragOffset.Y;

			if (newHeight <= textBox.MinHeight)
			{
				textBox.TextWrapping = TextWrapping.NoWrap;
				textBox.Height = textBox.MinHeight;
			}
			else
			{
				textBox.TextWrapping = TextWrapping.Wrap;
				textBox.Height = newHeight;
			}

			this.Height = double.NaN;

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		#endregion Move and Resize

		#endregion Methods
	}
}
