using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class ArrangementCardList : ArrangementCard
	{
		/// <summary>Whether or not the list item is minimized.</summary>
		public bool Minimized { get; set; }

		/// <summary>Initializes a new instance of the ArrangementCardList class.</summary>
		/// <param name="id">The arrangement card's database ID.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="textFields">The settings for all text fields.</param>
		/// <param name="minimized">Whether or not the list item is minimized.</param>
		public ArrangementCardList(string id, string cardID, ArrangementFieldText[] textFields, bool minimized)
			: base(id, cardID, textFields)
		{
			this.Minimized = minimized;
		}
	}
}
