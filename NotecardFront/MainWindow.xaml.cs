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

		/// <summary>The dialog used to save a file.</summary>
		SaveFileDialog saveDialog;

		/// <summary>The dialog used to open a file.</summary>
		OpenFileDialog openDialog;

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the MainWindow class.</summary>
		public MainWindow()
		{
			InitializeComponent();

			string errorMessage = string.Empty;

			CardTypes = new Dictionary<string, CardType>();
			Ancestries = new Dictionary<string, List<CardType>>();
			saveDialog = new SaveFileDialog();
			openDialog = new OpenFileDialog();

			errorMessage += newFile();
			
			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		#endregion Constructors

		#region Methods

		/// <summary>Starts a new file.</summary>
		/// <returns>Any error messages.</returns>
		private string newFile()
		{
			string errorMessage = string.Empty;

			CardTypes.Clear();
			Ancestries.Clear();

			pnlMain.Children.Clear();

			errorMessage += clearCurrentDir();

			Path = @"current\newcardfile.sqlite";
			errorMessage += CardManager.createNewFile(Path);

			errorMessage += refreshArrangementList();
			errorMessage += refreshCards();

			return errorMessage;
		}

		/// <summary>Clears the current working directory.</summary>
		/// <returns>Any error messages.</returns>
		private string clearCurrentDir()
		{
			string errorMessage = string.Empty;

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
				errorMessage += ex.Message;
			}

			return errorMessage;
		}

		/// <summary>Fills lstArrangements.</summary>
		/// <returns>Any error messages.</returns>
		private string refreshArrangementList()
		{
			string errorMessage = string.Empty;

			List<string[]> results;
			errorMessage += CardManager.getArrangementIDsAndNames(Path, out results);

			Item<string>[] items = new Item<string>[results.Count];

			for (int i = 0; i < items.Length; i++)
			{
				items[i] = new Item<string>(results[i][1], results[i][0]);
			}

			lstArrangements.ItemsSource = items;

			return errorMessage;
		}

		/// <summary>Removes and re-adds all of the cards on the page as well as refreshing the droplists.</summary>
		/// <returns>Any error messages.</returns>
		private string refreshCards()
		{
			string errorMessage = string.Empty;

			List<string[]> results;
			errorMessage += CardManager.getCardTypeIDsAndNames(path, out results);

			CardType[] tempCardTypes = new CardType[results.Count];

			// get card types
			CardTypes.Clear();
			Item<string>[] items = new Item<string>[results.Count];
			for (int i = 0; i < items.Length; i++)
			{
				CardType ct;
				errorMessage += CardManager.getCardType(results[i][0], path, out ct);
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

			errorMessage += refreshArrangement();

			return errorMessage;
		}

		/// <summary>Clears and reloads the cards in the current arrangement.</summary>
		/// <returns>Any error messages.</returns>
		public string refreshArrangement()
		{
			string errorMessage = string.Empty;

			pnlMain.Children.Clear();

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				ArrangementCardStandalone[] cards;
				errorMessage += CardManager.getArrangement((string)lstArrangements.SelectedValue, Path, out cards);

				foreach (ArrangementCardStandalone c in cards)
				{
					errorMessage += openCard(c.CardID, c, false);
				}
			}

			return errorMessage;
		}

		/// <summary>Load an existing card onto the arrangement.</summary>
		/// <param name="cardID">The database ID of the card to load.</param>
		/// <param name="arrangementSettings">The card's settings for the current arrangement.</param>
		/// <param name="addToArrangement">Whether or not to add the card to the current arrangement. false if it's already part of it.</param>
		/// <returns>Any error messages.</returns>
		private string openCard(string cardID, ArrangementCardStandalone arrangementSettings, bool addToArrangement)
		{
			string errorMessage = string.Empty;

			string cardTypeID;
			errorMessage += CardManager.getCardCardTypeID(cardID, path, out cardTypeID);

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

			c.refreshUI(Ancestries[cardTypeID], arrangementSettings);
			c.PreviewMouseDown += CardControl_PreviewMouseDown;
			c.Archived += CardControl_Archived;
			c.MovedOrResized += CardControl_MovedOrResized;
			pnlMain.Children.Add(c);

			bringToFront(c);

			if (addToArrangement)
			{
				c.UpdateLayout();
				string arrID;
				errorMessage += CardManager.arrangementAddCard((string)lstArrangements.SelectedValue, cardID, 0, 0, (int)Math.Round(c.ActualWidth), Path, out arrID);
				c.ArrangementCardID = arrID;
				c.updateListItemArrangementIDs();
			}

			return errorMessage;
		}

		/// <summary>Brings a card to the front.</summary>
		/// <param name="c">The card.</param>
		private void bringToFront(CardControl c)
		{
			int oldZ = Canvas.GetZIndex(c);

			// if already at the front
			if (oldZ == pnlMain.Children.Count)
				return;

			Canvas.SetZIndex(c, pnlMain.Children.Count);

			foreach (UIElement ui in pnlMain.Children)
			{
				if (ui == c)
					continue;

				int z = Canvas.GetZIndex(ui);
				if (z > oldZ)
					Canvas.SetZIndex(ui, z - 1);
			}
		}

		#region Events

		/// <summary>Sets the position and size of the card in the current arrangement.</summary>
		private void CardControl_MovedOrResized(object sender, EventArgs e)
		{
			string errorMessage = string.Empty;

			CardControl card = (CardControl)sender;
			errorMessage += CardManager.setCardPosAndSize((string)lstArrangements.SelectedValue, (string)card.Tag, (int)Math.Round(Canvas.GetLeft(card)), (int)Math.Round(Canvas.GetTop(card)), (int)Math.Round(card.ActualWidth), Path);

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Removes the archived card from the current arrangement.</summary>
		private void CardControl_Archived(object sender, EventArgs e)
		{
			string errorMessage = string.Empty;
			CardControl c = (CardControl)sender;

			errorMessage += CardManager.arrangementRemoveCard((string)lstArrangements.SelectedValue, (string)c.Tag, Path);

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Shows the card type settings.</summary>
		private void btnShowCardTypes_Click(object sender, RoutedEventArgs e)
		{
			lclCardTypeSettings.Visibility = Visibility.Visible;
		}

		/// <summary>If the card type settings was hidden, then refresh the current arrangement.</summary>
		private void lclCardTypeSettings_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!(bool)e.NewValue)
				errorMessage += refreshCards();

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Add a new card to the arrangement.</summary>
		private void btnAddCard_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				string cardID;
				errorMessage += CardManager.newCard(Ancestries[(string)cmbAddCardType.SelectedValue], path, out cardID);

				errorMessage += openCard(cardID, null, true);
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Bring card to the front.</summary>
		private void CardControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			bringToFront((CardControl)sender);
		}

		/// <summary>Load the searched card onto the current arrangement.</summary>
		private void txtSearch_SelectionMade(UserControl sender, SearchBoxEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				foreach (CardControl c in pnlMain.Children)
				{
					// if the card is already on the board
					if (c.CardID == e.SelectedValue)
						return;
				}

				errorMessage += openCard(e.SelectedValue, null, true);
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Show the user the save dialog, and save the file to the selected path.</summary>
		private void btnSaveAs_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (saveDialog.ShowDialog() == true)
			{
				CardManager.vacuum(Path);

				try
				{
					ZipFile.CreateFromDirectory("current", saveDialog.FileName);
				}
				catch (Exception ex)
				{
					errorMessage += ex.Message;
				}
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Clears the page and starts a new file.</summary>
		private void btnNew_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			errorMessage += newFile();

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Opens an existing file.</summary>
		private void btnOpen_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (openDialog.ShowDialog() == true)
			{
				clearCurrentDir();
				CardTypes.Clear();
				Ancestries.Clear();
				pnlMain.Children.Clear();

				try
				{
					ZipFile.ExtractToDirectory(openDialog.FileName, "current");
				}
				catch (Exception ex)
				{
					errorMessage += ex.Message;
				}

				errorMessage += refreshArrangementList();
				errorMessage += refreshCards();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Loads the selected arrangement.</summary>
		private void lstArrangements_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue))
			{
				txtArrangementName.Text = ((Item<string>)lstArrangements.SelectedItem).Text;

				errorMessage += refreshArrangement();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Adds a new arrangement.</summary>
		private void btnAddArrangement_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			string id;
			string name;
			errorMessage += CardManager.addArrangement(null, Path, out id, out name);

			Item<string>[] items = (Item<string>[])lstArrangements.ItemsSource;
			Item<string>[] newItems = new Item<string>[items.Length + 1];
			items.CopyTo(newItems, 0);
			newItems[newItems.Length - 1] = new Item<string>(name, id);
			lstArrangements.ItemsSource = newItems;

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Removes the current arrangement.</summary>
		private void btnRemoveArrangement_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrWhiteSpace((string)lstArrangements.SelectedValue))
			{
				errorMessage += CardManager.removeArrangement((string)lstArrangements.SelectedValue, Path);
				errorMessage += refreshArrangementList();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		/// <summary>Changes the name of the current arrangement.</summary>
		private void txtArrangementName_LostFocus(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstArrangements.SelectedValue) && !string.IsNullOrEmpty(txtArrangementName.Text))
			{
				errorMessage += CardManager.arrangementChangeName((string)lstArrangements.SelectedValue, txtArrangementName.Text, Path);
				Item<string>[] items = (Item<string>[])lstArrangements.ItemsSource;
				items[lstArrangements.SelectedIndex].Text = txtArrangementName.Text;
				lstArrangements.Items.Refresh();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		#endregion Events

		#endregion Methods
	}
}
