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
	/// Interaction logic for TextField.xaml
	/// </summary>
	public partial class TextField : UserControl
	{
		/// <summary>The path of the current database.</summary>
		public string Path { get; set; }

		/// <summary>The database ID of the owning card.</summary>
		public string CardID { get; set; }

		/// <summary>The database ID of the card type field.</summary>
		public string CardTypeFieldID { get; set; }

		/// <summary>The database ID of the arrangement card.</summary>
		public string ArrangementCardID { get; set; }

		/// <summary>The index of the field in the card type.</summary>
		public int FieldIndex { get; set; }

		/// <summary>The text contained in the text field.</summary>
		public string Value { get; set; }

		/// <summary>The text to show in the label.</summary>
		public string LabelText
		{
			get { return lblName.Text; }
			set { lblName.Text = value; }
		}

		/// <summary>The height to add on to the minimum height of txtValue.</summary>
		public int HeightIncrease
		{
			get { return (int)Math.Round(txtValue.ActualHeight - txtValue.MinHeight); }
			set { txtValue.Height = (double)value + txtValue.MinHeight; }
		}

		/// <summary>How far the cursor is from the upper-left corner of rctResize when dragging.</summary>
		private Point dragOffset;

		/// <summary>Called after the value is changed.</summary>
		public event EventHandler ValueChanged;

		/// <summary>Called after the height is changed.</summary>
		public event EventHandler HeightChanged;

		public TextField()
		{
			InitializeComponent();

			this.Tag = DataType.Text;
		}

		/// <summary>Refreshes the interface.</summary>
		public void refresh()
		{
			txtValue.Text = this.Value;
		}

		/// <summary>Sets the field as a title field.</summary>
		/// <param name="backColor">The color of the background.</param>
		public void setAsTitle(Brush backColor)
		{
			this.Background = backColor;
			lblName.Foreground = Brushes.White;
			lblName.FontWeight = FontWeights.Bold;
		}

		/// <summary>Saves the new value.</summary>
		private void txtValue_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (this.Value != txtValue.Text)
			{
				this.Value = txtValue.Text;
				CardManager.saveCardTextField(this.Value, CardID, CardTypeFieldID, Path, ref userMessage);
				ValueChanged?.Invoke(this, null);
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Prepare to resize txtValue.</summary>
		private void rctResize_MouseDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;

			Mouse.Capture(null);
			if (rctResize.CaptureMouse())
			{
				Point mousePos = e.MouseDevice.GetPosition(rctResize);
				dragOffset = mousePos;

				rctResize.MouseMove += rctResize_MouseMove;
			}
		}

		/// <summary>Resize a text field.</summary>
		private void rctResize_MouseMove(object sender, MouseEventArgs e)
		{
			string userMessage = string.Empty;

			// stop resizing the card
			if (e.LeftButton == MouseButtonState.Released)
			{
				rctResize.MouseMove -= rctResize_MouseMove;
				Mouse.Capture(null);

				CardManager.setFieldTextHeightIncrease(ArrangementCardID, CardTypeFieldID, (int)Math.Round(txtValue.ActualHeight - txtValue.MinHeight), Path, ref userMessage);

				HeightChanged?.Invoke(this, null);

				return;
			}

			Point cursor = e.MouseDevice.GetPosition(rctResize);

			double newHeight = txtValue.ActualHeight + cursor.Y - dragOffset.Y;

			// set wrapping
			if (newHeight <= txtValue.MinHeight)
			{
				txtValue.TextWrapping = TextWrapping.NoWrap;
				txtValue.Height = txtValue.MinHeight;
			}
			else
			{
				txtValue.TextWrapping = TextWrapping.Wrap;
				txtValue.Height = newHeight;
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}
	}
}
