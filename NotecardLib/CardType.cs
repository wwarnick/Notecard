using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public enum CardTypeContext { Standalone = 0, List = 1 }

	public class CardType
	{
		#region Members

		/// <summary>The database ID.</summary>
		public string ID { get; set; }

		/// <summary>The name displayed to the user.</summary>
		public string Name { get; set; }

		/// <summary>The ID of the inherited card type.</summary>
		public string ParentID { get; set; }

		/// <summary>The context where where the card type is used.</summary>
		public CardTypeContext Context { get; set; }

		/// <summary>The total number of fields, including inherited.</summary>
		public int NumFields { get; set; }

		/// <summary>The fields contained in this card type, stored in the order displayed to the user.</summary>
		public List<CardTypeField> Fields { get; private set; }

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the CardType class for a new card type.</summary>
		public CardType() : this(string.Empty, string.Empty, string.Empty, CardTypeContext.Standalone, 0) { }

		/// <summary>Initializes a new instance of the CardType class for an existing card type.</summary>
		/// <param name="id">The database ID.</param>
		/// <param name="name">The name displayed to the user.</param>
		/// <param name="parentID">The ID of the inherited card type.</param>
		/// <param name="context">The context where where the card type is used.</param>
		/// <param name="numFields">The total number of fields, including inherited.</param>
		public CardType(string id, string name, string parentID, CardTypeContext context, int numFields)
		{
			this.ID = id;
			this.Name = name;
			this.ParentID = parentID;
			this.Context = context;
			this.NumFields = numFields;
			this.Fields = new List<CardTypeField>();
		}

		#endregion Constructors
	}
}
