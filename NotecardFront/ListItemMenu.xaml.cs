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
	/// Interaction logic for SearchBox.xaml
	/// </summary>
	public partial class ListItemMenu : UserControl
	{
		#region Members

		private bool includeSwitch;

		/// <summary>Whether or not to include the switch button.</summary>
		public bool IncludeSwitch
		{
			get { return includeSwitch; }
			set
			{
				if (includeSwitch != value)
				{
					includeSwitch = value;

					if (includeSwitch)
					{
						Button btnSwitch = new Button()
						{
							Name = "btnSwitch",
							Background = Brushes.Transparent,
							BorderBrush = Brushes.Transparent,
							BorderThickness = new Thickness(0d),
							Foreground = btnRemoveItem.Foreground,
							Content = "=",
							MaxHeight = btnRemoveItem.MaxHeight,
							Padding = new Thickness(0d),
							FontWeight = btnRemoveItem.FontWeight,
							VerticalContentAlignment = VerticalAlignment.Center
						};

						Grid.SetColumnSpan(btnSwitch, 3);
						Grid.SetZIndex(btnSwitch, -1);

						btnSwitch.Click += btnSwitch_Click;

						grdMain.Children.Add(btnSwitch);
					}
					else
					{
						grdMain.Children.Remove((Button)grdMain.FindName("btnSwitch"));
					}
				}
			}
		}

		/// <summary>The button foreground brush.</summary>
		public Brush ForeColor
		{
			get { return btnRemoveItem.Foreground; }
			set
			{
				btnMinimizeItem.Foreground = value;
				btnRemoveItem.Foreground = value;

				if (IncludeSwitch)
					((Button)grdMain.FindName("btnSwitch")).Foreground = value;
			}
		}

		private bool minimized;

		public bool Minimized
		{
			get { return minimized; }
			set
			{
				minimized = value;
				btnMinimizeItem.Content = minimized ? "+" : "-";
			}
		}

		/// <summary>Fired when the user clicks btnSwitch.</summary>
		public event EventHandler Switch;

		/// <summary>Fired when the user clicks btnMinimizeItem</summary>
		public event EventHandler Minimize;

		/// <summary>Fired when the user clicks btnRemoveItem.</summary>
		public event EventHandler RemoveItem;

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the SearchBox class.</summary>
		public ListItemMenu()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region Events

		/// <summary>Fired when btnRemove is clicked.</summary>
		private void btnRemoveItem_Click(object sender, RoutedEventArgs e)
		{
			RemoveItem?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Fired when btnSwitch is clicked.</summary>
		private void btnSwitch_Click(object sender, RoutedEventArgs e)
		{
			Switch?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Fired when btnMinimizeItem is clicked.</summary>
		private void btnMinimizeItem_Click(object sender, RoutedEventArgs e)
		{
			Minimized = !Minimized;
			Minimize?.Invoke(this, EventArgs.Empty);
		}

		#endregion Events
	}
}
