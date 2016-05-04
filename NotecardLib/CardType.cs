using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class CardType
    {
		#region Members

		/// <summary>The database ID.</summary>
		public string ID { get; set; }

		/// <summary>The name displayed to the user.</summary>
		public string VisibleName { get; set; }

		/// <summary>The name of the associated database table.</summary>
		public string TableName { get; set; }

		/// <summary>The ID of the inherited card type.</summary>
		public string InheritsCardTypeID { get; set; }

		/// <summary>The fields contained in this card type, stored in the order displayed to the user.</summary>
		public List<CardTypeField> Fields { get; private set; }

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the CardType class for a new card type.</summary>
		public CardType() : this(string.Empty, string.Empty, string.Empty, string.Empty) { }

		/// <summary>Initializes a new instance of the CardType class for an existing card type.</summary>
		/// <param name="id">The database ID.</param>
		/// <param name="visibleName">The name displayed to the user.</param>
		/// <param name="tableName">The name of the associated database table.</param>
		/// <param name="inheritsCardTypeID">The ID of the inherited card type.</param>
		public CardType(string id, string visibleName, string tableName, string inheritsCardTypeID)
		{
			this.ID = id;
			this.VisibleName = visibleName;
			this.InheritsCardTypeID = inheritsCardTypeID;
			this.Fields = new List<CardTypeField>();
		}

		#endregion Constructors
	}
}
