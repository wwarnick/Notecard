using Microsoft.Win32;
using NotecardLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region Members

		/// <summary>The different ways to open a card.</summary>
		private enum CardOpenType { New, Existing, Refresh }

		/// <summary>The current version of NoteCard.</summary>
		private readonly string NoteCardVersion;

		/// <summary>The current file path.</summary>
		private string currentFilePath;

		/// <summary>The current file path.</summary>
		private string CurrentFilePath
		{
			get { return currentFilePath; }
			set
			{
				currentFilePath = value;

				string userMessage = string.Empty;
				FileName = string.IsNullOrEmpty(currentFilePath) ? string.Empty : currentFilePath.Substring(currentFilePath.LastIndexOf(@"\") + 1);
				this.Title = (string.IsNullOrEmpty(currentFilePath) ? string.Empty : (FileName + " - ")) + "NoteCard v" + NoteCardVersion;

				if (!string.IsNullOrEmpty(userMessage))
					MessageBox.Show(userMessage);
			}
		}

		/// <summary>The name of the current file.</summary>
		private string FileName { get; set; }

		/// <summary>The 'last modified' date of the current file when it was first opened or last saved.</summary>
		private DateTime CurrentFileOldLastModified { get; set; }

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the MainWindow class.</summary>
		public MainWindow()
		{
			InitializeComponent();

			string userMessage = string.Empty;

			((System.Collections.Specialized.INotifyCollectionChanged)lstCardTypes.Items).CollectionChanged += ItemsSource_Changed;

			NoteCardVersion = CardManager.getNoteCardVersion(ref userMessage);

			// start with an empty file
			newFile(ref userMessage);
			
			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		#endregion Constructors

		#region Methods

		/// <summary>Starts a new file.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void newFile(ref string userMessage)
		{
			pnlMain.Children.Clear();

			CurrentFilePath = null;
			clearCurrentDir(ref userMessage);

			CardManager.Path = @"current\newcardfile.sqlite";
			CardManager.createNewFile(ref userMessage);

			refreshOldLastModifiedDate(ref userMessage);

			refreshArrangementList(ref userMessage);
			refreshCards(ref userMessage);
			lstArrangements.SelectedIndex = 0;
		}

		/// <summary>Create the current directory.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void createCurrentDir(ref string userMessage)
		{
			try
			{
				var sec = new System.Security.AccessControl.DirectorySecurity();//Directory.GetAccessControl(path);
				var everyone = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
				sec.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(everyone, System.Security.AccessControl.FileSystemRights.Modify | System.Security.AccessControl.FileSystemRights.Synchronize, System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));

				Directory.CreateDirectory("current", sec);
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Clears the current working directory.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void clearCurrentDir(ref string userMessage)
		{
			// make sure it exists
			createCurrentDir(ref userMessage);

			try
			{
				// delete all files in it if it already exists
				DirectoryInfo di = new DirectoryInfo("current");

				foreach (FileInfo file in di.GetFiles())
				{
					file.Delete();
				}

				foreach (DirectoryInfo dir in di.GetDirectories())
				{
					dir.Delete(true);
				}
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Removes orphaned files from the current directory.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void cleanOrphanedFiles(ref string userMessage)
		{
			List<string> imageIDs = CardManager.getImageIDs(ref userMessage);

			// make sure it exists
			createCurrentDir(ref userMessage);

			try
			{
				// delete all orphaned files in it if it already exists
				DirectoryInfo di = new DirectoryInfo("current");

				foreach (FileInfo file in di.GetFiles())
				{
					if (!file.Name.EndsWith(".sqlite") && !imageIDs.Contains(file.Name))
						file.Delete();
				}
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Fills lstArrangements.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void refreshArrangementList(ref string userMessage)
		{
			List<string[]> results = CardManager.getArrangementIDsAndNames(ref userMessage);
			Item<string>[] items = new Item<string>[results.Count];

			for (int i = 0; i < items.Length; i++)
			{
				items[i] = new Item<string>(results[i][1], results[i][0]);
			}

			lstArrangements.ItemsSource = items;
		}

		/// <summary>Removes and re-adds all of the cards on the page as well as refreshing the droplists.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void refreshCards(ref string userMessage)
		{
			CardManager.refreshCardTypes(ref userMessage);
			CardManager.fillBlankCardTitles(ref userMessage);

			Item<string, SolidColorBrush>[] items = new Item<string, SolidColorBrush>[CardManager.CardTypes.Length];
			for (int i = 0; i < items.Length; i++)
			{
				CardType ct = CardManager.CardTypes[i];
				items[i] = new Item<string, SolidColorBrush>(ct.Name, ct.ID, new SolidColorBrush(Color.FromRgb(ct.ColorRed, ct.ColorGreen, ct.ColorBlue)));
			}

			lstCardTypes.ItemsSource = items;
			if (lstCardTypes.Items.Count > 0)
				lstCardTypes.SelectedIndex = 0;

			refreshArrangement(ref userMessage);
		}

		/// <summary>Clears and reloads the cards in the current arrangement.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public void refreshArrangement(ref string userMessage)
		{
			pnlMain.Children.Clear();

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				ArrangementCardStandalone[] cards = CardManager.getArrangement((string)lstArrangements.SelectedValue, CardManager.AncestryIDs, ref userMessage);

				foreach (ArrangementCardStandalone c in cards)
				{
					openCard(c.CardID, c, CardOpenType.Refresh, ref userMessage);
				}

				refreshLines(ref userMessage);
			}
		}

		/// <summary>Load an existing card onto the arrangement.</summary>
		/// <param name="cardID">The database ID of the card to load.</param>
		/// <param name="arrangementSettings">The card's settings for the current arrangement.</param>
		/// <param name="openType">The type of card load, whether it's a new card, an existing card, or already part of the arrangement.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The arrangement card ID</returns>
		private string openCard(string cardID, ArrangementCardStandalone arrangementSettings, CardOpenType openType, ref string userMessage)
		{
			string cardTypeID = CardManager.getCardCardTypeID(cardID, ref userMessage);

			CardControl c = new CardControl()
			{
				CardID = cardID,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};

			if (arrangementSettings != null)
			{
				c.Width = arrangementSettings.Width;
				c.Margin = new Thickness(arrangementSettings.X, arrangementSettings.Y, 0d, 0d);
			}
			else
			{
				c.Width = CardControl.DefaultWidth;
				c.Margin = new Thickness(0d);
			}

			// add to arrangement
			if (openType == CardOpenType.Existing || openType == CardOpenType.New)
			{
				string arrangementCardID = CardManager.arrangementAddCard((string)lstArrangements.SelectedValue, cardID, 0, 0, (int)Math.Round(CardControl.DefaultWidth), (openType != CardOpenType.New), ref userMessage);
				arrangementSettings = CardManager.getArrangementCard(arrangementCardID, CardManager.AncestryIDs, ref userMessage);
			}

			c.refreshUI(CardManager.Ancestries[cardTypeID], arrangementSettings, ref userMessage);
			c.PreviewMouseDown += CardControl_PreviewMouseDown;
			c.Archived += CardControl_Archived;
			c.MovedOrResized += CardControl_MovedOrResized;
			c.OpenCard += CardControl_OpenCard;
			pnlMain.Children.Add(c);

			c.UpdateLayout();
			bringToFront(c);

			return c.ArrangementCardID;
		}

		/// <summary>Brings an element to the front.</summary>
		/// <param name="el">The element.</param>
		private void bringToFront(UIElement el)
		{
			int oldZ = Panel.GetZIndex(el);

			// if already at the front
			if (oldZ == pnlMain.Children.Count)
				return;

			// move the element to the front
			Panel.SetZIndex(el, pnlMain.Children.Count);

			// if not in the order yet
			if (oldZ == 0)
				return;

			// move the other elements
			foreach (FrameworkElement ui in pnlMain.Children)
			{
				if (ui == el)
					continue;

				int z = Panel.GetZIndex(ui);
				if (z > oldZ)
					Panel.SetZIndex(ui, z - 1);
			}
		}

		/// <summary>Sends an element to the back.</summary>
		/// <param name="el">The element.</param>
		private void sendToBack(UIElement el)
		{
			int oldZ = Panel.GetZIndex(el);

			// if already at the back
			if (oldZ == 1)
				return;

			// move the element to the back
			Panel.SetZIndex(el, 1);

			// if not in the order yet
			if (oldZ == 0)
				oldZ = int.MaxValue;

			// move the other elements
			foreach (FrameworkElement ui in pnlMain.Children)
			{
				if (ui == el)
					continue;

				int z = Panel.GetZIndex(ui);
				if (z < oldZ)
					Panel.SetZIndex(ui, z + 1);
			}
		}

		/// <summary>Refreshes the lines connecting the cards.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void refreshLines(ref string userMessage)
		{
			return;

			List<string[]> connections = CardManager.getArrangementCardConnections((string)lstArrangements.SelectedValue, ref userMessage);

			// collect card positions
			Dictionary<string, Point> positions = new Dictionary<string, Point>();
			for (int i = 0; i < pnlMain.Children.Count; i++)
			{
				FrameworkElement ui = (FrameworkElement)pnlMain.Children[i];

				// remove lines
				if ((string)ui.Tag == "line")
				{
					pnlMain.Children.RemoveAt(i);
					i--;
					continue;
				}

				//positions.Add(((CardControl)ui).CardID, new Point(Canvas.GetLeft(ui) + ui.ActualWidth / 2d, Canvas.GetTop(ui) + ui.ActualHeight / 2d));
				positions.Add(((CardControl)ui).CardID, new Point(ui.Margin.Left + ui.ActualWidth / 2d, ui.Margin.Top + ui.ActualHeight / 2d));
			}

			foreach (string[] connection in connections)
			{
				Point point1 = positions[connection[0]];
				Point point2 = positions[connection[1]];

				double width = Math.Abs(point2.X - point1.X);
				double height = Math.Abs(point2.Y - point1.Y);

				double x1 = 0;
				double x2 = 0;
				double y1 = 0;
				double y2 = 0;

				if (point2.X > point1.X)
					x2 = width;
				else
					x1 = width;

				if (point2.Y > point1.Y)
					y2 = height;
				else
					y1 = height;

				Line line = new Line()
				{
					X1 = x1,
					Y1 = y1,
					X2 = x2,
					Y2 = y2,
					Stroke = Brushes.Black,
					StrokeThickness = 4,
					Tag = "line",
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top
				};

				//Canvas.SetLeft(line, Math.Min(point1.X, point2.X));
				//Canvas.SetTop(line, Math.Min(point1.Y, point2.Y));
				line.Margin = new Thickness(Math.Min(point1.X, point2.X), Math.Min(point1.Y, point2.Y), 0d, 0d);

				pnlMain.Children.Add(line);
				sendToBack(line);
			}
		}

		/// <summary>Loads an existing card onto the arrangement.</summary>
		/// <param name="cardID">The database ID of the card to load.</param>
		/// <param name="userMessage">Any user messages.</param>
		private void loadExistingCard(string cardID, ref string userMessage)
		{
			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				foreach (FrameworkElement ui in pnlMain.Children)
				{
					if ((string)ui.Tag == "line")
						continue;

					// if the card is already on the board
					if (((CardControl)ui).CardID == cardID)
						return;
				}

				openCard(cardID, null, CardOpenType.Existing, ref userMessage);
			}
		}

		/// <summary>Saves the current file.</summary>
		/// <param name="saveAs">If a new path should be selected.</param>
		/// <param name="userMessage">Any user messages.</param>
		private void save(bool saveAs, ref string userMessage)
		{
			bool cancel = false;

			// get new path
			if (saveAs || CurrentFilePath == null)
			{
				SaveFileDialog saveDialog = new SaveFileDialog();
				saveDialog.Filter = "NoteCard Files | *.crd";

				if (saveDialog.ShowDialog() == true)
					CurrentFilePath = saveDialog.FileName;
				else
					cancel = true;
			}

			// save
			if (!cancel)
			{
				CardManager.vacuum(ref userMessage);
				cleanOrphanedFiles(ref userMessage);

				try
				{
					// delete the file path if it already exists
					if (File.Exists(CurrentFilePath))
						File.Delete(CurrentFilePath);

					ZipFile.CreateFromDirectory("current", CurrentFilePath);
					refreshOldLastModifiedDate(ref userMessage);
				}
				catch (Exception ex)
				{
					userMessage += ex.Message;
				}
			}
		}

		/// <summary>Refreshes CurrentFileOldLastModified.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void refreshOldLastModifiedDate(ref string userMessage)
		{
			try
			{
				CurrentFileOldLastModified = File.GetLastWriteTimeUtc(CardManager.Path);
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Determines whether or not there are unsaved changes.</summary>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>Whether or not there are unsaved changes.</returns>
		private bool hasUnsavedChanges(ref string userMessage)
		{
			try
			{
				return File.GetLastWriteTimeUtc(CardManager.Path) != CurrentFileOldLastModified;
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}

			return false;
		}

		/// <summary>Add a new card to the arrangement.</summary>
		private void addCard(ref string userMessage)
		{
			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue) && !string.IsNullOrEmpty((string)lstCardTypes.SelectedValue))
			{
				string cardID = CardManager.newCard(CardManager.Ancestries[(string)lstCardTypes.SelectedValue], ref userMessage);
				string arrangementCardID = openCard(cardID, null, CardOpenType.New, ref userMessage);
			}
		}

		/// <summary>Shows a save confirmation dialog with Yes, No, and Cancel buttons.</summary>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The dialog result.</returns>
		private MessageBoxResult showSaveConfirmation(ref string userMessage)
		{
			MessageBoxResult result = MessageBox.Show(this, "Do you want to save your changes" + (string.IsNullOrEmpty(CurrentFilePath) ? string.Empty : (" to " + FileName)) + "?", "NoteCard", MessageBoxButton.YesNoCancel);

			if (result == MessageBoxResult.Yes)
				save(string.IsNullOrEmpty(currentFilePath), ref userMessage);

			return result;
		}

		#region Events

		/// <summary>Opens a card.</summary>
		private void CardControl_OpenCard(object sender, EventArgs<string> e)
		{
			string userMessage = string.Empty;

			loadExistingCard(e.Value, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Sets the position and size of the card in the current arrangement.</summary>
		private void CardControl_MovedOrResized(object sender, EventArgs e)
		{
			string userMessage = string.Empty;

			CardControl card = (CardControl)sender;
			CardManager.setCardPosAndSize((string)lstArrangements.SelectedValue, (string)card.Tag, (int)Math.Round(card.Margin.Left), (int)Math.Round(card.Margin.Top), (int)Math.Round(card.ActualWidth), ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Removes the archived card from the current arrangement.</summary>
		private void CardControl_Archived(object sender, EventArgs e)
		{
			string userMessage = string.Empty;
			CardControl c = (CardControl)sender;

			CardManager.arrangementRemoveCard((string)lstArrangements.SelectedValue, (string)c.Tag, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Shows the card type settings.</summary>
		private void btnShowCardTypes_Click(object sender, RoutedEventArgs e)
		{
			dckMain.IsEnabled = false;
			lclCardTypeSettings.Visibility = Visibility.Visible;
		}

		/// <summary>If the card type settings was hidden, then refresh the current arrangement.</summary>
		private void lclCardTypeSettings_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!(bool)e.NewValue)
			{
				refreshCards(ref userMessage);
				dckMain.IsEnabled = true;
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Add a new card to the arrangement.</summary>
		private void btnAddCard_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			addCard(ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Add a new card to the arrangement.</summary>
		private void lstCardTypes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			string userMessage = string.Empty;

			addCard(ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Bring card to the front.</summary>
		private void CardControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			CardControl c = (CardControl)sender;

			if (c.Highlight != CardControl.HighlightStatuses.Selected)
			{
				foreach (FrameworkElement f in pnlMain.Children)
				{
					if ((string)f.Tag == "line")
						continue;

					((CardControl)f).Highlight = CardControl.HighlightStatuses.None;
				}

				c.Highlight = CardControl.HighlightStatuses.Selected;
			}

			bringToFront(c);
		}

		/// <summary>Load the selected card onto the current arrangement.</summary>
		private void lstSearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			string userMessage = string.Empty;
			
			if (!string.IsNullOrEmpty((string)lstSearchResults.SelectedValue))
				loadExistingCard((string)lstSearchResults.SelectedValue, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Load the selected card onto the current arrangement.</summary>
		private void btnSearchLoadCard_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstSearchResults.SelectedValue))
				loadExistingCard((string)lstSearchResults.SelectedValue, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Fill the search sidebar with the search results.</summary>
		private void txtSearch_SearchPerformed(UserControl sender, SearchTextBoxEventArgs e)
		{
			lstSearchResults.ItemsSource = e.Results;
		}

		/// <summary>Save the file to the current path.</summary>
		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			save(false, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Show the user the save dialog, and save the file to the selected path.</summary>
		private void btnSaveAs_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			save(true, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Clears the page and starts a new file.</summary>
		private void btnNew_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			newFile(ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Opens an existing file.</summary>
		private void btnOpen_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!hasUnsavedChanges(ref userMessage) ||
				showSaveConfirmation(ref userMessage) != MessageBoxResult.Cancel)
			{
				OpenFileDialog openDialog = new OpenFileDialog();
				openDialog.Filter = "NoteCard Files | *.crd";

				if (openDialog.ShowDialog() == true)
				{
					CurrentFilePath = openDialog.FileName;

					clearCurrentDir(ref userMessage);
					pnlMain.Children.Clear();

					try
					{
						ZipFile.ExtractToDirectory(CurrentFilePath, "current");
						File.SetLastWriteTimeUtc(CardManager.Path, File.GetLastWriteTimeUtc(CurrentFilePath));
					}
					catch (Exception ex)
					{
						userMessage += ex.Message;
					}
					refreshOldLastModifiedDate(ref userMessage);

					CardManager.updateDbVersion(ref userMessage);
					refreshArrangementList(ref userMessage);
					refreshCards(ref userMessage);
					if (lstArrangements.Items.Count > 0)
						lstArrangements.SelectedIndex = 0;
				}
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Loads the selected arrangement.</summary>
		private void lstArrangements_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				txtArrangementName.Text = ((Item<string>)lstArrangements.SelectedItem).Text;

				refreshArrangement(ref userMessage);
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Adds a new arrangement.</summary>
		private void btnAddArrangement_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			string name;
			string id = CardManager.addArrangement(null, out name, ref userMessage);

			Item<string>[] items = (Item<string>[])lstArrangements.ItemsSource;
			Item<string>[] newItems = new Item<string>[items.Length + 1];
			items.CopyTo(newItems, 0);
			newItems[newItems.Length - 1] = new Item<string>(name, id);
			lstArrangements.ItemsSource = newItems;

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Removes the current arrangement.</summary>
		private void btnRemoveArrangement_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrWhiteSpace((string)lstArrangements.SelectedValue))
			{
				CardManager.removeArrangement((string)lstArrangements.SelectedValue, ref userMessage);
				refreshArrangementList(ref userMessage);
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Changes the name of the current arrangement.</summary>
		private void txtArrangementName_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue) && !string.IsNullOrEmpty(txtArrangementName.Text))
			{
				CardManager.arrangementChangeName((string)lstArrangements.SelectedValue, txtArrangementName.Text, ref userMessage);
				Item<string>[] items = (Item<string>[])lstArrangements.ItemsSource;
				items[lstArrangements.SelectedIndex].Text = txtArrangementName.Text;
				lstArrangements.Items.Refresh();
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Clears out the current file.</summary>
		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			string userMessage = string.Empty;

			if (hasUnsavedChanges(ref userMessage) &&
				showSaveConfirmation(ref userMessage) == MessageBoxResult.Cancel)
			{
				// don't exit the application
				e.Cancel = true;
			}

			if (!e.Cancel)
			{
				// close card type settings if visible
				if (lclCardTypeSettings.Visibility == Visibility.Visible)
					lclCardTypeSettings.Visibility = Visibility.Collapsed;

				// clear the current directory
				clearCurrentDir(ref userMessage);
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Sets the visibility of lblNoCardTypes.</summary>
		private void ItemsSource_Changed(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			lblNoCardTypes.Visibility = lstCardTypes.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		#endregion Events

		#endregion Methods
	}
}
