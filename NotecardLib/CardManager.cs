using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class CardManager
	{
		#region Members

		/// <summary>The current parameter number.  Used for creating parameter names.</summary>
		private static int paramNum;

		/// <summary>The current parameter name.</summary>
		private static string paramCurName;

		/// <summary>The current parameter name.</summary>
		public static string CurParamName
		{
			get { return paramCurName; }
		}

		#endregion Members

		#region Methods

		/// <summary>Initializes a new card database.</summary>
		/// <param name="path">The path to save the database to.</param>
		/// <returns>Any error messages.</returns>
		public static string createNewFile(string path)
		{
			try
			{
				SQLiteConnection.CreateFile(path);
			}
			catch (Exception ex)
			{
				return "Could not create file at \"" + path + "\": " + ex.Message + "\n\n";
			}

			string sql = @"
				CREATE TABLE `card_type` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`name` TEXT NOT NULL,
					`parent_id` INTEGER NULL DEFAULT NULL
						REFERENCES `card_type` (`id`)
						ON UPDATE CASCADE ON DELETE SET NULL
						DEFERRABLE INITIALLY DEFERRED,
					`context` INTEGER NOT NULL,
					UNIQUE (`name`)
				);

				CREATE INDEX `idx_ct_parent_id`
					ON `card_type` (`parent_id`);

				CREATE TABLE `card_type_field` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_type_id` INTEGER NOT NULL
						REFERENCES `card_type`
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`name` TEXT NOT NULL,
					`field_type` INTEGER NOT NULL,
					`sort_order` INTEGER NOT NULL,
					`ref_card_type_id` INTEGER NULL DEFAULT NULL
						REFERENCES `card_type` (`id`)
						ON UPDATE CASCADE ON DELETE SET NULL
						DEFERRABLE INITIALLY DEFERRED,
					UNIQUE (`card_type_id`, `name`)
				);

				CREATE INDEX `idx_ctf_card_type_id`
					ON `card_type_field` (`card_type_id`);

				CREATE INDEX `idx_ctf_ref_card_type_id`
					ON `card_type_field` (`ref_card_type_id`);

				CREATE TABLE `card` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_type_id` INTEGER NOT NULL
						REFERENCES `card_type` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED
				);

				CREATE INDEX `idx_c_card_type_id`
					ON `card` (`card_type_id`);

				CREATE TABLE `field_text` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_id` INTEGER NOT NULL
						REFERENCES `card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`card_type_field_id` INTEGER NOT NULL
						REFERENCES `card_type_field` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`value` TEXT NOT NULL DEFAULT '',
					UNIQUE (`card_id`, `card_type_field_id`)
				);

				CREATE INDEX `idx_ft_card_id`
					ON `field_text` (`card_id`);

				CREATE INDEX `idx_ft_card_type_field_id`
					ON `field_text` (`card_type_field_id`);

				CREATE TABLE `field_card` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_id` INTEGER NOT NULL
						REFERENCES `card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`card_type_field_id` INTEGER NOT NULL
						REFERENCES `card_type_field` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`value` INTEGER NULL DEFAULT NULL
						REFERENCES `card` (`id`)
						ON UPDATE CASCADE ON DELETE SET NULL
						DEFERRABLE INITIALLY DEFERRED,
					UNIQUE (`card_id`, `card_type_field_id`)
				);

				CREATE INDEX `idx_fc_card_id`
					ON `field_card` (`card_id`);

				CREATE INDEX `idx_fc_card_type_field_id`
					ON `field_card` (`card_type_field_id`);

				CREATE INDEX `idx_fc_value`
					ON `field_card` (`value`);

				CREATE TABLE `field_list` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_id` INTEGER NOT NULL
						REFERENCES `card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`card_type_field_id` INTEGER NOT NULL
						REFERENCES `card_type_field` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`value` INTEGER NULL DEFAULT NULL
						REFERENCES `card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`sort_order` INTEGER NOT NULL
				);

				CREATE INDEX `idx_fl_card_id`
					ON `field_list` (`card_id`);

				CREATE INDEX `idx_fl_card_type_field_id`
					ON `field_list` (`card_type_field_id`);

				CREATE INDEX `idx_fl_value`
					ON `field_list` (`value`);";

			return execNonQuery(sql, path, null);
		}

		/// <summary>Saves a new card type to the database.</summary>
		/// <param name="cardType">The card type to save.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="result">The card type re-loaded from the database after being saved.</param>
		/// <returns>Any error messages.</returns>
		public static string saveNewCardType(CardType cardType, string path, out CardType result)
		{
			string errorMessage = string.Empty;
			result = null;
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			resetParamNames();

			// create card type record
			StringBuilder sql = new StringBuilder();
			sql.Append(@"
				INSERT INTO `card_type` (`name`, `parent_id`, `context`) VALUES (@name, @parent_id, @context);

				SELECT LAST_INSERT_ROWID() AS `id`;");

			parameters.AddRange(new SQLiteParameter[] {
					createParam("@name", DbType.String, cardType.Name),
					createParam("@context", DbType.Int64, (int)cardType.Context)});

			if (string.IsNullOrEmpty(cardType.ParentID))
				parameters.Add(createParam("@parent_id", DbType.Int64, DBNull.Value));
			else
				parameters.Add(createParam("@parent_id", DbType.Int64, cardType.ParentID));

			string[] cardTypeResult;
			errorMessage += execReadField(sql.ToString(), path, parameters, out cardTypeResult, "id");
			cardType.ID = cardTypeResult[0];

			// create field records
			if (cardType.Fields.Count > 0)
			{
				sql.Clear();
				parameters.Clear();
				resetParamNames();

				sql.Append(@"
					INSERT INTO `card_type_field` (`card_type_id`, `name`, `field_type`, `sort_order`, `ref_card_type_id`) VALUES");

				parameters.Add(createParam("@card_type_id", DbType.Int64, cardType.ID));

				for (int i = 0; i < cardType.Fields.Count; i++)
				{
					CardTypeField f = cardType.Fields[i];

					if (f.FieldType == DataType.List)
					{
						CardType listResult;
						errorMessage += saveNewCardType(f.ListType, path, out listResult);
						f.RefCardTypeID = listResult.ID;
					}

					sql.Append((i > 0 ? ", " : "") + @"
						(@card_type_id, " + getNextParamName("name"));
					parameters.Add(createParam(CurParamName, DbType.String, f.Name));

					sql.Append(", " + getNextParamName("field_type"));
					parameters.Add(createParam(CurParamName, DbType.Int64, f.FieldType));

					sql.Append(", " + getNextParamName("sort_order"));
					parameters.Add(createParam(CurParamName, DbType.Int64, i + 1));

					sql.Append(", " + getNextParamName("ref_card_type_id") + ")");
					if (string.IsNullOrEmpty(f.RefCardTypeID))
						parameters.Add(createParam(CurParamName, DbType.Int64, DBNull.Value));
					else
						parameters.Add(createParam(CurParamName, DbType.Int64, f.RefCardTypeID));
				}

				sql.Append(";");

				errorMessage += execNonQuery(sql.ToString(), path, parameters);
			}

			// get final version of card type
			errorMessage += getCardType(cardType.ID, path, out result);

			return errorMessage;
		}

		/// <summary>Saves changes to an existing card type.</summary>
		/// <param name="cardTypeID">The database ID of the card type to update.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="changes">The changes to make.</param>
		/// <returns>Any error messages.</returns>
		public static string saveCardType(string cardTypeID, CardTypeChg change, string path)
		{
			string errorMessage = string.Empty;

			CardType cardType;
			errorMessage += getCardType(cardTypeID, path, out cardType);
			string sql = null;

			switch (change.ChgType)
			{
				case CardTypeChange.CardTypeNameChange:
					sql = "UPDATE `card_type` SET `name` = @name WHERE `id` = @id;";
					errorMessage += execNonQuery(sql, path, createParam("@name", DbType.String, (string)change.Parameters[0]), createParam("@id", DbType.Int64, cardType.ID));
					break;
				case CardTypeChange.CardTypeParentChange:
					errorMessage += changeCardTypeParent(cardType, (string)change.Parameters[0], path);
					break;
				case CardTypeChange.CardTypeRemove:
					errorMessage += removeCardType(cardType, path);
					break;
				case CardTypeChange.CardTypeFieldAdd:
					errorMessage += addCardTypeField(cardType, (CardTypeField)change.Parameters[0], path);
					break;
				case CardTypeChange.CardTypeFieldNameChange:
					sql = "UPDATE `card_type_field` SET `name` = @name WHERE `id` = @id;";
					errorMessage += execNonQuery(sql, path, createParam("@name", DbType.String, (string)change.Parameters[1]), createParam("@id", DbType.Int64, (string)change.Parameters[0]));
					break;
				case CardTypeChange.CardTypeFieldTypeChange:
					errorMessage += changeCardTypeFieldType((string)change.Parameters[0], (DataType)change.Parameters[1], path);
					break;
				case CardTypeChange.CardTypeFieldOrderChange:
					sql = "UPDATE `card_type_field` SET `sort_order` = @sort_order WHERE `id` = @id;";
					errorMessage += execNonQuery(sql, path, createParam("@id", DbType.Int64, (string)change.Parameters[0]), createParam("@sort_order", DbType.Int64, (string)change.Parameters[1]));
					break;
				case CardTypeChange.CardTypeFieldRemove:
					errorMessage += removeCardTypeField((string)change.Parameters[0], path);
					break;
				default:
					errorMessage += "Unknown change type: " + change.ChgType.ToString();
					break;
			}

			return errorMessage;
		}

		/// <summary>Changes the parent of a card type and applies the changes to all associated cards.</summary>
		/// <param name="cardType">The card type to change.</param>
		/// <param name="parentID">The database ID of the new parent.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		private static string changeCardTypeParent(CardType cardType, string parentID, string path)
		{
			// TODO: if new parent is an ancestor, just delete the card types in between

			string errorMessage = string.Empty;

			StringBuilder sql = new StringBuilder("UPDATE `card_type` SET `parent_id` = @parent_id WHERE `id` = @card_type_id;");

			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_type_id", DbType.Int64, cardType.ID));
			if (string.IsNullOrEmpty(parentID))
				parameters.Add(createParam("@parent_id", DbType.Int64, DBNull.Value));
			else
				parameters.Add(createParam("@parent_id", DbType.Int64, parentID));

			List<string> temp;
			errorMessage += getCardTypeDescendents(cardType.ID, path, out temp);
			string descendents = string.Join(", ", temp);

			// remove old inherited fields
			if (!string.IsNullOrEmpty(cardType.ParentID))
			{
				errorMessage += getCardTypeAncestry(cardType.ParentID, path, out temp);
				string oldParents = string.Join(", ", temp);

				// delete list cards first
				sql.Append(@"
					DELETE FROM `card`
					WHERE `id` IN (
						SELECT `fl`.`value`
						FROM `field_list` `fl`
							JOIN `card` `c` ON `c`.`id` = `fl`.`card_id`
						WHERE `c`.`card_type_id` IN (" + descendents + @")
							AND `fl`.`card_type_field_id` IN (
								SELECT `id`
								FROM `card_type_field`
								WHERE `card_type_id` IN (" + oldParents + ")));");

				// delete fields
				string[] fieldTableNames = new string[] { "field_text", "field_card", "field_list" };

				foreach (string fieldTableName in fieldTableNames)
				{
					sql.Append(@"

					DELETE FROM `" + fieldTableName + @"`
					WHERE `card_type_field_id` IN (SELECT `id` FROM `card_type_field` WHERE `card_type_id` IN (" + oldParents + @"))
						AND `card_id` IN (SELECT `id` FROM `card` WHERE `card_type_id` IN (" + descendents + "));");
				}
			}

			// add new inherited fields
			if (!string.IsNullOrEmpty(parentID))
			{
				List<CardType> newParents;
				errorMessage += getCardTypeAncestry(parentID, path, out newParents);

				StringBuilder fieldText = new StringBuilder();
				StringBuilder fieldCard = new StringBuilder();

				foreach (CardType parent in newParents)
				{
					foreach (CardTypeField f in parent.Fields)
					{
						switch (f.FieldType)
						{
							case DataType.Text:
								fieldText.Append((fieldText.Length > 0 ? ", " : "") + f.ID);
								break;
							case DataType.Card:
								fieldCard.Append((fieldCard.Length > 0 ? ", " : "") + f.ID);
								break;
							case DataType.List:
								// do nothing
								break;
						}
					}
				}

				if (fieldText.Length > 0)
				{
					sql.Append(@"
						INSERT INTO `field_text` (`card_id`, `card_type_field_id`, `value`)
						SELECT `c`.`id`, `cdf`.`id`, ''
						FROM `card` `c`
							JOIN `card_type_field` `cdf` ON `cdf`.`id` IN (" + fieldText.ToString() + ");");
				}

				if (fieldCard.Length > 0)
				{
					sql.Append(@"
						INSERT INTO `field_card` (`card_id`, `card_type_field_id`, `value`)
						SELECT `c`.`id`, `cdf`.`id`, NULL
						FROM `card` `c`
							JOIN `card_type_field` `cdf` ON `cdf`.`id` IN (" + fieldCard.ToString() + ");");
				}
			}

			// execute query
			errorMessage += execNonQuery(sql.ToString(), path, parameters);

			return errorMessage;
		}

		/// <summary>Removes a card type from the database.</summary>
		/// <param name="cardType">The card type to remove.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		private static string removeCardType(CardType cardType, string path)
		{
			string errorMessage = string.Empty;

			StringBuilder sql = new StringBuilder();
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_type_id", DbType.Int64, cardType.ID));

			// assign cards and child types to parent
			if (!string.IsNullOrEmpty(cardType.ParentID))
			{
				sql.Append(@"
					UPDATE `card_type` SET `parent_id` = @new_parent_id WHERE `parent_id` = @card_type_id;
					UPDATE `card` SET `card_type_id` = @new_parent_id WHERE `card_type_id` = @card_type_id;");

				parameters.Add(createParam("@new_parent_id", DbType.Int64, cardType.ParentID));
			}

			// get list of card types to delete
			StringBuilder toDelete = new StringBuilder("@card_type_id");

			// get list types
			foreach (CardTypeField f in cardType.Fields)
			{
				if (f.FieldType == DataType.List)
					toDelete.Append(", " + f.RefCardTypeID);
			}

			sql.Append(@"
					DELETE FROM `card_type` WHERE `id` IN (" + toDelete.ToString() + ");");

			errorMessage += execNonQuery(sql.ToString(), path, parameters);

			return errorMessage;
		}

		/// <summary>Adds a card type field to the database.</summary>
		/// <param name="cardType">The owning card type.</param>
		/// <param name="field">The field to add.</param>
		/// <param name="path">The path of the curent database.</param>
		/// <returns>Any error messages.</returns>
		private static string addCardTypeField(CardType cardType, CardTypeField field, string path)
		{
			string errorMessage = string.Empty;

			// insert card type field records
			StringBuilder sql = new StringBuilder(@"
				INSERT INTO `card_type_field` (`card_type_id`, `name`, `field_type`, `sort_order`, `ref_card_type_id`)
				VALUES (@card_type_id, @name, @field_type, (SELECT COALESCE(MAX(`sort_order`), 0) + 1 FROM `card_type_field` WHERE `card_type_id` = @card_type_id), @ref_card_type_id);
");

			List<SQLiteParameter> parameters = new List<SQLiteParameter>()
			{
				createParam("@card_type_id", DbType.Int64, cardType.ID),
				createParam("@name", DbType.String, field.Name),
				createParam("@field_type", DbType.Int64, (int)field.FieldType)
			};

			SQLiteParameter refCardTypeID = createParam("@ref_card_type_id", DbType.Int64, DBNull.Value);

			// insert card field records
			List<string> temp;
			errorMessage += getCardTypeDescendents(cardType.ID, path, out temp);
			string descendents = string.Join(", ", temp);

			switch (field.FieldType)
			{
				case DataType.Text:
					sql.Append(@"
				INSERT INTO `field_text` (`card_id`, `card_type_field_id`, `value`)
				SELECT `id`, LAST_INSERT_ROWID(), '' FROM `card` WHERE `card_type_id` IN (" + descendents + ");");
					break;
				case DataType.Card:
					sql.Append(@"
				INSERT INTO `field_card` (`card_id`, `card_type_field_id`, `value`)
				SELECT `id`, LAST_INSERT_ROWID(), NULL FROM `card` WHERE `card_type_id` IN (" + descendents + ");");

					refCardTypeID.Value = field.RefCardTypeID;
					break;
				case DataType.List:
					refCardTypeID.Value = field.RefCardTypeID;
					break;
			}

			parameters.Add(refCardTypeID);

			// execute sql
			errorMessage += execNonQuery(sql.ToString(), path, parameters);

			return errorMessage;
		}

		/// <summary>Changes a card type field's field type.</summary>
		/// <param name="fieldID">The database ID of the card type field.</param>
		/// <param name="newType">The type to change it to.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		private static string changeCardTypeFieldType(string fieldID, DataType newType, string path)
		{
			string errorMessage = string.Empty;

			// TODO: allow changing to a list
			if (newType == DataType.List)
				return "A field cannot be changed to a list. This feature has not been implemented yet.";

			CardTypeField oldField;
			string cardTypeID;
			errorMessage += getCardTypeField(fieldID, path, out oldField, out cardTypeID);

			StringBuilder sql = new StringBuilder(@"
						UPDATE `card_type_field` SET `field_type` = @field_type WHERE `id` = @card_type_field_id;");
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_type_field_id", DbType.Int64, fieldID));
			parameters.Add(createParam("@card_type_id", DbType.Int64, cardTypeID));
			parameters.Add(createParam("@field_type", DbType.Int64, (int)newType));
			
			// remove old fields
			switch (oldField.FieldType)
			{
				case DataType.Text:
					sql.Append(@"
						DELETE FROM `field_text` WHERE `card_type_field_id` = @card_type_field_id;");
					break;
				case DataType.Card:
					sql.Append(@"
						DELETE FROM `field_card` WHERE `card_type_field_id` = @card_type_field_id;");
					break;
				case DataType.List:
					sql.Append(@"
						DELETE FROM `card_type_field` WHERE `id` = @list_type_id;
						DELETE FROM `field_list` WHERE `card_type_field_id` = @card_type_field_id;");
					parameters.Add(createParam("@list_type_id", DbType.Int64, oldField.RefCardTypeID));
					break;
				default:
					errorMessage += "Unkown field type: " + oldField.FieldType.ToString();
					break;
			}

			// insert new fields
			List<string> temp;
			errorMessage += getCardTypeDescendents(cardTypeID, path, out temp);
			string descendents = string.Join(", ", temp);

			switch (newType)
			{
				case DataType.Text:
					sql.Append(@"
						INSERT INTO `field_text` (`card_id`, `card_type_field_id`, `value`)
						SELECT `id`, @card_type_field_id, '' FROM `card` WHERE `card_type_id` = @card_type_id;");
					break;
				case DataType.Card:
					sql.Append(@"
						INSERT INTO `field_card` (`card_id`, `card_type_field_id`, `value`)
						SELECT `id`, @card_type_field_id, NULL FROM `card` WHERE `card_type_id` = @card_type_id;");
					break;
				case DataType.List:
					// TODO: THIS!
					break;
				default:
					errorMessage += "Unknown field type: " + newType.ToString();
					break;
			}

			// execute sql
			errorMessage += execNonQuery(sql.ToString(), path, parameters);

			return errorMessage;
		}

		/// <summary>Removes a card type field from the database.</summary>
		/// <param name="fieldID">The database ID of the card type field.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		private static string removeCardTypeField(string fieldID, string path)
		{
			string errorMessage = string.Empty;

			// delete card_type_field record
			StringBuilder sql = new StringBuilder(@"
				DELETE FROM `card_type_field` WHERE `id` = @card_type_field_id;");
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_type_field_id", DbType.Int64, fieldID));

			// remove list type if list
			CardTypeField field;
			errorMessage += getCardTypeField(fieldID, path, out field);
			if (field.FieldType == DataType.List)
			{
				sql.Append(@"
				DELETE FROM `card_type` WHERE `id` = @list_type_id;");

				parameters.Add(createParam("@list_type_id", DbType.Int64, field.RefCardTypeID));
			}

			errorMessage += execNonQuery(sql.ToString(), path, parameters);

			return errorMessage;
		}

		/// <summary>Retrieves a card type.</summary>
		/// <param name="id">The database id of the card type.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="cardType">The requested card type.</param>
		/// <returns>Any error messages..</returns>
		public static string getCardType(string id, string path, out CardType cardType)
		{
			string errorMessage = string.Empty;

			cardType = null;

			// get card type
			string sql = "SELECT * FROM `card_type` WHERE `id` = @id;";
			List<SQLiteParameter> parameters = new List<SQLiteParameter>() { createParam("@id", DbType.Int64, id) };
			string[] cardTypeResult;
			errorMessage += execReadField(sql, path, parameters, out cardTypeResult, "name", "parent_id", "context");

			cardType = new CardType(id, cardTypeResult[0], cardTypeResult[1], (CardTypeContext)int.Parse(cardTypeResult[2]), 0);

			// get fields
			sql = "SELECT * FROM `card_type_field` WHERE `card_type_id` = @id ORDER BY `sort_order` ASC;";
			List<string[]> fieldResult;
			errorMessage += execReadField(sql, path, parameters, out fieldResult, "id", "name", "field_type", "ref_card_type_id");

			for (int i = 0; i < fieldResult.Count; i++)
			{
				string[] f = fieldResult[i];
				CardTypeField field = new CardTypeField(f[0], f[1], (DataType)int.Parse(f[2]), (i + 1).ToString(), f[3]);

				// get list type
				if (field.FieldType == DataType.List)
				{
					CardType listType;
					errorMessage += getCardType(field.RefCardTypeID, path, out listType);
					field.ListType = listType;
				}

				cardType.Fields.Add(field);
			}

			// get field count
			cardType.NumFields += cardType.Fields.Count;

			string cardID = cardType.ParentID;
			sql = @"
					SELECT
						`ct`.`parent_id`,
						COALESCE(COUNT(`ctf`.`id`), 0) AS `num_fields`
					FROM `card_type` `ct`
						LEFT JOIN `card_type_field` `ctf` ON `ctf`.`card_type_id` = `ct`.`id`
					WHERE `ct`.`id` = @card_type_id;";

			while (!string.IsNullOrEmpty(cardID))
			{
				parameters.Clear();
				parameters.Add(createParam("@card_type_id", DbType.Int64, cardID));
				string[] countResult;
				errorMessage += execReadField(sql, path, parameters, out countResult, "parent_id", "num_fields");

				cardID = countResult[0];
				cardType.NumFields += int.Parse(countResult[1]);
			}

			return errorMessage;
		}

		/// <summary>Retrieves a card type field.</summary>
		/// <param name="id">The database ID of the card type field.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="field">The retrieved card type field.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTypeField(string id, string path, out CardTypeField field)
		{
			string cardTypeID;
			return getCardTypeField(id, path, out field, out cardTypeID);
		}

		/// <summary>Retrieves a card type field.</summary>
		/// <param name="id">The database ID of the card type field.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="field">The retrieved card type field.</param>
		/// <param name="cardTypeID">The database ID of the owning card type.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTypeField(string id, string path, out CardTypeField field, out string cardTypeID)
		{
			string errorMessage = string.Empty;

			string sql = "SELECT * FROM `card_type_field` WHERE `id` = @id;";
			string[] result;
			errorMessage += execReadField(sql, path, createParam("@id", DbType.Int64, id), out result, "card_type_id", "name", "field_type", "sort_order", "ref_card_type_id");

			field = new CardTypeField(id, result[1], (DataType)int.Parse(result[2]), result[3], result[4]);
			cardTypeID = result[0];

			// get list type if it's a list
			if (field.FieldType == DataType.List)
			{
				CardType listType;
				errorMessage += getCardType(field.RefCardTypeID, path, out listType);
				field.ListType = listType;
			}

			return errorMessage;
		}

		/// <summary>Gets a card type and all of its ancestors.</summary>
		/// <param name="cardTypeID">The child card type.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="ancestry">The retrieved card types.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTypeAncestry(string cardTypeID, string path, out List<CardType> ancestry)
		{
			CardType cardType;
			string errorMessage = getCardType(cardTypeID, path, out cardType);
			return errorMessage + getCardTypeAncestry(cardType, path, out ancestry);
		}

		/// <summary>Gets a card type's and all of its ancestors.</summary>
		/// <param name="cardType">The child card type.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="ancestry">The retrieved card types.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTypeAncestry(CardType cardType, string path, out List<CardType> ancestry)
		{
			string errorMessage = string.Empty;

			ancestry = new List<CardType>() { cardType };
			while (!string.IsNullOrEmpty(cardType.ParentID))
			{
				errorMessage += getCardType(cardType.ParentID, path, out cardType);
				ancestry.Add(cardType);
			}

			return errorMessage;
		}

		/// <summary>Gets a card type and all of its ancestors' database IDs.</summary>
		/// <param name="cardTypeID">The child card type.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="ancestry">The retrieved card types' database IDs.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTypeAncestry(string cardTypeID, string path, out List<string> ancestry)
		{
			string errorMessage = string.Empty;

			ancestry = new List<string>();
			string parentID = cardTypeID;
			string sql = "SELECT `parent_id` FROM `card_type` WHERE `id` = @id;";
			while (!string.IsNullOrEmpty(parentID))
			{
				ancestry.Add(parentID);

				string[] result;
				errorMessage += execReadField(sql, path, createParam("@id", DbType.Int64, parentID), out result, "parent_id");
				parentID = result[0];
			}

			return errorMessage;
		}

		/// <summary>Gets a card type and all of its descendents' database IDs.</summary>
		/// <param name="cardTypeID">The parent card type.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="descendents">The retrieved card types' database IDs.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTypeDescendents(string cardTypeID, string path, out List<string> descendents)
		{
			string errorMessage = string.Empty;

			descendents = new List<string>();

			string sql = "SELECT `id` FROM `card_type` WHERE `parent_id` IN (";

			List<string> curList = new List<string>() { cardTypeID };
			while (curList.Count > 0)
			{
				descendents.AddRange(curList);
				string ids = string.Join(", ", curList);
				curList.Clear();

				List<string[]> results;
				errorMessage += execReadField((sql + ids + ");"), path, (List<SQLiteParameter>)null, out results, "id");
				foreach (string[] result in results)
				{
					curList.Add(result[0]);
				}
			}

			return errorMessage;
		}

		/// <summary>Creates a new card.</summary>
		/// <param name="cardType">The type of card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="card">The new card.</param>
		/// <returns>Any error messages.</returns>
		public static string newCard(CardType cardType, string path, out Card card)
		{
			string errorMessage = string.Empty;
			card = null;

			StringBuilder sql = new StringBuilder();
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			resetParamNames();

			sql.Append(@"
				INSERT INTO `card` (`card_type_id`) VALUES (@card_type_id);
				SELECT LAST_INSERT_ROWID() AS `id`;");
			parameters.Add(createParam("@card_type_id", DbType.Int64, cardType.ID));

			string[] cardTypeResult;
			errorMessage += execReadField(sql.ToString(), path, parameters, out cardTypeResult, "id");

			string cardID = cardTypeResult[0];

			// get parent card types
			List<CardType> cardTypes;
			errorMessage += getCardTypeAncestry(cardType, path, out cardTypes);

			// insert fields
			StringBuilder fieldText = new StringBuilder();
			StringBuilder fieldCard = new StringBuilder();
			parameters.Clear();

			foreach (CardType ct in cardTypes)
			{
				foreach (CardTypeField f in ct.Fields)
				{
					switch (f.FieldType)
					{
						case DataType.Text:
							fieldText.Append((fieldText.Length > 0 ? ", " : "") + @"
								(@card_id, " + getNextParamName("card_type_field_id") + ")");
							parameters.Add(createParam(CurParamName, DbType.Int64, f.ID));
							break;
						case DataType.Card:
							fieldCard.Append((fieldCard.Length > 0 ? ", " : "") + @"
								(@card_id, " + getNextParamName("card_type_field_id") + ")");
							parameters.Add(createParam(CurParamName, DbType.Int64, f.ID));
							break;
						case DataType.List:
							// do nothing
							break;
						default:
							errorMessage += "Unknown field type: " + f.FieldType;
							break;
					}
				}
			}

			sql.Clear();

			if (fieldText.Length > 0)
			{
				sql.Append(@"
					INSERT INTO `field_text` (`card_id`, `card_type_field_id`) VALUES" + fieldText.ToString() + ";");
			}

			if (fieldCard.Length > 0)
			{
				sql.Append(@"
					INSERT INTO `field_card` (`card_id`, `card_type_field_id`) VALUES" + fieldCard.ToString() + ";");
			}

			if (sql.Length > 0)
			{
				parameters.Add(createParam("@card_id", DbType.Int64, cardID));
				errorMessage += execNonQuery(sql.ToString(), path, parameters);
			}

			// get saved card
			errorMessage += getCard(cardID, path, out card);

			return errorMessage;
		}

		/// <summary>Saves changes to an existing card.</summary>
		/// <param name="card">The updated card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string saveCard(Card card, string path)
		{
			string errorMessage = string.Empty;

			StringBuilder sql = new StringBuilder();
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			resetParamNames();

			List<CardType> cardTypes;
			errorMessage += getCardTypeAncestry(card.CType, path, out cardTypes);


			int pastFields = 0;
			foreach (CardType ct in cardTypes)
			{
				for (int i = 0; i < ct.Fields.Count; i++)
				{
					CardTypeField f = ct.Fields[i];
					switch (f.FieldType)
					{
						case DataType.Text:
							{
								string field = (string)card.Fields[pastFields + i];
								sql.Append(@"
								UPDATE `field_text` SET `value` = " + getNextParamName("value") + " WHERE `card_id` = @card_id AND `card_type_field_id` = ");
								parameters.Add(createParam(CurParamName, DbType.String, field));

								sql.Append(getNextParamName("card_type_field_id") + ";");
								parameters.Add(createParam(CurParamName, DbType.Int64, f.ID));
							}
							break;
						case DataType.Card:
							{
								string field = (string)card.Fields[pastFields + i];
								sql.Append(@"
								UPDATE `field_card` SET `value` = " + getNextParamName("value") + " WHERE `card_id` = @card_id AND `card_type_field_id` = ");
								parameters.Add(createParam(CurParamName, DbType.Int64, field));

								sql.Append(getNextParamName("card_type_field_id") + ";");
								parameters.Add(createParam(CurParamName, DbType.Int64, f.ID));
							}
							break;
						case DataType.List:
							{
								List<Card> list = (List<Card>)card.Fields[pastFields + i];
								for (int j = 0; j < list.Count; j++)
								{
									sql.Append(@"
								UPDATE `field_list` SET `sort_order` = " + getNextParamName("sort_order"));
									parameters.Add(createParam(CurParamName, DbType.Int64, j + 1));

									sql.Append(" WHERE `value` = " + getNextParamName("value") + ";");
									parameters.Add(createParam(CurParamName, DbType.Int64, list[j].ID));

									errorMessage += saveCard(list[j], path);
								}
							}
							break;
						default:
							errorMessage += "Unknown card type: " + f.FieldType;
							break;
					}
				}

				pastFields += ct.Fields.Count;
			}

			// execute changes
			if (sql.Length > 0)
			{
				parameters.Add(createParam("@card_id", DbType.Int64, card.ID));
				errorMessage += execNonQuery(sql.ToString(), path, parameters);
			}

			return errorMessage;
		}

		/// <summary>Retrieves a card from the database.</summary>
		/// <param name="id">The database id of the card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="card">The card.</param>
		/// <returns>Any error messages.</returns>
		public static string getCard(string id, string path, out Card card)
		{
			string errorMessage = string.Empty;
			card = null;

			// get base card data
			List<SQLiteParameter> parameters = new List<SQLiteParameter>() { createParam("@card_id", DbType.Int64, id) };
			string sql = "SELECT `card_type_id` FROM `card` WHERE `id` = @card_id;";

			string[] cardResult;
			errorMessage += execReadField(sql, path, parameters, out cardResult, "card_type_id");

			// get field data
			CardType cardType;
			errorMessage += getCardType(cardResult[0], path, out cardType);
			card = new Card(cardType, id);

			// get parent card types
			List <CardType> cardTypes;
			errorMessage += getCardTypeAncestry(cardType, path, out cardTypes);

			int pastFields = 0;
			foreach (CardType ct in cardTypes)
			{
				parameters.Clear();
				parameters.Add(createParam("@card_id", DbType.Int64, id));
				parameters.Add(createParam("@card_type_id", DbType.Int64, ct.ID));

				bool hasTextField = false;
				bool hasCardField = false;
				bool hasListField = false;

				foreach (CardTypeField f in ct.Fields)
				{
					switch (f.FieldType)
					{
						case DataType.Text:
							hasTextField = true;
							break;
						case DataType.Card:
							hasCardField = true;
							break;
						case DataType.List:
							hasListField = true;
							break;
						default:
							errorMessage += "Unknown field type: " + f.FieldType.ToString();
							break;
					}

					if (hasTextField && hasCardField && hasListField)
						break;
				}

				// get text fields
				if (hasTextField)
				{
					List<string[]> textFieldResult;
					sql = @"
					SELECT `ft`.`value`, `ctf`.`card_type_id`
					FROM `field_text` `ft`
						JOIN `card_type_field` `ctf` ON `ctf`.`id` = `ft`.`card_type_field_id`
					WHERE `ft`.`card_id` = @card_id
						AND `ctf`.`card_type_id` = @card_type_id
					ORDER BY `ctf`.`sort_order` ASC;";
					errorMessage += execReadField(sql, path, parameters, out textFieldResult, "value");

					// fill text fields
					for (int i = 0, j = 0; i < ct.Fields.Count && j < textFieldResult.Count; i++)
					{
						if (ct.Fields[i].FieldType == DataType.Text)
						{
							card.Fields[pastFields + i] = textFieldResult[j][0];
							j++;
						}
					}
				}

				// get card fields
				if (hasCardField)
				{
					List<string[]> cardFieldResult;
					sql = @"
					SELECT `ft`.*
					FROM `field_card` `fc`
						JOIN `card_type_field` `ctf` ON `ctf`.`id` = `fc`.`card_type_field_id`
					WHERE `ft`.`card_id` = @card_id
						AND `ctf`.`card_type_id` = @card_type_id
					ORDER BY `ctf`.`sort_order` ASC;";
					errorMessage += execReadField(sql, path, parameters, out cardFieldResult, "value");

					// fill card fields
					for (int i = 0, j = 0; i < ct.Fields.Count && j < cardFieldResult.Count; i++)
					{
						if (ct.Fields[i].FieldType == DataType.Card)
						{
							card.Fields[pastFields + i] = cardFieldResult[j][0];
							j++;
						}
					}
				}

				// get list fields
				if (hasListField)
				{
					List<string[]> listFieldResult;
					sql = @"
					SELECT `fl`.*, `ctf`.`sort_order` AS `ctf_sort_order`
					FROM `field_list` `fl`
						JOIN `card_type_field` `ctf` ON `ctf`.`id` = `fl`.`card_type_field_id`
					WHERE `fl`.`card_id` = @card_id
						AND `ctf`.`card_type_id` = @card_type_id
					ORDER BY `ctf`.`sort_order` ASC, `fl`.`sort_order` ASC;";
					errorMessage += execReadField(sql, path, parameters, out listFieldResult, "value", "ctf_sort_order");

					List<Card> items = new List<Card>();
					string lastSortOrder = null;

					int fieldIndex = -1;
					for (int i = 0; i < listFieldResult.Count && fieldIndex < ct.Fields.Count; i++)
					{
						// if current list field is complete
						if (listFieldResult[i][1] != lastSortOrder)
						{
							lastSortOrder = listFieldResult[i][1];

							if (items.Count > 0)
							{
								do
								{
									fieldIndex++;
								} while (ct.Fields[fieldIndex].FieldType != DataType.List);

								card.Fields[pastFields + fieldIndex] = items;
								items = new List<Card>();
							}
						}

						// add list item
						Card listCard;
						errorMessage += getCard(listFieldResult[i][0], path, out listCard);
						items.Add(listCard);
					}

					// add last items
					if (items.Count > 0)
					{
						do
						{
							fieldIndex++;
						} while (ct.Fields[fieldIndex].FieldType != DataType.List);

						card.Fields[pastFields + fieldIndex] = items;
					}
				}

				pastFields += ct.Fields.Count;
			}

			return errorMessage;
		}

		/// <summary>Adds a new list item.</summary>
		/// <param name="card">The card that owns the list.</param>
		/// <param name="cardTypeField">The list to add to.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string newListItem(Card card, CardTypeField cardTypeField, string path)
		{
			string errorMessage = string.Empty;

			// get next sort order
			string sql = "SELECT COALESCE(MAX(`sort_order`), 0) + 1 AS `next_sort_order` FROM `field_list` WHERE `card_id` = @card_id AND `card_type_field_id` = @card_type_field_id;";
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_id", DbType.Int64, card.ID));
			parameters.Add(createParam("@card_type_field_id", DbType.Int64, cardTypeField.ID));

			string[] orderResult;
			errorMessage += execReadField(sql, path, parameters, out orderResult, "next_sort_order");

			// add new card
			Card listItem;
			errorMessage += newCard(cardTypeField.ListType, path, out listItem);

			// add field_list record
			sql = @"
				INSERT INTO `field_list` (`card_id`, `card_type_field_id`, `value`, `sort_order`)
				VALUES (@card_id, @card_type_field_id, @value, @sort_order);";

			parameters.Add(createParam("@value", DbType.Int64, listItem.ID));
			parameters.Add(createParam("@sort_order", DbType.Int64, orderResult[0]));

			errorMessage += execNonQuery(sql, path, parameters);

			return errorMessage;
		}

		/// <summary>Gets the IDs and names of all card types.</summary>
		/// <param name="path">The path of the current database.</param>
		/// <param name="results">A list of two-dimensional arrays. [0] = ID; [1] = Name.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTypeIDsAndNames(string path, out List<string[]> results)
		{
			string errorMessage = string.Empty;

			string sql = "SELECT `id`, `name` FROM `card_type` WHERE `context` = @context ORDER BY `name` ASC;";
			errorMessage += execReadField(sql, path, new SQLiteParameter[] { createParam("@context", DbType.Int64, (int)CardTypeContext.Standalone) }, out results, "id", "name");

			return errorMessage;
		}

		/// <summary>Gets the IDs and names of all fields in a card type.</summary>
		/// <param name="cardTypeID">The database ID of the owning card type.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="results">A list of two-dimensional arrays. [0] = ID; [1] = Name.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTypeFieldIDsAndNames(string cardTypeID, string path, out List<string[]> results)
		{
			string errorMessage = string.Empty;

			string sql = "SELECT `id`, `name` FROM `card_type_field` WHERE `card_type_id` = @card_type_id ORDER BY `sort_order` ASC;";
			errorMessage += execReadField(sql, path, createParam("@card_type_id", DbType.Int64, cardTypeID), out results, "id", "name");

			return errorMessage;
		}

		/// <summary>Search for a specific string query.</summary>
		/// <param name="query">The search query.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string search(string query, string path, out string[] cardIDs)
		{
			// TODO: sort by relevance (number of keyword matches)
			string errorMessage = string.Empty;
			cardIDs = new string[0];

			query = query.ToUpper();
			string[] keywords = query.Split(' ');

			StringBuilder sql = new StringBuilder();
			foreach (string keyword in keywords)
			{
				sql.Append((sql.Length > 0 ? " OR " : "") + "UPPER(`ft`.`value`) LIKE '%" + keyword + "%'");
			}

			if (sql.Length > 0)
			{
				sql.Insert(0, @"
					SELECT COALESCE(`fl`.`card_id`, `c`.`id`) AS `id`
					FROM `field_text` `ft`
						JOIN `card` `c` ON `c`.`id` = `ft`.`card_id`
						LEFT JOIN `field_list` `fl` ON `fl`.`value` = `c`.`id`
					WHERE ");
				sql.Append(";");

				List<string[]> results;
				errorMessage += execReadField(sql.ToString(), path, (SQLiteParameter)null, out results, "id");

				cardIDs = new string[results.Count];
				for (int i = 0; i < cardIDs.Length; i++)
				{
					cardIDs[i] = results[i][0];
				}
			}

			return errorMessage;
		}

		#region DB functions

		/// <summary>Generates a connection string to the database.</summary>
		/// <param name="path">The path of the current database.</param>
		/// <returns>The connection string.</returns>
		public static string genConnectionString(string path)
		{
			return "Data Source=" + path + ";Version=3;Foreign Keys=true;";
		}

		/// <summary>Executes a nonquery SQL string.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		public static string execNonQuery(string sql, string path, params SQLiteParameter[] parameters)
		{
			return execNonQuery(sql, path, (IEnumerable<SQLiteParameter>)parameters);
		}

		/// <summary>Executes a nonquery SQL string.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		public static string execNonQuery(string sql, string path, IEnumerable<SQLiteParameter> parameters)
		{
			string errorMessage = string.Empty;

			try
			{
				using (SQLiteConnection con = new SQLiteConnection(genConnectionString(path)))
				{
					con.Open();

					using (SQLiteCommand cmd = new SQLiteCommand("BEGIN;\n" + sql + "\nCOMMIT;", con))
					{
						if (parameters != null)
						{
							foreach (SQLiteParameter p in parameters)
							{
								cmd.Parameters.Add(p);
							}
						}

						cmd.ExecuteNonQuery();
					}

				}
			}
			catch (Exception ex)
			{
				errorMessage += ex.Message;
			}

			return errorMessage;
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameter">A parameter used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
		/// <returns>Any error messages.</returns>
		public static string execReadField(string sql, string path, SQLiteParameter parameter, out string[] result, params string[] fieldName)
		{
			return execReadField(sql, path, new SQLiteParameter[] { parameter }, out result, fieldName);
		}

		/// <summary>Executes an SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
		/// <returns>Any error messages.</returns>
		public static string execReadField(string sql, string path, IEnumerable<SQLiteParameter> parameters, out string[] result, params string[] fieldName)
		{
			string errorMessage = string.Empty;
			result = null;

			try
			{
				using (SQLiteConnection con = new SQLiteConnection(genConnectionString(path)))
				{
					con.Open();

					using (SQLiteCommand cmd = new SQLiteCommand("BEGIN;\n" + sql + "\nCOMMIT;", con))
					{
						if (parameters != null)
						{
							foreach (SQLiteParameter p in parameters)
							{
								cmd.Parameters.Add(p);
							}
						}

						using (SQLiteDataReader reader = cmd.ExecuteReader())
						{
							result = new string[fieldName.Length];

							if (reader.Read())
							{
								for (int i = 0; i < fieldName.Length; i++)
								{
									result[i] = reader[fieldName[i]].ToString();
								}
							}
							else
							{
								errorMessage += "Field `" + fieldName + "` was empty.";
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				errorMessage += ex.Message;
			}

			return errorMessage;
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameter">A parameter used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
		public static string execReadField(string sql, string path, SQLiteParameter parameter, out List<string[]> result, params string[] fieldName)
		{
			return execReadField(sql, path, (parameter == null ? null : new SQLiteParameter[] { parameter }), out result, fieldName);
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
		/// <returns>Any error messages.</returns>
		public static string execReadField(string sql, string path, IEnumerable<SQLiteParameter> parameters, out List<string[]> result, params string[] fieldName)
		{
			string errorMessage = string.Empty;
			result = null;

			try
			{
				using (SQLiteConnection con = new SQLiteConnection(genConnectionString(path)))
				{
					con.Open();

					using (SQLiteCommand cmd = new SQLiteCommand("BEGIN;\n" + sql + "\nCOMMIT;", con))
					{
						if (parameters != null)
						{
							foreach (SQLiteParameter p in parameters)
							{
								cmd.Parameters.Add(p);
							}
						}

						using (SQLiteDataReader reader = cmd.ExecuteReader())
						{
							result = new List<string[]>();

							while (reader.Read())
							{
								string[] results = new string[fieldName.Length];
								for (int i = 0; i < fieldName.Length; i++)
								{
									results[i] = reader[fieldName[i]].ToString();
								}

								result.Add(results);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				errorMessage += ex.Message;
				result = null;
			}

			return errorMessage;
		}

		/// <summary>Executes a SQL string and returns a dataset.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="ds">The returned dataset.</param>
		/// <returns>Any error messages.</returns>
		public static string execDataSet(string sql, string path, IEnumerable<SQLiteParameter> parameters, out DataTable dt)
		{
			string errorMessage = string.Empty;
			dt = new DataTable();

			try
			{
				using (SQLiteConnection con = new SQLiteConnection(genConnectionString(path)))
				using (SQLiteDataAdapter da = new SQLiteDataAdapter(sql, con))
				using (SQLiteCommandBuilder b = new SQLiteCommandBuilder(da))
				{
					if (parameters != null)
					{
						foreach (SQLiteParameter p in parameters)
						{
							da.SelectCommand.Parameters.Add(p);
						}
					}

					con.Open();
					da.Fill(dt);
				}
			}
			catch (Exception ex)
			{
				errorMessage += ex.Message;
			}

			return errorMessage;
		}

		/// <summary>Creates a SQLiteParameter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="type">The datatype of the parameter.</param>
		/// <param name="value">The initial value of the parameter.</param>
		/// <returns>A new SQLiteParameter object.</returns>
		public static SQLiteParameter createParam(string name, DbType type, object value)
		{
			SQLiteParameter param = new SQLiteParameter(name, type);
			param.Value = value;
			return param;
		}

		#endregion DB Functions

		#region General Tools

		/// <summary>Finds the index of the provided value in a list.  Returns -1 if not found.</summary>
		public static int indexOf<T>(IEnumerable<T> list, T value)
		{
			int index = 0;

			foreach (T element in list)
			{
				if (element.Equals(value))
					return index;
				index++;
			}

			return -1;
		}

		/// <summary>Resets the parameter names to the beginning.</summary>
		public static void resetParamNames()
		{
			paramNum = 0;
			paramCurName = null;
		}

		/// <summary>Gets the next parameter name.</summary>
		public static string getNextParamName(string label)
		{
			paramNum++;
			paramCurName = "@" + label + paramNum;
			return paramCurName;
		}

		#endregion General Tools

		#endregion Methods
	}
}
