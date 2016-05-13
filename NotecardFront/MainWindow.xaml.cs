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

		private const string NewCardTypeName = "Card Type {0}";
		private readonly int NewCardTypeNameIndex;
		private readonly string NewCardTypeNameStart;
		private readonly string NewCardTypeNameEnd;
		private readonly string NewCardTypeFieldName = "Field {0}";
		private readonly int NewCardTypeFieldNameIndex;
		private readonly string NewCardTypeFieldNameStart;
		private readonly string NewCardTypeFieldNameEnd;
		private string path;

		#endregion Members

		#region Constructors

		public MainWindow()
		{
			InitializeComponent();
			
			// create new name templates
			NewCardTypeNameIndex = NewCardTypeName.IndexOf("{0}");
			NewCardTypeNameStart = NewCardTypeName.Substring(0, NewCardTypeNameIndex);
			NewCardTypeNameEnd = NewCardTypeName.Substring(NewCardTypeNameIndex + 3);

			NewCardTypeFieldNameIndex = NewCardTypeFieldName.IndexOf("{0}");
			NewCardTypeFieldNameStart = NewCardTypeFieldName.Substring(0, NewCardTypeFieldNameIndex);
			NewCardTypeFieldNameEnd = NewCardTypeFieldName.Substring(NewCardTypeFieldNameIndex + 3);

			// fill field type combo boxes
			Item[] items = new Item[3];
			items[0] = new Item("Text", ((int)DataType.Text).ToString());
			items[1] = new Item("Card", ((int)DataType.Card).ToString());
			items[2] = new Item("List", ((int)DataType.List).ToString());
			cmbCardTypeFieldType.ItemsSource = items;
			cmbListFieldType.ItemsSource = items;

			// !!TEMPORARY!!
			path = @"C:\Users\wwarnick\Desktop\newcardfile.sqlite";
			string errorMessage = CardManager.createNewFile(path);

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		#endregion Constructors

		#region Methods

		private void refreshCardTypeList()
		{
			string selType = (string)lstCardType.SelectedValue;
			string selField = (string)lstCardTypeField.SelectedValue;

			List<string[]> results;
			CardManager.getCardTypeIDsAndNames(path, out results);

			Item[] items = new Item[results.Count];

			for (int i = 0; i < items.Length; i++)
			{
				items[i] = new Item(results[i][1], results[i][0]);
			}

			lstCardType.ItemsSource = items;

			if (listBoxContains(lstCardType, selType))
				lstCardType.SelectedValue = selType;
			
			refreshCardTypeFieldList();

			if (listBoxContains(lstCardTypeField, selField))
				lstCardTypeField.SelectedValue = selField;
		}

		private bool listBoxContains(ListBox lb, string value)
		{
			Item[] items = (Item[])lb.ItemsSource;

			if (items != null)
			{
				foreach (Item i in items)
				{
					if (i.Value == value)
						return true;
				}
			}

			return false;
		}

		private void refreshCardTypeFieldList()
		{
			refreshCardTypeFieldList((string)lstCardType.SelectedValue);
		}

		private void refreshCardTypeFieldList(string cardTypeID)
		{
			if (cardTypeID == null)
			{
				lstCardTypeField.ItemsSource = new Item[0];
			}
			else
			{
				string selField = (string)lstCardTypeField.SelectedValue;

				List<string[]> results;
				CardManager.getCardTypeFieldIDsAndNames(cardTypeID, path, out results);

				Item[] items = new Item[results.Count];

				for (int i = 0; i < items.Length; i++)
				{
					items[i] = new Item(results[i][1], results[i][0]);
				}

				lstCardTypeField.ItemsSource = items;

				if (listBoxContains(lstCardTypeField, selField))
					lstCardTypeField.SelectedValue = selField;
			}
		}

		private string findNextName(List<string[]> names, string newName, string newNameStart, string newNameEnd, int newNameIndex)
		{
			int nameNum = 1;
			foreach (string[] r in names)
			{
				string name = r[1];

				int temp;
				if (name.StartsWith(newNameStart) && name.EndsWith(newNameEnd) && int.TryParse(name.Substring(newNameIndex, name.Length - newNameEnd.Length - newNameIndex), out temp) && temp >= nameNum)
					nameNum = temp + 1;
			}

			return string.Format(newName, nameNum.ToString());
		}

		#region Events

		private void btnAddCardType_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			List<string[]> results;
			errorMessage += CardManager.getCardTypeIDsAndNames(path, out results);

			CardType ct = new CardType()
			{
				Name = findNextName(results, NewCardTypeName, NewCardTypeNameStart, NewCardTypeNameEnd, NewCardTypeNameIndex),
				Context = CardTypeContext.Standalone
			};

			CardType result;
			CardManager.saveNewCardType(ct, path, out result);

			refreshCardTypeList();

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void btnRemCardType_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue))
			{
				errorMessage += CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeRemove), path);

				refreshCardTypeList();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void lstCardType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string errorMessage = string.Empty;

			string selValue = e.AddedItems.Count == 0 ? null : ((Item)e.AddedItems[0]).Value;

			if (selValue == null)
			{
				txtCardTypeName.Text = string.Empty;
			}
			else
			{
				CardType ct;
				errorMessage += CardManager.getCardType(selValue, path, out ct);

				txtCardTypeName.Text = ct.Name;
			}

			refreshCardTypeFieldList(selValue);

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void txtCardTypeName_LostFocus(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue) && !string.IsNullOrEmpty(txtCardTypeName.Text))
			{
				errorMessage += CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeNameChange, txtCardTypeName.Text), path);

				refreshCardTypeList();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void btnAddCardTypeField_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue))
			{
				List<string[]> results;
				errorMessage += CardManager.getCardTypeFieldIDsAndNames((string)lstCardType.SelectedValue, path, out results);

				CardTypeField ctf = new CardTypeField()
				{
					Name = findNextName(results, NewCardTypeFieldName, NewCardTypeFieldNameStart, NewCardTypeFieldNameEnd, NewCardTypeFieldNameIndex),
					FieldType = DataType.Text
				};

				CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldAdd, ctf), path);

				refreshCardTypeFieldList();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void txtCardTypeFieldName_LostFocus(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && !string.IsNullOrEmpty(txtCardTypeFieldName.Text))
			{
				errorMessage += CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldNameChange, lstCardTypeField.SelectedValue, txtCardTypeFieldName.Text), path);

				refreshCardTypeFieldList();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void lstCardTypeField_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string errorMessage = string.Empty;

			string selValue = e.AddedItems.Count == 0 ? null : ((Item)e.AddedItems[0]).Value;

			if (selValue == null)
			{
				txtCardTypeFieldName.Text = string.Empty;
			}
			else
			{
				CardTypeField ctf;
				errorMessage += CardManager.getCardTypeField(selValue, path, out ctf);

				txtCardTypeFieldName.Text = ctf.Name;
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		#endregion Events

		#endregion Methods
	}
}
