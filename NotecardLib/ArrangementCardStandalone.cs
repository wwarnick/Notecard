using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class ArrangementCardStandalone : ArrangementCard
	{
		/// <summary>The x-coordinate of the card in the arrangement.</summary>
		public int X { get; set; }

		/// <summary>The y-coordinate of the card in the arrangement.</summary>
		public int Y { get; set; }

		/// <summary>The width of the card in the arrangement.</summary>
		public int Width { get; set; }

		/// <summary>The settings for all list fields.</summary>
		public ArrangementFieldList[] ListFields { get; set; }

		/// <summary>A list of all list items</summary>
		public ArrangementCardList[] ListItems { get; set; }

		/// <summary>Initializes a new instance of the ArrangementCardStandalone class.</summary>
		/// <param name="id">The arrangement card's database ID.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="textFields">The settings for all text fields.</param>
		/// <param name="x">The x-coordinate of the card in the arrangement.</param>
		/// <param name="y">The y-coordinate of the card in the arrangement.</param>
		/// <param name="width">The width of the card in the arrangement.</param>
		public ArrangementCardStandalone(string id, string cardID, ArrangementFieldText[] textFields, int x, int y, int width)
			: base(id, cardID, textFields)
		{
			this.X = x;
			this.Y = y;
			this.Width = width;
		}
	}
}
