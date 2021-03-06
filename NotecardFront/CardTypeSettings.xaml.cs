﻿using NotecardLib;
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

		private CardType CurCardType;

		#endregion Members

		public CardTypeSettings()
		{
			InitializeComponent();

			// fill field type combo box
			Item<string>[] items = new Item<string>[]
				{
					new Item<string>("Text", ((int)DataType.Text).ToString()),
					new Item<string>("Card", ((int)DataType.Card).ToString()),
					new Item<string>("List", ((int)DataType.List).ToString()),
					new Item<string>("Image", ((int)DataType.Image).ToString()),
					new Item<string>("Checkbox", ((int)DataType.CheckBox).ToString())
				};
			cmbCardTypeFieldType.ItemsSource = items;

			// fill list field type combo box
			items = new Item<string>[]
				{
					new Item<string>("Text", ((int)DataType.Text).ToString()),
					new Item<string>("Card", ((int)DataType.Card).ToString()),
					new Item<string>("Image", ((int)DataType.Image).ToString()),
					new Item<string>("Checkbox", ((int)DataType.CheckBox).ToString())
				};
			cmbListFieldType.ItemsSource = items;

			// fill card type color combo box
			Item<int>[] iItems = new Item<int>[]
				{
					new Item<int>("Red", 175 * 16 * 16 * 16 * 16),
					new Item<int>("Orange", 100 * 16 * 16 + 200 * 16 * 16 * 16 * 16),
					new Item<int>("Yellow", 150 * 16 * 16 + 150 * 16 * 16 * 16 * 16),
					new Item<int>("Green", 128 * 16 * 16),
					new Item<int>("Blue", 255),
					new Item<int>("Purple", 150 + 150 * 16 * 16 * 16 * 16)
				};
			cmbCardTypeColor.ItemsSource = iItems;

			// the current card type
			CurCardType = null;

			// add drop shadow effect
			var shadow = new System.Windows.Media.Effects.DropShadowEffect();
			shadow.ShadowDepth = 10d;
			shadow.BlurRadius = 10d;
			shadow.Color = Color.FromRgb(200, 200, 200);
			this.Effect = shadow;
		}

		#region Methods

		private void refresh(ref string userMessage)
		{
			lstCardType.SelectedValue = null;
			refreshCardTypeLists(ref userMessage);
			if (lstCardType.Items.Count > 0)
				lstCardType.SelectedIndex = 0;
		}

		private void refreshCardTypeLists(ref string userMessage)
		{
			string selType = (string)lstCardType.SelectedValue;
			string selField = (string)lstCardTypeField.SelectedValue;

			List<string[]> results = CardManager.getCardTypeIDsAndNames(ref userMessage);

			Item<string>[] items = new Item<string>[results.Count];
			Item<string>[] itemsWNull = new Item<string>[results.Count + 1];

			itemsWNull[0] = new Item<string>("[Any Type]", string.Empty);

			for (int i = 0; i < items.Length; i++)
			{
				items[i] = new Item<string>(results[i][1], results[i][0]);
				itemsWNull[i + 1] = items[i];
			}

			lstCardType.ItemsSource = items;
			cmbCardTypeFieldCardType.ItemsSource = itemsWNull;
			cmbListFieldCardType.ItemsSource = itemsWNull;

			if (listBoxContains(lstCardType, selType))
				lstCardType.SelectedValue = selType;

			refreshCardTypeFieldList(ref userMessage);

			if (listBoxContains(lstCardTypeField, selField))
				lstCardTypeField.SelectedValue = selField;
		}

		private bool listBoxContains(ListBox lb, string value)
		{
			Item<string>[] items = (Item<string>[])lb.ItemsSource;

			if (items != null)
			{
				foreach (Item<string> i in items)
				{
					if (i.Value == value)
						return true;
				}
			}

			return false;
		}

		private void refreshCardTypeFieldList(ref string userMessage)
		{
			refreshCardTypeFieldList((string)lstCardType.SelectedValue, ref userMessage);
		}

		private void refreshCardTypeFieldList(string cardTypeID, ref string userMessage)
		{
			if (string.IsNullOrEmpty(cardTypeID))
			{
				lstCardTypeField.ItemsSource = new Item<string>[0];
			}
			else
			{
				string selField = (string)lstCardTypeField.SelectedValue;

				List<string[]> results = CardManager.getCardTypeFieldIDsAndNames(cardTypeID, ref userMessage);

				Item<string>[] items = new Item<string>[results.Count];

				for (int i = 0; i < items.Length; i++)
				{
					items[i] = new Item<string>(results[i][1], results[i][0]);
				}

				lstCardTypeField.ItemsSource = items;

				if (listBoxContains(lstCardTypeField, selField))
					lstCardTypeField.SelectedValue = selField;
			}
		}

		private void refreshCurCardType(ref string userMessage)
		{
			refreshCurCardType((string)lstCardType.SelectedValue, ref userMessage);
		}

		private void refreshCurCardType(string selValue, ref string userMessage)
		{
			CurCardType = string.IsNullOrEmpty(selValue)
				? null
				: CardManager.getCardType(selValue, ref userMessage);
		}

		private void refreshListFieldList(ref string userMessage)
		{
			refreshListFieldList((string)lstCardTypeField.SelectedValue, ref userMessage);
		}

		private void refreshListFieldList(string cardTypeFieldID, ref string userMessage)
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
				lstListField.ItemsSource = new Item<string>[0];
			}
			else
			{
				string selField = (string)lstListField.SelectedValue;

				List<string[]> results = CardManager.getCardTypeFieldIDsAndNames(curTypeField.RefCardTypeID, ref userMessage);

				Item<string>[] items = new Item<string>[results.Count];

				for (int i = 0; i < items.Length; i++)
				{
					items[i] = new Item<string>(results[i][1], results[i][0]);
				}

				lstListField.ItemsSource = items;

				if (listBoxContains(lstListField, selField))
					lstListField.SelectedValue = selField;
			}
		}

		private void showMessages(string userMessages)
		{
			if (!string.IsNullOrEmpty(userMessages))
				MessageBox.Show(userMessages);
		}

		#region Events

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			this.Visibility = Visibility.Collapsed;
			lstCardType.SelectedValue = null;
		}

		private void this_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			string userMessage = string.Empty;

			if ((bool)e.NewValue)
				refresh(ref userMessage);

			showMessages(userMessage);
		}

		#region Card Type

		private void btnAddCardType_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			string id = CardManager.newCardType(CardTypeContext.Standalone, ref userMessage);

			refreshCardTypeLists(ref userMessage);

			showMessages(userMessage);
		}

		private void btnRemCardType_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue))
			{
				CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeRemove), ref userMessage);

				refreshCardTypeLists(ref userMessage);
			}

			showMessages(userMessage);
		}

		private void lstCardType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string userMessage = string.Empty;

			string selValue = e.AddedItems.Count == 0 ? null : ((Item<string>)e.AddedItems[0]).Value;

			refreshCurCardType(selValue, ref userMessage);

			if (string.IsNullOrEmpty(selValue))
			{
				txtCardTypeName.Text = string.Empty;
				cmbCardTypeColor.SelectedValue = null;
				grdCardType.IsEnabled = false;
			}
			else
			{
				txtCardTypeName.Text = CurCardType.Name;
				cmbCardTypeColor.SelectedValue = CurCardType.Color;
				grdCardType.IsEnabled = true;
			}

			refreshCardTypeFieldList(selValue, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		private void txtCardTypeName_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue) && !string.IsNullOrEmpty(txtCardTypeName.Text) && txtCardTypeName.Text != CurCardType.Name)
			{
				CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeNameChange, txtCardTypeName.Text), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshCardTypeLists(ref userMessage);
			}

			showMessages(userMessage);
		}

		private void cmbCardTypeColor_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue) && (int)cmbCardTypeColor.SelectedValue != CurCardType.Color)
			{
				CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeColorChange, (int)cmbCardTypeColor.SelectedValue), ref userMessage);

				refreshCurCardType(ref userMessage);
			}

			showMessages(userMessage);
		}

		#endregion Card Type

		#region Card Type Field

		private void btnAddCardTypeField_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardType.SelectedValue))
			{
				CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldAdd), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshCardTypeFieldList(ref userMessage);
			}

			showMessages(userMessage);
		}

		private void btnRemCardTypeField_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && lstCardTypeField.SelectedIndex > 0)
			{
				CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldRemove, (string)lstCardTypeField.SelectedValue), ref userMessage);

				refreshCardTypeFieldList(ref userMessage);
			}

			showMessages(userMessage);
		}

		private void txtCardTypeFieldName_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && !string.IsNullOrEmpty(txtCardTypeFieldName.Text) && CurCardType.Fields[lstCardTypeField.SelectedIndex].Name != txtCardTypeFieldName.Text)
			{
				CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldNameChange, lstCardTypeField.SelectedValue, txtCardTypeFieldName.Text), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshCardTypeFieldList(ref userMessage);
			}

			showMessages(userMessage);
		}

		private void lstCardTypeField_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string userMessage = string.Empty;

			string selValue = e.AddedItems.Count == 0 ? null : ((Item<string>)e.AddedItems[0]).Value;

			if (string.IsNullOrEmpty(selValue))
			{
				txtCardTypeFieldName.Text = string.Empty;
				cmbCardTypeFieldType.SelectedValue = null;
				cmbCardTypeFieldCardType.SelectedValue = null;
				chkCardTypeFieldShowLabel.IsChecked = true;

				grdCardTypeField.IsEnabled = false;
			}
			else
			{
				CardTypeField ctf = CurCardType.Fields[lstCardTypeField.SelectedIndex];

				txtCardTypeFieldName.Text = ctf.Name;
				cmbCardTypeFieldType.SelectedValue = ((int)ctf.FieldType).ToString();
				cmbCardTypeFieldCardType.SelectedValue = (ctf.FieldType != DataType.Card) ? null : string.IsNullOrEmpty(ctf.RefCardTypeID) ? string.Empty : ctf.RefCardTypeID;
				cmbCardTypeFieldCardType.IsEnabled = ctf.FieldType == DataType.Card;
				chkCardTypeFieldShowLabel.IsChecked = ctf.ShowLabel;
				chkCardTypeFieldShowLabel.IsEnabled = ctf.FieldType != DataType.List;

				grdCardTypeField.IsEnabled = true;
			}

			refreshListFieldList(selValue, ref userMessage);

			showMessages(userMessage);
		}

		private void cmbCardTypeFieldType_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && !string.IsNullOrEmpty((string)cmbCardTypeFieldType.SelectedValue) && (int)CurCardType.Fields[lstCardTypeField.SelectedIndex].FieldType != int.Parse((string)cmbCardTypeFieldType.SelectedValue))
			{
				DataType fieldType = (DataType)int.Parse((string)cmbCardTypeFieldType.SelectedValue);
				CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldTypeChange, (string)lstCardTypeField.SelectedValue, fieldType), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshListFieldList(ref userMessage);

				if (fieldType == DataType.Card)
				{
					cmbCardTypeFieldCardType.IsEnabled = true;
					cmbCardTypeFieldCardType.SelectedIndex = 0;
				}
				else
				{
					cmbCardTypeFieldCardType.IsEnabled = false;
					cmbCardTypeFieldCardType.SelectedValue = null;
				}

				if (fieldType == DataType.List)
				{
					chkCardTypeFieldShowLabel.IsEnabled = false;
					chkCardTypeFieldShowLabel.IsChecked = true;
				}
				else
				{
					chkCardTypeFieldShowLabel.IsEnabled = true;
				}
			}

			showMessages(userMessage);
		}

		private void cmbCardTypeFieldCardType_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && cmbCardTypeFieldCardType.SelectedIndex >= 0)
			{
				CardTypeField field = CurCardType.Fields[lstCardTypeField.SelectedIndex];
				if (field.FieldType == DataType.Card && (!string.IsNullOrEmpty(field.RefCardTypeID) || !string.IsNullOrEmpty((string)cmbCardTypeFieldCardType.SelectedValue)) && field.RefCardTypeID != (string)cmbCardTypeFieldCardType.SelectedValue)
				{
					CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldCardTypeChange, (string)lstCardTypeField.SelectedValue, (string)cmbCardTypeFieldCardType.SelectedValue), ref userMessage);
					refreshCurCardType(ref userMessage);
				}
			}

			showMessages(userMessage);
		}

		private void chkCardTypeFieldShowLabel_CheckedChanged(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && chkCardTypeFieldShowLabel.IsFocused)
			{
				CardTypeField field = CurCardType.Fields[lstCardTypeField.SelectedIndex];

				if (field.ShowLabel != chkCardTypeFieldShowLabel.IsChecked)
				{
					CardManager.saveCardType((string)lstCardType.SelectedValue, new CardTypeChg(CardTypeChange.CardTypeFieldShowLabelChange, (string)lstCardTypeField.SelectedValue, chkCardTypeFieldShowLabel.IsChecked), ref userMessage);
					refreshCurCardType(ref userMessage);
				}
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		private void btnMoveCardTypeFieldDown_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (lstCardTypeField.SelectedIndex < lstCardTypeField.Items.Count - 1)
			{
				int selected = lstCardTypeField.SelectedIndex;

				string id1 = CurCardType.Fields[selected].ID;
				string id2 = CurCardType.Fields[selected + 1].ID;

				CardManager.saveCardType(CurCardType.ID, new CardTypeChg(CardTypeChange.CardTypeFieldSwap, id1, id2), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshCardTypeFieldList(ref userMessage);
				lstCardTypeField.SelectedIndex = selected + 1;
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		private void btnMoveCardTypeFieldUp_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (lstCardTypeField.SelectedIndex > 0)
			{
				int selected = lstCardTypeField.SelectedIndex;

				string id1 = CurCardType.Fields[selected - 1].ID;
				string id2 = CurCardType.Fields[selected].ID;

				CardManager.saveCardType(CurCardType.ID, new CardTypeChg(CardTypeChange.CardTypeFieldSwap, id1, id2), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshCardTypeFieldList(ref userMessage);
				lstCardTypeField.SelectedIndex = selected - 1;
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		#endregion Card Type Field

		#region List Field

		private void btnAddListField_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstCardTypeField.SelectedValue) && CurCardType.Fields[lstCardTypeField.SelectedIndex].FieldType == DataType.List)
			{
				string listTypeID = CurCardType.Fields[lstCardTypeField.SelectedIndex].RefCardTypeID;

				CardManager.saveCardType(listTypeID, new CardTypeChg(CardTypeChange.CardTypeFieldAdd), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshListFieldList(ref userMessage);
			}

			showMessages(userMessage);
		}

		private void btnRemListField_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstListField.SelectedValue))
			{
				CardManager.saveCardType(CurCardType.Fields[lstCardTypeField.SelectedIndex].RefCardTypeID, new CardTypeChg(CardTypeChange.CardTypeFieldRemove, (string)lstListField.SelectedValue), ref userMessage);

				refreshListFieldList(ref userMessage);
			}

			showMessages(userMessage);
		}

		private void lstListField_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			string userMessage = string.Empty;

			string selValue = e.AddedItems.Count == 0 ? null : ((Item<string>)e.AddedItems[0]).Value;

			if (string.IsNullOrEmpty(selValue))
			{
				txtListFieldName.Text = string.Empty;
				cmbListFieldType.SelectedValue = null;
				cmbListFieldCardType.SelectedValue = null;
				chkListFieldShowLabel.IsChecked = true;

				grdListField.IsEnabled = false;
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
				cmbListFieldCardType.IsEnabled = ctf.FieldType == DataType.Card;
				chkListFieldShowLabel.IsChecked = ctf.ShowLabel;

				grdListField.IsEnabled = true;
			}

			showMessages(userMessage);
		}

		private void txtListFieldName_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstListField.SelectedValue) && !string.IsNullOrEmpty(txtListFieldName.Text) && CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType.Fields[lstListField.SelectedIndex].Name != txtListFieldName.Text)
			{
				CardManager.saveCardType(CurCardType.Fields[lstCardTypeField.SelectedIndex].RefCardTypeID, new CardTypeChg(CardTypeChange.CardTypeFieldNameChange, lstListField.SelectedValue, txtListFieldName.Text), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshListFieldList(ref userMessage);
			}

			showMessages(userMessage);
		}

		private void cmbListFieldType_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstListField.SelectedValue) && !string.IsNullOrEmpty((string)cmbListFieldType.SelectedValue) && (int)CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType.Fields[lstListField.SelectedIndex].FieldType != int.Parse((string)cmbListFieldType.SelectedValue))
			{
				DataType fieldType = (DataType)int.Parse((string)cmbListFieldType.SelectedValue);
				CardManager.saveCardType(CurCardType.Fields[lstCardTypeField.SelectedIndex].RefCardTypeID, new CardTypeChg(CardTypeChange.CardTypeFieldTypeChange, (string)lstListField.SelectedValue, fieldType), ref userMessage);

				refreshCurCardType(ref userMessage);

				if (fieldType == DataType.Card)
				{
					cmbListFieldCardType.IsEnabled = true;
					cmbListFieldCardType.SelectedIndex = 0;
				}
				else
				{
					cmbListFieldCardType.IsEnabled = false;
					cmbListFieldCardType.SelectedValue = null;
				}
			}

			showMessages(userMessage);
		}

		private void cmbListFieldCardType_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstListField.SelectedValue) && cmbListFieldCardType.SelectedIndex >= 0)
			{
				CardType listType = CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType;
				CardTypeField field = listType.Fields[lstListField.SelectedIndex];
				if (field.FieldType == DataType.Card && (!string.IsNullOrEmpty(field.RefCardTypeID) || !string.IsNullOrEmpty((string)cmbListFieldCardType.SelectedValue)) && field.RefCardTypeID != (string)cmbListFieldCardType.SelectedValue)
				{
					CardManager.saveCardType(listType.ID, new CardTypeChg(CardTypeChange.CardTypeFieldCardTypeChange, (string)lstListField.SelectedValue, (string)cmbListFieldCardType.SelectedValue), ref userMessage);
					refreshCurCardType(ref userMessage);
				}
			}

			showMessages(userMessage);
		}

		private void chkListFieldShowLabel_CheckedChanged(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (!string.IsNullOrEmpty((string)lstListField.SelectedValue) && chkListFieldShowLabel.IsFocused)
			{
				CardType listType = CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType;
				CardTypeField field = listType.Fields[lstListField.SelectedIndex];

				if (field.ShowLabel != chkListFieldShowLabel.IsChecked)
				{
					CardManager.saveCardType(listType.ID, new CardTypeChg(CardTypeChange.CardTypeFieldShowLabelChange, field.ID, chkListFieldShowLabel.IsChecked), ref userMessage);
					refreshCurCardType(ref userMessage);
				}
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		private void btnMoveListFieldDown_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (lstListField.SelectedIndex < lstListField.Items.Count - 1)
			{
				int selected = lstListField.SelectedIndex;

				string id1 = CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType.Fields[selected].ID;
				string id2 = CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType.Fields[selected + 1].ID;

				CardManager.saveCardType(CurCardType.ID, new CardTypeChg(CardTypeChange.CardTypeFieldSwap, id1, id2), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshListFieldList(ref userMessage);
				lstListField.SelectedIndex = selected + 1;
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		private void btnMoveListFieldUp_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (lstListField.SelectedIndex > 0)
			{
				int selected = lstListField.SelectedIndex;

				string id1 = CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType.Fields[selected - 1].ID;
				string id2 = CurCardType.Fields[lstCardTypeField.SelectedIndex].ListType.Fields[selected].ID;

				CardManager.saveCardType(CurCardType.ID, new CardTypeChg(CardTypeChange.CardTypeFieldSwap, id1, id2), ref userMessage);

				refreshCurCardType(ref userMessage);
				refreshListFieldList(ref userMessage);
				lstListField.SelectedIndex = selected - 1;
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		#endregion List Field

		#endregion Events

		#endregion Methods
	}
}
