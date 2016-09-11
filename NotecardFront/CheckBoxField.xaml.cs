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
	/// Interaction logic for CheckBoxField.xaml
	/// </summary>
	public partial class CheckBoxField : UserControl
	{
		/// <summary>The database ID of the owning card.</summary>
		public string CardID { get; set; }

		/// <summary>The database ID of the card type field.</summary>
		public string CardTypeFieldID { get; set; }

		/// <summary>The database ID of the arrangement card.</summary>
		public string ArrangementCardID { get; set; }

		/// <summary>The index of the field in the card type.</summary>
		public int FieldIndex { get; set; }

		/// <summary>Whether or not the checkbox is checked.</summary>
		public bool Value { get; set; }

		/// <summary>The text to show in the label.</summary>
		public string LabelText
		{
			get { return lblName.Text; }
			set { if (ShowLabel) lblName.Text = value; }
		}

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

					chkValue.Margin = new Thickness(0d, 5d, 10d, 5d);
				}
				else if (!value && lblName != null)
				{
					grdMain.Children.Remove(lblName);
					lblName = null;

					chkValue.Margin = new Thickness(10d, 5d, 10d, 5d);
				}
			}
		}

		/// <summary>The field's label.</summary>
		private TextBlock lblName { get; set; }

		/// <summary>Called after the value is changed.</summary>
		public event EventHandler ValueChanged;

		public CheckBoxField()
		{
			InitializeComponent();

			this.Tag = DataType.CheckBox;
		}

		/// <summary>Refreshes the interface.</summary>
		public void refresh()
		{
			chkValue.IsChecked = this.Value;
		}

		/// <summary>Fired when chkChecked is checked or unchecked.</summary>
		private void chkValue_CheckedChanged(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			if (this.Value != chkValue.IsChecked)
			{
				this.Value = chkValue.IsChecked.Value;
				CardManager.saveCardCheckBoxField(this.Value, CardID, CardTypeFieldID, ref userMessage);
				ValueChanged?.Invoke(this, EventArgs.Empty);
			}

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}
	}
}
