using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public enum CardTypeChange { CardTypeNameChange, CardTypeColorChange, CardTypeRemove, CardTypeFieldAdd, CardTypeFieldNameChange, CardTypeFieldTypeChange, CardTypeFieldCardTypeChange, CardTypeFieldShowLabelChange, CardTypeFieldSwap, CardTypeFieldRemove }

	public class CardTypeChg
	{
		#region Members

		/// <summary>The type of change to make.</summary>
		public CardTypeChange ChgType { get; set; }

		/// <summary>Parameters relevant to the change.</summary>
		public object[] Parameters { get; set; }

		#endregion Members

		#region Constructors

		/// <summary>Initializes a new instance of the CardTypeChg class.</summary>
		/// <param name="chgType">The type of change to make.</param>
		/// <param name="parameters">Parameters relevant to the change.</param>
		public CardTypeChg(CardTypeChange chgType, params object[] parameters)
		{
			this.ChgType = chgType;
			this.Parameters = parameters;
		}

		#endregion Constructors
	}
}
