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

		/// <summary>The path to the current database.</summary>
		string path;

		/// <summary>The path to the current database.</summary>
		string Path
		{
			get { return path; }
			set
			{
				path = value;
				lclCardTypeSettings.Path = value;
				txtSearch.Path = value;
			}
		}

		/// <summary>The card types, organized by database ID.</summary>
		Dictionary<string, CardType> CardTypes;

		/// <summary>Each card type's ancestry.</summary>
		Dictionary<string, List<CardType>> Ancestries;

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the MainWindow class.</summary>
		public MainWindow()
		{
			InitializeComponent();

			string userMessage = string.Empty;

			CardTypes = new Dictionary<string, CardType>();
			Ancestries = new Dictionary<string, List<CardType>>();

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
			CardTypes.Clear();
			Ancestries.Clear();

			pnlMain.Children.Clear();

			clearCurrentDir(ref userMessage);

			Path = @"current\newcardfile.sqlite";
			CardManager.createNewFile(Path, ref userMessage);

			refreshArrangementList(ref userMessage);
			refreshCards(ref userMessage);
		}

		/// <summary>Clears the current working directory.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void clearCurrentDir(ref string userMessage)
		{
			try
			{
				// make sure it exists
				Directory.CreateDirectory("current");

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
			List<string> imageIDs = CardManager.getImageIDs(this.Path, ref userMessage);

			try
			{
				// make sure it exists
				Directory.CreateDirectory("current");

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
			List<string[]> results = CardManager.getArrangementIDsAndNames(Path, ref userMessage);
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
			List<string[]> results = CardManager.getCardTypeIDsAndNames(path, ref userMessage);

			CardType[] tempCardTypes = new CardType[results.Count];

			// get card types
			CardTypes.Clear();
			Item<string>[] items = new Item<string>[results.Count];
			for (int i = 0; i < items.Length; i++)
			{
				CardType ct = CardManager.getCardType(results[i][0], path, ref userMessage);
				CardTypes.Add(ct.ID, ct);

				tempCardTypes[i] = ct;

				items[i] = new Item<string>(ct.Name, ct.ID);
			}

			cmbAddCardType.ItemsSource = items;

			// fill Ancestries
			Ancestries.Clear();
			foreach (CardType ct in tempCardTypes)
			{
				List<CardType> ancestry = new List<CardType>() { ct };

				CardType temp = ct;
				while (!string.IsNullOrEmpty(temp.ParentID))
				{
					temp = CardTypes[temp.ParentID];
					ancestry.Add(temp);
				}

				ancestry.Reverse();
				Ancestries.Add(ct.ID, ancestry);
			}

			refreshArrangement(ref userMessage);
		}

		/// <summary>Clears and reloads the cards in the current arrangement.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public void refreshArrangement(ref string userMessage)
		{
			pnlMain.Children.Clear();

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				ArrangementCardStandalone[] cards = CardManager.getArrangement((string)lstArrangements.SelectedValue, Path, ref userMessage);

				foreach (ArrangementCardStandalone c in cards)
				{
					openCard(c.CardID, c, false, ref userMessage);
				}

				refreshLines(ref userMessage);
			}
		}

		/// <summary>Load an existing card onto the arrangement.</summary>
		/// <param name="cardID">The database ID of the card to load.</param>
		/// <param name="arrangementSettings">The card's settings for the current arrangement.</param>
		/// <param name="addToArrangement">Whether or not to add the card to the current arrangement. false if it's already part of it.</param>
		/// <param name="userMessage">Any user messages.</param>
		private void openCard(string cardID, ArrangementCardStandalone arrangementSettings, bool addToArrangement, ref string userMessage)
		{
			string cardTypeID = CardManager.getCardCardTypeID(cardID, path, ref userMessage);

			CardControl c = new CardControl()
			{
				CardID = cardID,
				Path = path
			};

			if (arrangementSettings != null)
			{
				c.Width = arrangementSettings.Width;

				Canvas.SetLeft(c, arrangementSettings.X);
				Canvas.SetTop(c, arrangementSettings.Y);
			}
			else
			{
				Canvas.SetLeft(c, 0);
				Canvas.SetTop(c, 0);
			}

			c.refreshUI(Ancestries[cardTypeID], arrangementSettings, ref userMessage);
			c.PreviewMouseDown += CardControl_PreviewMouseDown;
			c.Archived += CardControl_Archived;
			c.MovedOrResized += CardControl_MovedOrResized;
			pnlMain.Children.Add(c);

			c.UpdateLayout();
			bringToFront(c);

			if (addToArrangement)
			{
				c.ArrangementCardID = CardManager.arrangementAddCard((string)lstArrangements.SelectedValue, cardID, 0, 0, (int)Math.Round(c.ActualWidth), Path, ref userMessage);
				c.updateFieldArrangementIDs(ref userMessage);
			}
		}

		/// <summary>Brings an element to the front.</summary>
		/// <param name="el">The element.</param>
		private void bringToFront(UIElement el)
		{
			int oldZ = Canvas.GetZIndex(el);

			// if already at the front
			if (oldZ == pnlMain.Children.Count)
				return;

			// move the element to the front
			Canvas.SetZIndex(el, pnlMain.Children.Count);

			// if not in the order yet
			if (oldZ == 0)
				return;

			// move the other elements
			foreach (FrameworkElement ui in pnlMain.Children)
			{
				if (ui == el)
					continue;

				int z = Canvas.GetZIndex(ui);
				if (z > oldZ)
					Canvas.SetZIndex(ui, z - 1);
			}
		}

		/// <summary>Sends an element to the back.</summary>
		/// <param name="el">The element.</param>
		private void sendToBack(UIElement el)
		{
			int oldZ = Canvas.GetZIndex(el);

			// if already at the back
			if (oldZ == 1)
				return;

			// move the element to the back
			Canvas.SetZIndex(el, 1);

			// if not in the order yet
			if (oldZ == 0)
				oldZ = int.MaxValue;

			// move the other elements
			foreach (FrameworkElement ui in pnlMain.Children)
			{
				if (ui == el)
					continue;

				int z = Canvas.GetZIndex(ui);
				if (z < oldZ)
					Canvas.SetZIndex(ui, z + 1);
			}
		}

		/// <summary>Refreshes the lines connecting the cards.</summary>
		/// <param name="userMessage">Any user messages.</param>
		private void refreshLines(ref string userMessage)
		{
			return;

			List<string[]> connections = CardManager.getArrangementCardConnections((string)lstArrangements.SelectedValue, Path, ref userMessage);

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

				positions.Add(((CardControl)ui).CardID, new Point(Canvas.GetLeft(ui) + ui.ActualWidth / 2d, Canvas.GetTop(ui) + ui.ActualHeight / 2d));
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
					Tag = "line"
				};

				Canvas.SetLeft(line, Math.Min(point1.X, point2.X));
				Canvas.SetTop(line, Math.Min(point1.Y, point2.Y));

				pnlMain.Children.Add(line);
				sendToBack(line);
			}
		}

		#region Events

		/// <summary>Sets the position and size of the card in the current arrangement.</summary>
		private void CardControl_MovedOrResized(object sender, EventArgs e)
		{
			string userMessage = string.Empty;

			CardControl card = (CardControl)sender;
			CardManager.setCardPosAndSize((string)lstArrangements.SelectedValue, (string)card.Tag, (int)Math.Round(Canvas.GetLeft(card)), (int)Math.Round(Canvas.GetTop(card)), (int)Math.Round(card.ActualWidth), Path, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Removes the archived card from the current arrangement.</summary>
		private void CardControl_Archived(object sender, EventArgs e)
		{
			string userMessage = string.Empty;
			CardControl c = (CardControl)sender;

			CardManager.arrangementRemoveCard((string)lstArrangements.SelectedValue, (string)c.Tag, Path, ref userMessage);

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

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				string cardID = CardManager.newCard(Ancestries[(string)cmbAddCardType.SelectedValue], path, ref userMessage);

				openCard(cardID, null, true, ref userMessage);
			}

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

		/// <summary>Load the searched card onto the current arrangement.</summary>
		private void txtSearch_SelectionMade(UserControl sender, SearchBoxEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				foreach (FrameworkElement ui in pnlMain.Children)
				{
					if ((string)ui.Tag == "line")
						continue;

					// if the card is already on the board
					if (((CardControl)ui).CardID == e.SelectedValue)
						return;
				}

				openCard(e.SelectedValue, null, true, ref userMessage);
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Show the user the save dialog, and save the file to the selected path.</summary>
		private void btnSaveAs_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			SaveFileDialog saveDialog = new SaveFileDialog();
			saveDialog.Filter = "NoteCard Files | *.crd";


			if (saveDialog.ShowDialog() == true)
			{
				CardManager.vacuum(Path, ref userMessage);
				cleanOrphanedFiles(ref userMessage);

				try
				{
					ZipFile.CreateFromDirectory("current", saveDialog.FileName);
				}
				catch (Exception ex)
				{
					userMessage += ex.Message;
				}
			}

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

			OpenFileDialog openDialog = new OpenFileDialog();
			openDialog.Filter = "NoteCard Files | *.crd";

			if (openDialog.ShowDialog() == true)
			{
				clearCurrentDir(ref userMessage);
				CardTypes.Clear();
				Ancestries.Clear();
				pnlMain.Children.Clear();

				try
				{
					ZipFile.ExtractToDirectory(openDialog.FileName, "current");
				}
				catch (Exception ex)
				{
					userMessage += ex.Message;
				}

				refreshArrangementList(ref userMessage);
				refreshCards(ref userMessage);
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
			string id = CardManager.addArrangement(null, Path, out name, ref userMessage);

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
				CardManager.removeArrangement((string)lstArrangements.SelectedValue, Path, ref userMessage);
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
				CardManager.arrangementChangeName((string)lstArrangements.SelectedValue, txtArrangementName.Text, Path, ref userMessage);
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

			clearCurrentDir(ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		#endregion Events

		#endregion Methods
	}
}
