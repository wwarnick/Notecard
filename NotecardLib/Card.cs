using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class Card
	{
		#region Members

		/// <summary>The database ID.</summary>
		public string ID { get; set; }

		/// <summary>The type of card.</summary>
		public CardType CType { get; set; }

		/// <summary>This card's fields.</summary>
		public object[] Fields { get; private set; }

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the Card class.</summary>
		/// <param name="id">The database ID.</param>
		/// <param name="cType">The type of card.</param>
		public Card(CardType cType, string id)
		{
			this.ID = id;
			this.CType = cType;
			this.Fields = new object[cType.NumFields];
		}

		#endregion Constructors
	}
}
