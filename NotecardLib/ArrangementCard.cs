using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class ArrangementCard
	{
		/// <summary>The arrangement card's database ID.</summary>
		public string ID { get; set; }

		/// <summary>The database ID of the card.</summary>
		public string CardID { get; set; }

		/// <summary>The settings for all text fields.</summary>
		public ArrangementFieldText[] TextFields { get; set; }

		/// <summary>Initializes a new instance of the ArrangementCard class.</summary>
		/// <param name="id">The arrangement card's database ID.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="textFields">The settings for all text fields.</param>
		public ArrangementCard(string id, string cardID, ArrangementFieldText[] textFields)
		{
			this.ID = id;
			this.CardID = cardID;
			this.TextFields = textFields;
		}
	}
}
