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
				// prepare title bar
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

				// get title bar color
				titleBrush = new SolidColorBrush(Color.FromRgb(CardData.CType.ColorRed, CardData.CType.ColorGreen, CardData.CType.ColorBlue));

				// show card type
				TextBlock lblCardType = new TextBlock()
				{
					Background = titleBrush,
					Foreground = Brushes.White,
					FontSize = 10d,
					Padding = new Thickness(3, 0, 0, 0),
					Text = CardData.CType.Name
				};
				grdTitleBar.Children.Add(lblCardType);

				// archive button
				Button btnArchive = new Button();
				btnArchive.Content = "-";
				btnArchive.Click += btnArchive_Click;
				Grid.SetColumn(btnArchive, 1);
				grdTitleBar.Children.Add(btnArchive);

				// delete button
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
			int imageFieldIndex = 0;
			foreach (CardType ct in CardTypes)
			{
				foreach (CardTypeField f in ct.Fields)
				{
					/*Panel newPanel = null;

					TextBlock lbl = new TextBlock();
					lbl.Text = f.Name;
					lbl.VerticalAlignment = VerticalAlignment.Center;
					lbl.Margin = new Thickness(5d);*/

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
									LabelText = f.Name,
									HeightIncrease = heightIncrease
								};

								text.ValueChanged += TextField_ValueChanged;
								text.HeightChanged += TextField_HeightChanged;

								// if title...
								if (fieldIndex == 0 && CardData.CType.Context == CardTypeContext.Standalone)
									text.setAsTitle(titleBrush);

								text.refresh();

								/*/ create controls
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
								newPanel.Children.Add(textFieldResize);*/

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
									LabelText = f.Name,
									FilterCardTypes = (string.IsNullOrEmpty(f.RefCardTypeID) ? null : CardManager.getCardTypeDescendents(f.RefCardTypeID, Path, ref userMessage))
								};

								fCard.ValueChanged += CardField_ValueChanged;

								fCard.refresh(ref userMessage);


								/*newPanel = new DockPanel();
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
									txtCardSearch.CardTypes = CardManager.getCardTypeDescendents(f.RefCardTypeID, Path, ref userMessage);

								newPanel.Children.Add(txtCardSearch);*/

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


								/*newPanel = new DockPanel();
								newPanel.Tag = DataType.List;
								DockPanel.SetDock(lbl, Dock.Top);
								newPanel.Children.Add(lbl);

								List<Card> items = (List<Card>)CardData.Fields[fieldIndex];
								StackPanel pnlList = new StackPanel();

								foreach (Card c in items)
								{
									ArrangementCardList l = (arrangementSettings == null) ? null : ((ArrangementCardStandalone)arrangementSettings).ListItems[listFieldIndex];
									CardControl item = newListItem(c.ID, f.ListType, l, ref userMessage);
									item.ArrangementCardID = CardManager.getArrangementListCardID(ArrangementCardID, item.CardID, Path, ref userMessage);
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

								newPanel.Children.Add(pnlList);*/

								stkMain.Children.Add(list);

								//listFieldIndex++;
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
									LabelText = f.Name
								};

								image.Deleted += ImageField_Deleted;
								image.Added += ImageField_Added;

								image.refresh();

								/*newPanel = new DockPanel();
								newPanel.Tag = DataType.Image;
								DockPanel.SetDock(lbl, Dock.Left);
								newPanel.Children.Add(lbl);

								Image image = new Image()
								{
									MaxHeight = 200,
									MaxWidth = 200,
									Tag = f
								};
								newPanel.Background = Brushes.Transparent;
								newPanel.PreviewDragEnter += image_PreviewDragEnter;
								newPanel.PreviewDragOver += image_PreviewDragOver;
								newPanel.PreviewDrop += image_PreviewDrop;
								newPanel.AllowDrop = true;

								newPanel.MouseEnter += image_MouseEnter;
								newPanel.MouseLeave += image_MouseLeave;

								// delete button
								Button btnDelImage = new Button()
								{
									HorizontalAlignment = HorizontalAlignment.Right,
									VerticalAlignment = VerticalAlignment.Top,
									Width = 20,
									Height = 20,
									Content = "X",
									Tag = f
								};
								btnDelImage.Click += btnDelImage_Click;

								string imgID = (string)CardData.Fields[fieldIndex];

								// load current image
								if (!string.IsNullOrEmpty(imgID))
								{
									BitmapImage bmp = new BitmapImage();
									bmp.BeginInit();
									bmp.UriSource = new Uri(@"current\" + imgID, UriKind.Relative);
									bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
									bmp.CacheOption = BitmapCacheOption.OnLoad;
									bmp.EndInit();
									image.Source = bmp;
									btnDelImage.Visibility = Visibility.Hidden;
								}
								else
								{
									btnDelImage.Visibility = Visibility.Collapsed;
								}

								Grid grdImage = new Grid()
								{
									HorizontalAlignment = HorizontalAlignment.Center
								};
								grdImage.Children.Add(image);
								grdImage.Children.Add(btnDelImage);
								newPanel.Children.Add(grdImage);*/

								stkMain.Children.Add(image);

								imageFieldIndex++;
							}
							break;
						default:
							MessageBox.Show("Unknown field type: " + f.FieldType.ToString());
							break;
					}

					/*/ title field
					if (fieldIndex == 0 && CardData.CType.Context == CardTypeContext.Standalone)
					{
						newPanel.Background = titleBrush;
						lbl.Foreground = Brushes.White;
						lbl.FontWeight = FontWeights.Bold;
					}

					stkMain.Children.Add(newPanel);*/

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
			MovedOrResized?.Invoke(this, null);

			//this.Height = double.NaN;
		}

		/// <summary>Updates the card size.</summary>
		private void ImageField_Added(object sender, EventArgs e)
		{
			this.UpdateLayout();
			MovedOrResized?.Invoke(this, null);
		}

		/// <summary>Update the value.</summary>
		private void ImageField_Deleted(object sender, EventArgs e)
		{
			CardData.Fields[((ImageField)sender).FieldIndex] = null;
		}

		/*private void btnDelImage_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			Button btn = (Button)sender;
			CardTypeField field = (CardTypeField)btn.Tag;
			CardManager.removeCardImage(CardID, field.ID, Path, ref userMessage);

			for (int i = 0; i < CardData.CType.Fields.Count; i++)
			{
				if (CardData.CType.Fields[i].ID == field.ID)
				{
					System.IO.FileInfo imgFile = new System.IO.FileInfo(@"current\" + (string)CardData.Fields[i]);
					imgFile.Delete();
					CardData.Fields[i] = null;
					Panel imgPnl = (Panel)((Panel)stkMain.Children[i + 1]).Children[1];
					((Image)imgPnl.Children[0]).Source = null;
					((Button)imgPnl.Children[1]).Visibility = Visibility.Collapsed;
				}
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		private void image_MouseLeave(object sender, MouseEventArgs e)
		{
			Button btn = (Button)((Panel)((Panel)sender).Children[1]).Children[1];
			if (btn.Visibility == Visibility.Visible)
				btn.Visibility = Visibility.Hidden;
		}

		private void image_MouseEnter(object sender, MouseEventArgs e)
		{
			Button btn = (Button)((Panel)((Panel)sender).Children[1]).Children[1];
			if (btn.Visibility == Visibility.Hidden)
				btn.Visibility = Visibility.Visible;
		}

		/// <summary>Tells whether a drag is valid or not.</summary>
		private void image_PreviewDragEnter(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string imgPath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

				string extension = imgPath.Substring(imgPath.LastIndexOf('.') + 1).ToLower();

				if (extension == "bmp" || extension == "jpg" || extension == "jpeg" || extension == "png" || extension == "gif" || extension == "tiff" || extension == "ico")
					e.Effects = DragDropEffects.Move;
			}

			e.Handled = true;
		}

		/// <summary>Tells whether a drag is valid or not.</summary>
		private void image_PreviewDragOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string imgPath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

				string extension = imgPath.Substring(imgPath.LastIndexOf('.') + 1).ToLower();

				if (extension == "bmp" || extension == "jpg" || extension == "jpeg" || extension == "png" || extension == "gif" || extension == "tiff" || extension == "ico")
					e.Effects = DragDropEffects.Move;
			}

			e.Handled = true;
		}

		/// <summary>Loads the dragged image.</summary>
		private void image_PreviewDrop(object sender, DragEventArgs e)
		{
			string userMessage = string.Empty;

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string imgPath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

				string extension = imgPath.Substring(imgPath.LastIndexOf('.') + 1).ToLower();

				if (extension == "bmp" || extension == "jpg" || extension == "jpeg" || extension == "png" || extension == "gif" || extension == "tiff" || extension == "ico")
				{
					Image img = (Image)((Panel)((Panel)sender).Children[1]).Children[0];
					CardTypeField field = (CardTypeField)img.Tag;

					int fieldIndex = 0;
					while (CardData.CType.Fields[fieldIndex].ID != field.ID)
					{
						fieldIndex++;
					}

					string imgID = (string)CardData.Fields[fieldIndex];

					if (string.IsNullOrEmpty(imgID))
					{
						CardData.Fields[fieldIndex] = imgID = CardManager.addCardImage(CardID, field.ID, Path, ref userMessage);
						((Button)((Panel)((Panel)sender).Children[1]).Children[1]).Visibility = Visibility.Visible;
					}

					// resize image
					var photoDecoder = BitmapDecoder.Create(
						new Uri(imgPath),
						BitmapCreateOptions.PreservePixelFormat,
						BitmapCacheOption.None);
					var photo = photoDecoder.Frames[0];

					double mod = Math.Min(1d, Math.Min(img.MaxWidth / photo.PixelWidth, img.MaxHeight / photo.PixelHeight));

					var target = new TransformedBitmap(
						photo,
						new ScaleTransform(
							mod,
							mod,
							0, 0));
					var thumbnail = BitmapFrame.Create(target);
					
					using (var fileStream = new System.IO.FileStream(@"current\" + imgID, System.IO.FileMode.Create))
					{
						BitmapEncoder encoder = new PngBitmapEncoder();
						encoder.Frames.Add(thumbnail);
						encoder.Save(fileStream);
					}

					// display image
					img.Source = thumbnail;

					// update card size
					this.UpdateLayout();
					MovedOrResized?.Invoke(this, null);
				}
			}

			e.Handled = true;

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}*/

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

			Archived?.Invoke(this, null);
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

		#endregion Move and Resize

		#endregion Methods
	}
}
