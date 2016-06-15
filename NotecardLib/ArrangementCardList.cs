using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class ArrangementCardList : ArrangementCard
	{
		/// <summary>Initializes a new instance of the ArrangementCardList class.</summary>
		/// <param name="id">The arrangement card's database ID.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="textFields">The settings for all text fields.</param>
		public ArrangementCardList(string id, string cardID, ArrangementFieldText[] textFields)
			: base(id, cardID, textFields)
		{
			
		}
	}
}
