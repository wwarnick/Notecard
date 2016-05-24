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
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region Members

		string path;

		#endregion Members

		#region Constructors

		public MainWindow()
		{
			InitializeComponent();

			// TEMPORARY!!
			path = @"C:\Users\wwarnick\Desktop\newcardfile.sqlite";
		}

		#endregion Constructors

		#region Methods

		private void btnShowCardTypes_Click(object sender, RoutedEventArgs e)
		{
			lclCardTypeSettings.Visibility = Visibility.Visible;
		}

		#region Events

		private void lclCardTypeSettings_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!(bool)e.NewValue)
			{
				List<string[]> results;
				errorMessage += CardManager.getCardTypeIDsAndNames(path, out results);

				Item[] items = new Item[results.Count];
				for (int i = 0; i < items.Length; i++)
				{
					items[i] = new Item(results[i][1], results[i][0]);
				}

				cmbAddCardType.ItemsSource = items;

				for (int i = 0; i < pnlMain.Children.Count; i++)
				{
					CardControl c = (CardControl)pnlMain.Children[i];

					if (CardManager.cardExists(c.CardID, path, ref errorMessage))
					{
						c.refreshUI();
					}
					else
					{
						pnlMain.Children.RemoveAt(i);
						i--;
					}
				}
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void btnAddCard_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			Card card;
			errorMessage += CardManager.newCard((string)cmbAddCardType.SelectedValue, path, out card);

			errorMessage += openCard(card.ID);

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private string openCard(string cardID)
		{
			string errorMessage = string.Empty;

			CardControl c = new CardControl();
			c.CardID = cardID;
			c.path = path;
			Canvas.SetTop(c, 0d);
			Canvas.SetLeft(c, 0d);
			c.refreshUI();
			c.PreviewMouseDown += CardControl_PreviewMouseDown;
			pnlMain.Children.Add(c);

			return errorMessage;
		}

		private void CardControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			CardControl c = (CardControl)sender;
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

		#endregion Events

		#endregion Methods

		private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			string errorMessage = string.Empty;

			string[] ids = null;

			if (!string.IsNullOrEmpty(txtSearch.Text))
				errorMessage += CardManager.search(txtSearch.Text, path, out ids);

			if (ids == null || ids.Length == 0)
			{
				lstSearchResults.ItemsSource = new Item[0];
				popSearchResults.IsOpen = false;
			}
			else
			{
				string[] names;
				errorMessage += CardManager.getCardNames(ids, path, out names);

				Item[] items = new Item[ids.Length];

				for (int i = 0; i < ids.Length; i++)
				{
					items[i] = new Item(names[i], ids[i]);
				}

				lstSearchResults.ItemsSource = items;
				popSearchResults.IsOpen = true;
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void lstSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (e.AddedItems.Count > 0)
			{
				string id = ((Item)e.AddedItems[0]).Value;

				foreach (CardControl c in pnlMain.Children)
				{
					// if the card is already on the board
					if (c.CardID == id)
						return;
				}
				
				errorMessage += openCard(id);
			}

			popSearchResults.IsOpen = false;

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}
	}
}
