using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public enum CardTypeChange { DeleteCardType, NameChange, InheritChange, FieldOrderChange, FieldRemove, FieldAdd, FieldNameChange, FieldTypeChange, ListFieldOrderChange, ListFieldRemove, ListFieldNameChange, ListFieldTypeChange }

	public class CardTypeChg
	{
		#region Members

		/// <summary>The type of change to make.</summary>
		public CardTypeChange ChgType { get; set; }

		/// <summary>Parameters relevant to the change.</summary>
		public object Parameters { get; set; }

		#endregion Members
	}
}
