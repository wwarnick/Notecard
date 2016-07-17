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

		/// <summary>Whether or not to include the switch button.</summary>
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
						// create switch button
						btnSwitch = new Button()
						{
							Name = "btnSwitch",
							Background = Brushes.Transparent,
							BorderBrush = Brushes.Transparent,
							BorderThickness = new Thickness(0d),
							MaxHeight = btnRemoveItem.MaxHeight,
						};
						btnSwitch.Click += btnSwitch_Click;
						grdMain.Children.Add(btnSwitch);

						// overlay switch label (separate so that we can center it across the menu)
						lblSwitch = new TextBlock()
						{
							Name = "lblSwitch",
							Foreground = btnRemoveItem.Foreground,
							Text = "=",
							Padding = new Thickness(0d),
							FontWeight = btnRemoveItem.FontWeight,
							VerticalAlignment = VerticalAlignment.Center,
							HorizontalAlignment = HorizontalAlignment.Center,
							IsHitTestVisible = false
						};
						Grid.SetColumnSpan(lblSwitch, 3);
						grdMain.Children.Add(lblSwitch);
					}
					else
					{
						grdMain.Children.Remove(btnSwitch);
						grdMain.Children.Remove(lblSwitch);

						btnSwitch = null;
						lblSwitch = null;
					}
				}
			}
		}

		/// <summary>The switch button.</summary>
		public Button btnSwitch { get; private set; }

		/// <summary>The label for the switch button.</summary>
		public TextBlock lblSwitch { get; private set; }

		/// <summary>Whether or not to include the minimize button.</summary>
		private bool includeMinimize;

		/// <summary>Whether or not to include the minimize button.</summary>
		public bool IncludeMinimize
		{
			get { return includeMinimize; }
			set
			{
				if (includeMinimize != value)
				{
					includeMinimize = value;
					//<Button Name="btnMinimizeItem" Content="-" Grid.Column="1" Background="Transparent" BorderBrush="Transparent" BorderThickness="0" Padding="5 0 5 0" VerticalContentAlignment="Center" MaxHeight="15" FontWeight="Bold" Click="btnMinimizeItem_Click"/>

					if (includeMinimize)
					{
						// create minimize button
						btnMinimizeItem = new Button()
						{
							Name = "btnMinimizeItem",
							Background = Brushes.Transparent,
							BorderBrush = Brushes.Transparent,
							BorderThickness = new Thickness(0d),
							MaxHeight = btnRemoveItem.MaxHeight,
							Padding = new Thickness(5d, 0d, 5d, 0d),
							VerticalContentAlignment = VerticalAlignment.Center,
							FontWeight = btnRemoveItem.FontWeight,
							Foreground = btnRemoveItem.Foreground,
							Content = "-"
						};
						Grid.SetColumn(btnMinimizeItem, 1);
						btnMinimizeItem.Click += btnMinimizeItem_Click;
						grdMain.Children.Add(btnMinimizeItem);
					}
					else
					{
						grdMain.Children.Remove(btnMinimizeItem);
						btnMinimizeItem = null;
					}
				}
			}
		}

		/// <summary>The minimize button.</summary>
		public Button btnMinimizeItem { get; private set; }

		/// <summary>The button foreground brush.</summary>
		public Brush ForeColor
		{
			get { return btnRemoveItem.Foreground; }
			set
			{
				if (IncludeMinimize)
					btnMinimizeItem.Foreground = value;

				btnRemoveItem.Foreground = value;

				if (IncludeSwitch)
					lblSwitch.Foreground = value;
			}
		}

		/// <summary>Determines how the minimize button will be displayed.</summary>
		private bool minimized;

		/// <summary>Determines how the minimize button will be displayed.</summary>
		public bool Minimized
		{
			get { return minimized; }
			set
			{
				minimized = value;
				if (IncludeMinimize)
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

			includeMinimize = false;
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
