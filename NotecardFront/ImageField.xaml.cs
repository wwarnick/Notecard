using Microsoft.Win32;
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
	/// Interaction logic for ImageField.xaml
	/// </summary>
	public partial class ImageField : UserControl
	{
		public readonly string[] SupportFormats = new string[] { "bmp", "jpg", "jpeg", "png", "gif", "tiff", "ico" };

		/// <summary>The database ID of the owning card.</summary>
		public string CardID { get; set; }

		/// <summary>The database ID of the card type field.</summary>
		public string CardTypeFieldID { get; set; }

		/// <summary>The index of the field in the card type.</summary>
		public int FieldIndex { get; set; }

		/// <summary>Both the filename of the image and the database ID of the image field.</summary>
		public string Value { get; set; }

		/// <summary>The text to show in the label.</summary>
		public string LabelText
		{
			get { return lblName.Text; }
			set { if (lblName != null) lblName.Text = value; }
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
				}
				else if (!value && lblName != null)
				{
					grdMain.Children.Remove(lblName);
					lblName = null;
				}
			}
		}

		/// <summary>The field's label.</summary>
		public TextBlock lblName { get; set; }

		/// <summary>Called after btnDelete is pressed.</summary>
		public event EventHandler Deleted;

		/// <summary>Called after an image is added (not when an image is switched).</summary>
		public event EventHandler Added;

		public ImageField()
		{
			InitializeComponent();

			this.Tag = DataType.Image;
		}

		/// <summary>Refreshes the interface.</summary>
		public void refresh()
		{
			if (!string.IsNullOrEmpty(this.Value))
			{
				BitmapImage bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(@"current\" + this.Value, UriKind.Relative);
				bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();
				imgImage.Source = bmp;
				btnBrowse.Visibility = Visibility.Collapsed;
				imgImage.Visibility = Visibility.Visible;
				btnDelete.Visibility = Visibility.Hidden;
			}
			else
			{
				btnBrowse.Visibility = Visibility.Visible;
				imgImage.Visibility = Visibility.Collapsed;
				btnDelete.Visibility = Visibility.Collapsed;
			}
		}

		/// <summary>Determines whether or not the provided path is an image.</summary>
		/// <param name="imgPath">The path to check.</param>
		/// <returns>Whether or not the provided path is an image.</returns>
		private bool isValidImagePath(string imgPath)
		{
			string extension = imgPath.Substring(imgPath.LastIndexOf('.') + 1).ToLower();
			return SupportFormats.Contains(extension);
		}

		/// <summary>Tells whether a drag is valid or not.</summary>
		private void Grid_PreviewDragEnterOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string imgPath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

				if (isValidImagePath(imgPath))
					e.Effects = DragDropEffects.Move;
			}

			e.Handled = true;
		}

		/// <summary>Loads the dragged image.</summary>
		private void Grid_PreviewDrop(object sender, DragEventArgs e)
		{
			string userMessage = string.Empty;

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string imgPath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

				if (isValidImagePath(imgPath))
					loadImage(imgPath, ref userMessage);
			}

			e.Handled = true;

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Loads an image into the field.</summary>
		/// <param name="imgPath">The path of the image file.</param>
		/// <param name="userMessage">Any user messages.</param>
		private void loadImage(string imgPath, ref string userMessage)
		{
			// if no existing image, create new record
			if (string.IsNullOrEmpty(this.Value))
			{
				this.Value = CardManager.addCardImage(CardID, CardTypeFieldID, ref userMessage);
				btnBrowse.Visibility = Visibility.Collapsed;
				imgImage.Visibility = Visibility.Visible;
				btnDelete.Visibility = grdImage.IsMouseOver ? Visibility.Visible : Visibility.Hidden;
			}

			// resize image
			var photoDecoder = BitmapDecoder.Create(
				new Uri(imgPath),
				BitmapCreateOptions.PreservePixelFormat,
				BitmapCacheOption.None);
			var photo = photoDecoder.Frames[0];

			double mod = Math.Min(1d, Math.Min(imgImage.MaxWidth / photo.PixelWidth, imgImage.MaxHeight / photo.PixelHeight));

			var target = new TransformedBitmap(
				photo,
				new ScaleTransform(
					mod,
					mod,
					0, 0));
			var thumbnail = BitmapFrame.Create(target);

			// save image
			using (var fileStream = new System.IO.FileStream(@"current\" + this.Value, System.IO.FileMode.Create))
			{
				BitmapEncoder encoder = new PngBitmapEncoder();
				encoder.Frames.Add(thumbnail);
				encoder.Save(fileStream);
			}

			// display image
			imgImage.Source = thumbnail;

			Added?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Shows btnDelete.</summary>
		private void Grid_MouseEnter(object sender, MouseEventArgs e)
		{
			if (btnDelete.Visibility == Visibility.Hidden)
				btnDelete.Visibility = Visibility.Visible;
		}

		/// <summary>Hides btnDelete.</summary>
		private void Grid_MouseLeave(object sender, MouseEventArgs e)
		{
			if (btnDelete.Visibility == Visibility.Visible)
				btnDelete.Visibility = Visibility.Hidden;
		}

		/// <summary>Removes the current image.</summary>
		private void btnDelete_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			CardManager.removeCardImage(CardID, CardTypeFieldID, ref userMessage);

			System.IO.FileInfo imgFile = new System.IO.FileInfo(@"current\" + Value);
			imgFile.Delete();

			Value = null;

			imgImage.Source = null;
			imgImage.Visibility = Visibility.Collapsed;
			btnDelete.Visibility = Visibility.Collapsed;
			btnBrowse.Visibility = Visibility.Visible;

			Deleted?.Invoke(this, EventArgs.Empty);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}

		/// <summary>Allows the user to browse for an image to load.</summary>
		private void btnBrowse_Click(object sender, RoutedEventArgs e)
		{
			string userMessage = string.Empty;

			OpenFileDialog openDialog = new OpenFileDialog();
			openDialog.Filter = "Image Files | *.bmp; *.jpg; *.jpeg; *.png; *.gif; *.tiff; *.ico";

			if (openDialog.ShowDialog() == true)
				loadImage(openDialog.FileName, ref userMessage);

			if (!string.IsNullOrEmpty(userMessage))
				MessageBox.Show(userMessage);
		}
	}
}
