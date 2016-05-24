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
		public string CardID { get; set; }
		public string path { get; set; }
		private List<CardType> CardTypes;
		private Card CardData;
		private Point dragOffset;
		private bool dragging;

		public CardControl()
		{
			InitializeComponent();
		}

		public string refreshUI()
		{
			string errorMessage = string.Empty;

			errorMessage += CardManager.getCard(CardID, path, out CardData);
			errorMessage += CardManager.getCardTypeAncestry(CardData.CType, path, out CardTypes);

			stkMain.Children.RemoveRange(1, stkMain.Children.Count - 1);

			lblCardType.Content = CardData.CType.Name;

			int fieldIndex = 0;
			foreach (CardType ct in CardTypes)
			{
				foreach (CardTypeField f in ct.Fields)
				{
					DockPanel newPanel = new DockPanel();

					Label lbl = new Label();
					lbl.HorizontalAlignment = HorizontalAlignment.Right;
					lbl.Content = f.Name;
					lbl.VerticalAlignment = VerticalAlignment.Center;
					//newPanel.Children.Add(lbl);

					if (fieldIndex == 0)
					{
						newPanel.Background = Brushes.Green;
						lbl.Foreground = Brushes.White;
						lbl.FontWeight = FontWeights.Bold;
					}

					switch (f.FieldType)
					{
						case DataType.Text:
							{
								TextBox txt = new TextBox();
								DockPanel.SetDock(txt, Dock.Right);
								//txt.HorizontalAlignment = HorizontalAlignment.Right;
								txt.Height = 20d;
								txt.Width = 100d;
								txt.Text = (string)CardData.Fields[fieldIndex];
								txt.Tag = f.ID;
								txt.Margin = new Thickness(0d, 5d, 10d, 5d);
								txt.LostFocus += txtField_LostFocus;
								newPanel.Children.Add(txt);
							}
							break;
						case DataType.Card:
							{
								//newPanel.Orientation = Orientation.Horizontal;

								Label bob = new Label();
								bob.Content = "(Empty)";
								bob.Height = 20d;
								bob.Width = 100d;
								newPanel.Children.Add(bob);
							}
							break;
						case DataType.List:

							break;
						default:
							MessageBox.Show("Unknown field type: " + f.FieldType.ToString());
							break;
					}

					newPanel.Children.Add(lbl);
					stkMain.Children.Add(newPanel);

					fieldIndex++;
				}
			}

			return errorMessage;
		}

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

		public void txtField_LostFocus(object sender, RoutedEventArgs e)
		{
			TextBox txt = (TextBox)sender;

			int index = getFieldIndex((string)txt.Tag);
			if ((string)CardData.Fields[index] != txt.Text)
			{
				CardData.Fields[index] = txt.Text;
				CardManager.saveCardTextField(txt.Text, CardData.ID, (string)txt.Tag, path);
			}
		}

		private void stkMain_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Mouse.Capture(null);
			if (this.CaptureMouse())
			{
				dragging = true;
				Point mousePos = e.MouseDevice.GetPosition(this);
				dragOffset = mousePos;

				this.MouseMove += CardControl_MouseMove;
			}
		}

		private void CardControl_MouseMove(object sender, MouseEventArgs e)
		{
			if (!dragging)
				MessageBox.Show("not dragging, but still fired the mousemove event!");

			if (e.LeftButton == MouseButtonState.Released)
			{
				this.MouseMove -= CardControl_MouseMove;
				dragging = false;
				Mouse.Capture(null);

				return;
			}

			Point cursor = e.MouseDevice.GetPosition((UIElement)this.Parent);

			Canvas.SetLeft(this, cursor.X - dragOffset.X);
			Canvas.SetTop(this, cursor.Y - dragOffset.Y);
		}

		private void btnArchive_Click(object sender, RoutedEventArgs e)
		{
			((Canvas)this.Parent).Children.Remove(this);
		}
	}
}
