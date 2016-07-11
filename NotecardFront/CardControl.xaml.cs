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

		/// <summary>The various different highlight statuses.</summary>
		public enum HighlightStatuses { Selected, Connected, None }

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

		/// <summary>How far the cursor is from the upper-left corner of the card when dragging.</summary>
		private Point dragOffset;

		/// <summary>The card's current highlight status.</summary>
		private HighlightStatuses highlight;

		/// <summary>The card's current highlight status.</summary>
		public HighlightStatuses Highlight
		{
			get { return highlight; }
			set
			{
				highlight = value;

				return;

				switch (highlight)
				{
					case HighlightStatuses.Selected:
						{
							var shadow = new System.Windows.Media.Effects.DropShadowEffect();
							shadow.ShadowDepth = 0d;
							shadow.BlurRadius = 10d;
							shadow.Color = Colors.LightBlue;
							stkMain.Effect = shadow;
						}
						break;
					case HighlightStatuses.Connected:
						{
							var shadow = new System.Windows.Media.Effects.DropShadowEffect();
							shadow.ShadowDepth = 0d;
							shadow.Color = Colors.Red;
							stkMain.Effect = shadow;
						}
						break;
					case HighlightStatuses.None:
						{
							stkMain.Effect = null;
						}
						break;
					default:
						throw new Exception("Unknown highlight status: " + highlight.ToString());
				}
			}
		}

		/// <summary>Whether or not the card control is minimized (shows only the first field).</summary>
		private bool minimized;

		/// <summary>Whether or not the card control is minimized (shows only the first field).</summary>
		public bool Minimized
		{
			get { return minimized; }
			set
			{
				if (minimized != value)
				{
					minimized = value;
					Visibility visible = minimized ? Visibility.Collapsed : Visibility.Visible;

					for (int i = 1; i < stkMain.Children.Count; i++)
					{
						stkMain.Children[i].Visibility = visible;
					}
				}
			}
		}

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

			highlight = HighlightStatuses.None;
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
		public void refreshUI(List<CardType> cardTypes, ArrangementCard arrangementSettings, ref string userMessage)
		{
			CardTypes = cardTypes;
			ArrangementCardID = (arrangementSettings == null) ? null : arrangementSettings.ID;

			CardData = CardManager.getCard(CardID, Path, CardTypes, ref userMessage);

			stkMain.MouseDown -= stkMain_MouseDown;
			stkMain.Children.Clear();

			SolidColorBrush titleBrush = null;

			// add title bar
			if (CardData.CType.Context == CardTypeContext.Standalone)
			{
				// add resize borders
				Rectangle rResizeL = new Rectangle()
				{
					Cursor = Cursors.SizeWE,
					Width = 5d,
					Fill = Brushes.Transparent
				};
				rResizeL.MouseDown += rResizeL_MouseDown;

				Rectangle rResizeR = new Rectangle()
				{
					Cursor = Cursors.SizeWE,
					Width = 5d,
					Fill = Brushes.Transparent
				};
				Grid.SetColumn(rResizeR, 2);
				rResizeR.MouseDown += rResizeR_MouseDown;

				grdBase.Children.Add(rResizeL);
				grdBase.Children.Add(rResizeR);

				// get title bar color
				titleBrush = new SolidColorBrush(Color.FromRgb(CardData.CType.ColorRed, CardData.CType.ColorGreen, CardData.CType.ColorBlue));

				// prepare title bar
				Grid grdTitleBar = new Grid()
				{
					Background = titleBrush
				};
				ColumnDefinition col = new ColumnDefinition();
				col.Width = new GridLength(1d, GridUnitType.Star);
				grdTitleBar.ColumnDefinitions.Add(col);
				col = new ColumnDefinition();
				col.Width = new GridLength(20d);
				grdTitleBar.ColumnDefinitions.Add(col);
				col = new ColumnDefinition();
				col.Width = new GridLength(20d);
				grdTitleBar.ColumnDefinitions.Add(col);

				// show card type
				TextBlock lblCardType = new TextBlock()
				{
					Foreground = Brushes.White,
					FontSize = 10d,
					Padding = new Thickness(3, 0, 0, 0),
					FontWeight = FontWeights.Bold,
					Text = CardData.CType.Name
				};
				grdTitleBar.Children.Add(lblCardType);

				// archive button
				Button btnArchive = new Button()
				{
					BorderThickness = new Thickness(0d),
					BorderBrush = Brushes.Transparent,
					Background = Brushes.Transparent,
					Foreground = Brushes.White,
					FontWeight = FontWeights.Bold,
					Content = "-"
				};
				btnArchive.Click += btnArchive_Click;
				Grid.SetColumn(btnArchive, 1);
				grdTitleBar.Children.Add(btnArchive);

				// delete button
				Button btnDeleteCard = new Button()
				{
					BorderThickness = new Thickness(0d),
					BorderBrush = Brushes.Transparent,
					Background = Brushes.Transparent,
					Foreground = Brushes.White,
					FontWeight = FontWeights.Bold,
					Content = "X"
				};
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
			int imageFieldIndex = 0;
			foreach (CardType ct in CardTypes)
			{
				foreach (CardTypeField f in ct.Fields)
				{
					switch (f.FieldType)
					{
						case DataType.Text:
							{
								// get height increase
								int heightIncrease = 0;
								string arrangementCardID = null;

								if (arrangementSettings != null)
								{
									heightIncrease = arrangementSettings.TextFields[textFieldIndex].HeightIncrease;
									arrangementCardID = arrangementSettings.ID;
								}

								TextField text = new TextField()
								{
									Path = this.Path,
									CardID = this.CardID,
									CardTypeFieldID = f.ID,
									ArrangementCardID = arrangementCardID,
									FieldIndex = fieldIndex,
									Value = (string)CardData.Fields[fieldIndex],
									ShowLabel = f.ShowLabel,
									LabelText = f.Name,
									HeightIncrease = heightIncrease
								};

								text.ValueChanged += TextField_ValueChanged;
								text.HeightChanged += TextField_HeightChanged;

								// if title...
								if (fieldIndex == 0 && CardData.CType.Context == CardTypeContext.Standalone)
									text.setAsTitle(titleBrush);

								text.refresh();
								stkMain.Children.Add(text);

								textFieldIndex++;
							}
							break;
						case DataType.Card:
							{
								CardField fCard = new CardField()
								{
									Path = this.Path,
									CardID = this.CardID,
									CardTypeFieldID = f.ID,
									ArrangementCardID = ((arrangementSettings == null) ? null : arrangementSettings.ID),
									FieldIndex = fieldIndex,
									Value = (string)CardData.Fields[fieldIndex],
									ShowLabel = f.ShowLabel,
									LabelText = f.Name,
									FilterCardTypes = (string.IsNullOrEmpty(f.RefCardTypeID) ? null : CardManager.getCardTypeDescendents(f.RefCardTypeID, Path, ref userMessage))
								};

								fCard.ValueChanged += CardField_ValueChanged;
								fCard.refresh(ref userMessage);
								stkMain.Children.Add(fCard);

								cardFieldIndex++;
							}
							break;
						case DataType.List:
							{
								ListField list = new ListField()
								{
									Path = this.Path,
									CardID = this.CardID,
									CardTypeFieldID = f.ID,
									ArrangementCardID = ((arrangementSettings == null) ? null : arrangementSettings.ID),
									FieldIndex = fieldIndex,
									Value = (List<Card>)CardData.Fields[fieldIndex],
									ListType = f.ListType,
									LabelText = f.Name
								};

								list.refresh(arrangementSettings, ref listFieldIndex, ref userMessage);

								stkMain.Children.Add(list);
							}
							break;
						case DataType.Image:
							{
								ImageField image = new ImageField()
								{
									Path = this.Path,
									CardID = this.CardID,
									CardTypeFieldID = f.ID,
									FieldIndex = fieldIndex,
									Value = (string)CardData.Fields[fieldIndex],
									ShowLabel = f.ShowLabel,
									LabelText = f.Name
								};

								image.Deleted += ImageField_Deleted;
								image.Added += ImageField_Added;
								image.refresh();
								stkMain.Children.Add(image);

								imageFieldIndex++;
							}
							break;
						default:
							MessageBox.Show("Unknown field type: " + f.FieldType.ToString());
							break;
					}

					fieldIndex++;
				}
			}
		}

		/// <summary>Updates the card field's value.</summary>
		private void CardField_ValueChanged(object sender, EventArgs e)
		{
			CardField cf = (CardField)sender;
			CardData.Fields[cf.FieldIndex] = cf.Value;
		}

		/// <summary>Update the text field's value.</summary>
		private void TextField_ValueChanged(object sender, EventArgs e)
		{
			TextField text = (TextField)sender;
			CardData.Fields[text.FieldIndex] = text.Value;
		}

		/// <summary>Update the text card's size.</summary>
		private void TextField_HeightChanged(object sender, EventArgs e)
		{
			MovedOrResized?.Invoke(this, EventArgs.Empty);

			//this.Height = double.NaN;
		}

		/// <summary>Updates the card size.</summary>
		private void ImageField_Added(object sender, EventArgs e)
		{
			this.UpdateLayout();
			MovedOrResized?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Update the value.</summary>
		private void ImageField_Deleted(object sender, EventArgs e)
		{
			CardData.Fields[((ImageField)sender).FieldIndex] = null;
		}

		/// <summary>Updates the arrangement card IDs of all fields.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public void updateFieldArrangementIDs(ref string userMessage)
		{
			List<string> ids = CardManager.getArrangementListCardIDs(ArrangementCardID, Path, ref userMessage);

			int itemIndex = 0;
			foreach (FrameworkElement ui in stkMain.Children)
			{
				if (ui.Tag == null)
					continue;

				switch ((DataType)ui.Tag)
				{
					case DataType.Text:
						((TextField)ui).ArrangementCardID = this.ArrangementCardID;
						break;
					case DataType.Card:
						((CardField)ui).ArrangementCardID = this.ArrangementCardID;
						break;
					case DataType.List:
						((ListField)ui).ArrangementCardID = this.ArrangementCardID;
						((ListField)ui).updateArrangementIDs(ids, ref itemIndex, ref userMessage);
						break;
					case DataType.Image:
						// do nothing
						break;
					default:
						userMessage += "Unknown data type: " + ((DataType)ui.Tag).ToString();
						break;
				}
			}
		}

		/// <summary>Archive the card.</summary>
		private void btnArchive_Click(object sender, RoutedEventArgs e)
		{
			((Canvas)this.Parent).Children.Remove(this);

			Archived?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Delete the card.</summary>
		private void btnDeleteCard_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			((Canvas)this.Parent).Children.Remove(this);

			CardManager.deleteCard(CardData.ID, Path, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
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

				MovedOrResized?.Invoke(this, EventArgs.Empty);

				return;
			}

			Point cursor = e.MouseDevice.GetPosition((UIElement)this.Parent);

			Canvas.SetLeft(this, cursor.X - dragOffset.X);
			Canvas.SetTop(this, cursor.Y - dragOffset.Y);
		}

		/// <summary>Prepare to resize the card to the left.</summary>
		private void rResizeL_MouseDown(object sender, MouseButtonEventArgs e)
		{
			prepMoveResize((Rectangle)sender, rResizeL_MouseMove, e);
		}

		/// <summary>Resize the card to the left.</summary>
		private void rResizeL_MouseMove(object sender, MouseEventArgs e)
		{
			Rectangle rResizeL = (Rectangle)sender;

			// stop resizing the card
			if (e.LeftButton == MouseButtonState.Released)
			{
				rResizeL.MouseMove -= rResizeL_MouseMove;
				Mouse.Capture(null);

				MovedOrResized?.Invoke(this, EventArgs.Empty);

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
			prepMoveResize((Rectangle)sender, rResizeR_MouseMove, e);
		}

		/// <summary>Resize the card to the right.</summary>
		private void rResizeR_MouseMove(object sender, MouseEventArgs e)
		{
			Rectangle rResizeR = (Rectangle)sender;

			// stop resizing the card
			if (e.LeftButton == MouseButtonState.Released)
			{
				rResizeR.MouseMove -= rResizeR_MouseMove;
				Mouse.Capture(null);

				MovedOrResized?.Invoke(this, EventArgs.Empty);

				return;
			}

			Point cursor = e.MouseDevice.GetPosition(rResizeR);
			this.Width = this.ActualWidth + cursor.X - dragOffset.X;
		}

		#endregion Move and Resize

		#endregion Methods
	}
}
