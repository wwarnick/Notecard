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
		#region Members

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
			set { if (ShowLabel) lblName.Text = value; }
		}

		/// <summary>The height to add on to the minimum height of txtValue.</summary>
		public int HeightIncrease
		{
			get { return (int)Math.Round(txtValue.ActualHeight - txtValue.MinHeight); }
			set
			{
				txtValue.Height = (double)value + txtValue.MinHeight;
				txtValue.TextWrapping = value == 0 ? TextWrapping.NoWrap : TextWrapping.Wrap;
			}
		}

		/// <summary>How far the cursor is from the upper-left corner of rctResize when dragging.</summary>
		private Point dragOffset;

		/// <summary>Whether or not the label should be displayed.</summary>
		public bool ShowLabel
		{
			get { return lblName != null; }
			set
			{
				if (value && lblName == null)
				{
					lblName = new TextBlock()
					{
						Margin = new Thickness(5d),
						VerticalAlignment = VerticalAlignment.Center
					};

					grdMain.Children.Add(lblName);

					txtValue.Margin = new Thickness(0d, 5d, 10d, 0d);
					rctResize.Margin = new Thickness(0d, 0d, 10d, 0d);
				}
				else if (!value && lblName != null)
				{
					grdMain.Children.Remove(lblName);
					lblName = null;

					txtValue.Margin = new Thickness(10d, 5d, 10d, 0d);
					rctResize.Margin = new Thickness(10d, 0d, 10d, 0d);
				}
			}
		}

		/// <summary>The field's label.</summary>
		private TextBlock lblName { get; set; }

		/// <summary>Whether or not this text field is a title field.</summary>
		public bool IsTitle { get; private set; }

		/// <summary>Called after the value is changed.</summary>
		public event EventHandler ValueChanged;

		/// <summary>Called after the height is changed.</summary>
		public event EventHandler HeightChanged;

		#endregion Members

		#region Constructors

		public TextField()
		{
			InitializeComponent();

			this.Tag = DataType.Text;
		}

		#endregion Constructors

		#region Methods

		/// <summary>Refreshes the interface.</summary>
		public void refresh()
		{
			txtValue.Text = this.Value;
		}

		/// <summary>Sets the field as a title field.</summary>
		/// <param name="backColor">The color of the background.</param>
		public void setAsTitle(Brush backColor)
		{
			IsTitle = true;
			this.Background = backColor;

			if (lblName != null)
			{
				lblName.Foreground = Brushes.White;
				lblName.FontWeight = FontWeights.Bold;
			}
		}

		/// <summary>Saves the new value.</summary>
		private void txtValue_LostFocus(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (this.Value != txtValue.Text)
			{
				this.Value = txtValue.Text;
				CardManager.saveCardTextField(this.Value, CardID, CardTypeFieldID, ref userMessage);
				ValueChanged?.Invoke(this, EventArgs.Empty);
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

				CardManager.setFieldTextHeightIncrease(ArrangementCardID, CardTypeFieldID, (int)Math.Round(txtValue.ActualHeight - txtValue.MinHeight), ref userMessage);

				HeightChanged?.Invoke(this, EventArgs.Empty);

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

		#endregion Methods
	}
}
