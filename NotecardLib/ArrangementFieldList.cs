using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class ArrangementFieldList
	{
		/// <summary>The database ID of the card type field.</summary>
		public string CardTypeFieldID { get; set; }

		/// <summary>Whether or not the list field is minimized.</summary>
		public bool Minimized { get; set; }

		/// <summary>Initializes a new instance of the ArrangementFieldList class</summary>
		/// <param name="cardTypeFieldID">The database ID of the card type field.</param>
		/// <param name="minimized">Whether or not the list field is minimized.</param>
		public ArrangementFieldList(string cardTypeFieldID, bool minimized)
		{
			this.CardTypeFieldID = cardTypeFieldID;
			this.Minimized = minimized;
		}

		/// <summary>Initializes a new instance of the ArrangementFieldList class</summary>
		/// <param name="cardTypeFieldID">The database ID of the card type field.</param>
		public ArrangementFieldList(string cardTypeFieldID) : this(cardTypeFieldID, true){ }
	}
}
