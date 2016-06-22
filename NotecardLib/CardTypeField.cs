using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	/// <summary>Represents one of the possible card field datatypes.</summary>
	public enum DataType { Text = 0, Card = 1, List = 2, Image = 3 } // in future, add date, image, etc.

	public class CardTypeField
	{
		#region Members

		/// <summary>The database ID.</summary>
		public string ID { get; set; }

		/// <summary>The name shown to the user.</summary>
		public string Name { get; set; }

		/// <summary>This determines what kind of data is stored in the field.</summary>
		public DataType FieldType { get; set; }

		/// <summary>Where the field will be displayed in the order of fields.</summary>
		public string SortOrder { get; set; }

		/// <summary>The ID of the card type referenced by this card type (if FieldType == Card).</summary>
		public string RefCardTypeID { get; set; }

		/// <summary>The list schema (if the context is List).</summary>
		public CardType ListType { get; set; }

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the CardTypeField class for a new card type field.</summary>
		public CardTypeField() : this(string.Empty, string.Empty, DataType.Text, string.Empty, string.Empty) { }

		/// <summary>Initializes a new instance of the CardTypeField class for an existing card type field.</summary>
		/// <param name="id">The database ID.</param>
		/// <param name="name">The name shown to the user.</param>
		/// <param name="fieldType">This determines what kind of data is stored in the field.</param>
		/// <param name="sortOrder">Where the field will be displayed in the order of fields.</param>
		/// <param name="refCardTypeID">The ID of the card type referenced by this card type (if FieldType == Card).</param>
		public CardTypeField(string id, string name, DataType fieldType, string sortOrder, string refCardTypeID)
		{
			this.ID = id;
			this.Name = name;
			this.FieldType = fieldType;
			this.SortOrder = sortOrder;
			this.RefCardTypeID = refCardTypeID;
			this.ListType = null;
		}

		#endregion Constructors
	}
}
