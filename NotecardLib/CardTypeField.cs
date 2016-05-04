using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	/// <summary>Represents one of the possible card field datatypes.</summary>
	public enum DataType { Text = 0, List = 1, Card = 2 } // in future, add date, image, etc.

	public class CardTypeField
	{
		#region Members

		/// <summary>The database ID.</summary>
		public string ID { get; set; }

		/// <summary>This determines what kind of data is stored in the field.</summary>
		public DataType FieldType { get; set; }

		/// <summary>The name shown to the user.</summary>
		public string VisibleName { get; set; }

		/// <summary>The name of the associated column in the database.</summary>
		public string ColumnName { get; set; }

		/// <summary>Where the field will be displayed in the order of fields.</summary>
		public string SortOrder { get; set; }

		/// <summary>The ID of the card type referenced by this card type (if FieldType == Card).</summary>
		public string RefCardTypeID { get; set; }

		/// <summary>The name of the list's associated database table (if FieldType == List).</summary>
		public string ListTableName { get; set; }

		/// <summary>The fields contained in this list card type field, stored in the order displayed to the user.</summary>
		public List<CardTypeField> ListFields { get; private set; }

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the CardTypeField class for a new card type field.</summary>
		public CardTypeField() : this(string.Empty, DataType.Text, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty) { }

		/// <summary>Initializes a new instance of the CardTypeField class for an existing card type field.</summary>
		/// <param name="id">The database ID.</param>
		/// <param name="fieldType">This determines what kind of data is stored in the field.</param>
		/// <param name="visibleName">The name shown to the user.</param>
		/// <param name="columnName">The name of the associated column in the database.</param>
		/// <param name="sortOrder">Where the field will be displayed in the order of fields.</param>
		/// <param name="refCardTypeID">The ID of the card type referenced by this card type (if FieldType == Card).</param>
		public CardTypeField(string id, DataType fieldType, string visibleName, string columnName, string sortOrder, string refCardTypeID) : this(id, fieldType, visibleName, columnName, sortOrder, refCardTypeID, string.Empty) { }

		/// <summary>Initializes a new instance of the CardTypeField class for an existing card type field.</summary>
		/// <param name="id">The database ID.</param>
		/// <param name="fieldType">This determines what kind of data is stored in the field.</param>
		/// <param name="visibleName">The name shown to the user.</param>
		/// <param name="columnName">The name of the associated column in the database.</param>
		/// <param name="sortOrder">Where the field will be displayed in the order of fields.</param>
		/// <param name="refCardTypeID">The ID of the card type referenced by this card type (if FieldType == Card).</param>
		/// <param name="listTableName">The name of the list's associated database table (if FieldType == List).</param>
		public CardTypeField(string id, DataType fieldType, string visibleName, string columnName, string sortOrder, string refCardTypeID, string listTableName)
		{
			this.ID = id;
			this.FieldType = fieldType;
			this.VisibleName = visibleName;
			this.ColumnName = columnName;
			this.SortOrder = sortOrder;
			this.RefCardTypeID = refCardTypeID;
			this.ListTableName = listTableName;
			this.ListFields = new List<CardTypeField>();
		}

		#endregion Constructors
	}
}
