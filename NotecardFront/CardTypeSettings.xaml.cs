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
	/// Interaction logic for CardTypeSettings.xaml
	/// </summary>
	public partial class CardTypeSettings : UserControl
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
		private CardType CurCardType;
		StringBuilder userMessage = new StringBuilder();

		#endregion Members

		public CardTypeSettings()
		{
			InitializeComponent();

			// create new name templates
			NewCardTypeNameIndex = NewCardTypeName.IndexOf("{0}");
			NewCardTypeNameStart = NewCardTypeName.Substring(0, NewCardTypeNameIndex);
			NewCardTypeNameEnd = NewCardTypeName.Substring(NewCardTypeNameIndex + 3);

			NewCardTypeFieldNameIndex = NewCardTypeFieldName.IndexOf("{0}");
			NewCardTypeFieldNameStart = NewCardTypeFieldName.Substring(0, NewCardTypeFieldNameIndex);
			NewCardTypeFieldNameEnd = NewCardTypeFieldName.Substring(NewCardTypeFieldNameIndex + 3);

			// fill field type combo box
			Item[] items = new Item[3];
			items[0] = new Item("Text", ((int)DataType.Text).ToString());
			items[1] = new Item("Card", ((int)DataType.Card).ToString());
			items[2] = new Item("List", ((int)DataType.List).ToString());
			cmbCardTypeFieldType.ItemsSource = items;

			// fill list field type combo box
			items = new Item[2];
			items[0] = new Item("Text", ((int)DataType.Text).ToString());
			items[1] = new Item("Card", ((int)DataType.Card).ToString());
			cmbListFieldType.ItemsSource = items;

			// !!TEMPORARY!!
			path = @"C:\Users\wwarnick\Desktop\newcardfile.sqlite";
			string errorMessage = CardManager.createNewFile(path);

			// the current card type
			CurCardType = null;

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		#region Methods

		private void refreshCardTypeLists()
		{
			string selType = (string)lstCardType.SelectedValue;
			string selField = (string)lstCardTypeField.SelectedValue;

			List<string[]> results;
			CardManager.getCardTypeIDsAndNames(path, out results);

			Item[] items = new Item[results.Count];
			Item[] itemsWNull = new Item[results.Count + 1];

			itemsWNull[0] = new Item("[Any Type]", string.Empty);

			for (int i = 0; i < items.Length; i++)
			{
				items[i] = new Item(results[i][1], results[i][0]);
				itemsWNull[i + 1] = items[i];
			}

			lstCardType.ItemsSource = items;
			cmbCardTypeFieldCardType.ItemsSource = itemsWNull;
			cmbListFieldCardType.ItemsSource = itemsWNull;

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
			if (string.IsNullOrEmpty(cardTypeID))
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

		private void refreshCurCardType()
		{
			refreshCurCardType((string)lstCardType.SelectedValue);
		}

		private string refreshCurCardType(string selValue)
		{
			string errorMessage = string.Empty;

			if (string.IsNullOrEmpty(selValue))
				CurCardType = null;
			else
				errorMessage += CardManager.getCardType(selValue, path, out CurCardType);

			return errorMessage;
		}

		private void refreshListFieldList()
		{
			refreshListFieldList((string)lstCardTypeField.SelectedValue);
		}

		private void refreshListFieldList(string cardTypeFieldID)
		{
			// get selected card type field
			CardTypeField curTypeField = null;
			if (!string.IsNullOrEmpty(cardTypeFieldID))
			{
				foreach (CardTypeField f in CurCardType.Fields)
				{
					if (f.ID == cardTypeFieldID)
					{
						curTypeField = f;
						break;
					}
				}
			}

			if (curTypeField == null || curTypeField.FieldType != DataType.List)
			{
				lstListField.ItemsSource = new Item[0];
			}
			else
			{
				string selField = (string)lstListField.SelectedValue;



				List<string[]> results;
				CardManager.getCardTypeFieldIDsAndNames(curTypeField.RefCardTypeID, path, out results);

				Item[] items = new Item[results.Count];

				for (int i = 0; i < items.Length; i++)
				{
					items[i] = new Item(results[i][1], results[i][0]);
				}

				lstListField.ItemsSource = items;

				if (listBoxContains(lstListField, selField))
					lstListField.SelectedValue = selField;
			}
		}

		private void addMessage(string message)
		{
			if (!string.IsNullOrWhiteSpace(message))
				userMessage.AppendLine(((userMessage.Length > 0) ? "\n" : "") + message);
		}

		private void showMessages()
		{
			if (userMessage.Length > 0)
			{
				MessageBox.Show(userMessage.ToString());
				userMessage.Clear();
			}
		}

		private void refrshParentList()
		{
			refreshParentList((string)lstCardType.SelectedValue);
		}

		private string refreshParentList(string cardTypeID)
		{
			string errorMessage = string.Empty;

			if (string.IsNullOrEmpty(cardTypeID))
			{
				cmbCardTypeParent.ItemsSource = new Item[0];
			}
			else
			{
				List<string[]> results;
				errorMessage += CardManager.getAllButDescendents(cardTypeID, path, out results);
				Item[] items = new Item[results.Count + 1];
				items[0] = new Item("[None]", string.Empty);
				for (int i = 0; i < results.Count; i++)
				{
					items[i + 1] = new Item(results[i][1], results[i][0]);
				}
				cmbCardTypeParent.ItemsSource = items;
			}

			return errorMessage;
		}

		#region Events

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			this.Visibility = Visibility.Collapsed;
		}

		#region Card Type

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

			refreshCardTypeLists();

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void btnRemCardType_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue))
			{
				errorMessage += CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeRemove), path);

				refreshCardTypeLists();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void lstCardType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string errorMessage = string.Empty;

			string selValue = e.AddedItems.Count == 0 ? null : ((Item)e.AddedItems[0]).Value;

			refreshCurCardType(selValue);
			refreshParentList(selValue);

			if (string.IsNullOrEmpty(selValue))
			{
				txtCardTypeName.Text = string.Empty;
				cmbCardTypeParent.SelectedValue = null;
			}
			else
			{
				txtCardTypeName.Text = CurCardType.Name;
				cmbCardTypeParent.SelectedValue = string.IsNullOrEmpty(CurCardType.ParentID) ? string.Empty : CurCardType.ParentID;
			}

			refreshCardTypeFieldList(selValue);

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void txtCardTypeName_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue) && !string.IsNullOrEmpty(txtCardTypeName.Text) && txtCardTypeName.Text != CurCardType.Name)
			{
				addMessage(CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeNameChange, txtCardTypeName.Text), path));

				refreshCurCardType();
				refreshCardTypeLists();
			}

			showMessages();
		}

		private void cmbCardTypeParent_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue) && (string)cmbCardTypeParent.SelectedValue != CurCardType.ParentID)
			{
				addMessage(CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeParentChange, (string)cmbCardTypeParent.SelectedValue), path));

				refreshCurCardType();
			}

			showMessages();
		}

		#endregion Card Type

		#region Card Type Field

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

				refreshCurCardType();
				refreshCardTypeFieldList();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void btnRemCardTypeField_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue))
			{
				errorMessage += CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldRemove, (string)lstCardTypeField.SelectedValue), path);

				refreshCardTypeFieldList();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void txtCardTypeFieldName_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && !string.IsNullOrEmpty(txtCardTypeFieldName.Text) && CurCardType.Fields[lstCardTypeField.SelectedIndex].Name != txtCardTypeFieldName.Text)
			{
				addMessage(CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldNameChange, lstCardTypeField.SelectedValue, txtCardTypeFieldName.Text), path));

				refreshCurCardType();
				refreshCardTypeFieldList();
			}

			showMessages();
		}

		private void lstCardTypeField_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string selValue = e.AddedItems.Count == 0 ? null : ((Item)e.AddedItems[0]).Value;

			if (selValue == null)
			{
				txtCardTypeFieldName.Text = string.Empty;
				cmbCardTypeFieldType.SelectedValue = null;
				cmbCardTypeFieldCardType.SelectedValue = null;
			}
			else
			{
				CardTypeField ctf = CurCardType.Fields[lstCardTypeField.SelectedIndex];

				txtCardTypeFieldName.Text = ctf.Name;
				cmbCardTypeFieldType.SelectedValue = ((int)ctf.FieldType).ToString();
				cmbCardTypeFieldCardType.SelectedValue = (ctf.FieldType != DataType.Card) ? null : string.IsNullOrEmpty(ctf.RefCardTypeID) ? string.Empty : ctf.RefCardTypeID;
			}

			refreshListFieldList(selValue);

			showMessages();
		}

		private void cmbCardTypeFieldType_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && !string.IsNullOrEmpty((string)cmbCardTypeFieldType.SelectedValue) && (int)CurCardType.Fields[lstCardTypeField.SelectedIndex].FieldType != int.Parse((string)cmbCardTypeFieldType.SelectedValue))
			{
				addMessage(CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldTypeChange, (string)lstCardTypeField.SelectedValue, (DataType)int.Parse((string)cmbCardTypeFieldType.SelectedValue)), path));

				refreshCurCardType();
				refreshListFieldList();
			}

			showMessages();
		}

		private void cmbCardTypeFieldCardType_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && cmbCardTypeFieldCardType.SelectedIndex >= 0)
			{
				CardTypeField field = CurCardType.Fields[lstCardTypeField.SelectedIndex];
				if (field.FieldType == DataType.Card && (!string.IsNullOrEmpty(field.RefCardTypeID) || !string.IsNullOrEmpty((string)cmbCardTypeFieldCardType.SelectedValue)) && field.RefCardTypeID != (string)cmbCardTypeFieldCardType.SelectedValue)
				{
					addMessage(CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldCardTypeChange, (string)lstCardTypeField.SelectedValue, (string)cmbCardTypeFieldCardType.SelectedValue), path));
					refreshCurCardType();
				}
			}

			showMessages();
		}

		#endregion Card Type Field

		#region List Field

		private void btnAddListField_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && CurCardType.Fields[lstCardTypeField.SelectedIndex].FieldType == DataType.List)
			{
				string listTypeID = CurCardType.Fields[lstCardTypeField.SelectedIndex].RefCardTypeID;

				List<string[]> results;
				errorMessage += CardManager.getCardTypeFieldIDsAndNames(listTypeID, path, out results);

				CardTypeField ctf = new CardTypeField()
				{
					Name = findNextName(results, NewCardTypeFieldName, NewCardTypeFieldNameStart, NewCardTypeFieldNameEnd, NewCardTypeFieldNameIndex),
					FieldType = DataType.Text
				};

				CardManager.saveCardType(listTypeID, new CardTypeChg(CardTypeChange.CardTypeFieldAdd, ctf), path);

				refreshCurCardType();
				refreshListFieldList();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void btnRemListField_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstListField.SelectedValue))
			{
				errorMessage += CardManager.saveCardType(CurCardType.Fields[lstCardTypeField.SelectedIndex].RefCardTypeID, new CardTypeChg(CardTypeChange.CardTypeFieldRemove, (string)lstListField.SelectedValue), path);

				refreshListFieldList();
			}

			if (!string.IsNullOrEmpty(errorMessage))
				MessageBox.Show(errorMessage);
		}

		private void lstListField_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string selValue = e.AddedItems.Count == 0 ? null : ((Item)e.AddedItems[0]).Value;

			if (selValue == null)
			{
				txtListFieldName.Text = string.Empty;
				cmbListFieldType.SelectedValue = null;
				cmbListFieldCardType.SelectedValue = null;
			}
			else
			{
				CardTypeField ctf = null;

				// get list field
				foreach (CardTypeField f in CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType.Fields)
				{
					if (f.ID == selValue)
					{
						ctf = f;
						break;
					}
				}

				txtListFieldName.Text = ctf.Name;
				cmbListFieldType.SelectedValue = ((int)ctf.FieldType).ToString();
				cmbListFieldCardType.SelectedValue = (ctf.FieldType != DataType.Card) ? null : string.IsNullOrEmpty(ctf.RefCardTypeID) ? string.Empty : ctf.RefCardTypeID;
			}

			showMessages();
		}

		private void txtListFieldName_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty((string)lstListField.SelectedValue) && !string.IsNullOrEmpty(txtListFieldName.Text) && CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType.Fields[lstListField.SelectedIndex].Name != txtListFieldName.Text)
			{
				addMessage(CardManager.saveCardType(CurCardType.Fields[lstCardTypeField.SelectedIndex].RefCardTypeID, new CardTypeChg(CardTypeChange.CardTypeFieldNameChange, lstListField.SelectedValue, txtListFieldName.Text), path));

				refreshCurCardType();
				refreshListFieldList();
			}

			showMessages();
		}

		private void cmbListFieldType_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty((string)lstListField.SelectedValue) && !string.IsNullOrEmpty((string)cmbListFieldType.SelectedValue) && (int)CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType.Fields[lstListField.SelectedIndex].FieldType != int.Parse((string)cmbListFieldType.SelectedValue))
			{
				addMessage(CardManager.saveCardType(CurCardType.Fields[lstCardTypeField.SelectedIndex].RefCardTypeID, new CardTypeChg(CardTypeChange.CardTypeFieldTypeChange, (string)lstListField.SelectedValue, (DataType)int.Parse((string)cmbListFieldType.SelectedValue)), path));

				refreshCurCardType();
			}

			showMessages();
		}

		private void cmbListFieldCardType_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty((string)lstListField.SelectedValue) && cmbListFieldCardType.SelectedIndex >= 0)
			{
				CardType listType = CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType;
				CardTypeField field = listType.Fields[lstListField.SelectedIndex];
				if (field.FieldType == DataType.Card && (!string.IsNullOrEmpty(field.RefCardTypeID) || !string.IsNullOrEmpty((string)cmbListFieldCardType.SelectedValue)) && field.RefCardTypeID != (string)cmbListFieldCardType.SelectedValue)
				{
					addMessage(CardManager.saveCardType(listType.ID, new CardTypeChg(CardTypeChange.CardTypeFieldCardTypeChange, (string)lstListField.SelectedValue, (string)cmbListFieldCardType.SelectedValue), path));
					refreshCurCardType();
				}
			}
		}

		#endregion List Field

		#endregion Events

		#endregion Methods
	}
}
