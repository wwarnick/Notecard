using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class ArrangementFieldText
	{
		/// <summary>The database ID of the card type field.</summary>
		public string CardTypeFieldID { get; set; }

		/// <summary>The amount to increase the height of the text field.</summary>
		public int HeightIncrease { get; set; }

		/// <summary>Initializes a new instance of the ArrangementFieldText class</summary>
		/// <param name="cardTypeFieldID">The database ID of the card type field.</param>
		/// <param name="heightIncrease">The amount to increase the height of the text field.</param>
		public ArrangementFieldText(string cardTypeFieldID, int heightIncrease)
		{
			this.CardTypeFieldID = cardTypeFieldID;
			this.HeightIncrease = heightIncrease;
		}

		/// <summary>Initializes a new instance of the ArrangementFieldText class</summary>
		/// <param name="cardTypeFieldID">The database ID of the card type field.</param>
		public ArrangementFieldText(string cardTypeFieldID) : this(cardTypeFieldID, 0){ }
	}
}
