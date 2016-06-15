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

		#region Name Templates

		private const string NewCardTypeName = "Card Type {0}";
		private static readonly int NewCardTypeNameIndex;
		private static readonly string NewCardTypeNameStart;
		private static readonly string NewCardTypeNameEnd;
		private static readonly string NewCardTypeNameLike;

		private const string NewCardTypeFieldName = "Field {0}";
		private static readonly int NewCardTypeFieldNameIndex;
		private static readonly string NewCardTypeFieldNameStart;
		private static readonly string NewCardTypeFieldNameEnd;
		private static readonly string NewCardTypeFieldNameLike;

		private const string NewArrangementName = "Arrangement {0}";
		private static readonly int NewArrangementNameIndex;
		private static readonly string NewArrangementNameStart;
		private static readonly string NewArrangementNameEnd;
		private static readonly string NewArrangementNameLike;

		#endregion Name Templates

		#endregion Members

		#region Constructors

		static CardManager()
		{
			// create new name templates
			NewCardTypeNameIndex = NewCardTypeName.IndexOf("{0}");
			NewCardTypeNameStart = NewCardTypeName.Substring(0, NewCardTypeNameIndex);
			NewCardTypeNameEnd = NewCardTypeName.Substring(NewCardTypeNameIndex + 3);
			NewCardTypeNameLike = NewCardTypeNameStart + "%" + NewCardTypeNameEnd;

			NewCardTypeFieldNameIndex = NewCardTypeFieldName.IndexOf("{0}");
			NewCardTypeFieldNameStart = NewCardTypeFieldName.Substring(0, NewCardTypeFieldNameIndex);
			NewCardTypeFieldNameEnd = NewCardTypeFieldName.Substring(NewCardTypeFieldNameIndex + 3);
			NewCardTypeFieldNameLike = NewCardTypeFieldNameStart + "%" + NewCardTypeFieldNameEnd;

			NewArrangementNameIndex = NewArrangementName.IndexOf("{0}");
			NewArrangementNameStart = NewArrangementName.Substring(0, NewArrangementNameIndex);
			NewArrangementNameEnd = NewArrangementName.Substring(NewArrangementNameIndex + 3);
			NewArrangementNameLike = NewArrangementNameStart + "%" + NewArrangementNameEnd;
		}

		#endregion Constructors

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
					`name` TEXT NULL DEFAULT NULL,
					`parent_id` INTEGER NULL DEFAULT NULL
						REFERENCES `card_type` (`id`)
						ON UPDATE CASCADE ON DELETE SET NULL
						DEFERRABLE INITIALLY DEFERRED,
					`context` INTEGER NOT NULL,
					`color` INTEGER NOT NULL DEFAULT 32768,
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
					ON `field_list` (`value`);

				CREATE TABLE `arrangement` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`name` TEXT NOT NULL DEFAULT '',
					UNIQUE (`name`)
				);

				CREATE TABLE `arrangement_card` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`arrangement_id` INTEGER NOT NULL
						REFERENCES `arrangement` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`card_id` INTEGER NOT NULL
						REFERENCES `card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					UNIQUE (`arrangement_id`, `card_id`)
				);

				CREATE INDEX `idx_ac_arrangement_id`
					ON `arrangement_card` (`arrangement_id`);

				CREATE INDEX `idx_ac_card_id`
					ON `arrangement_card` (`card_id`);

				INSERT INTO `arrangement` (`name`) VALUES ('Arrangement 1');

				CREATE TABLE `arrangement_card_standalone` (
					`arrangement_card_id` INTEGER NOT NULL PRIMARY KEY
						REFERENCES `arrangement_card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`x` INTEGER NOT NULL DEFAULT 0,
					`y` INTEGER NOT NULL DEFAULT 0,
					`width` INTEGER NOT NULL DEFAULT 0
				);

				CREATE TABLE `arrangement_card_list` (
					`arrangement_card_id` INTEGER NOT NULL PRIMARY KEY
						REFERENCES `arrangement_card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED
				);

				CREATE TABLE `arrangement_field_text` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`arrangement_card_id` INTEGER NOT NULL
						REFERENCES `arrangement_card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`card_type_field_id` INTEGER NOT NULL
						REFERENCES `card_type_field` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`height_increase` INTEGER NOT NULL DEFAULT 0,
					UNIQUE (`arrangement_card_id`, `card_type_field_id`)
				);

				CREATE INDEX `idx_aft_arrangement_card_id`
					ON `arrangement_field_text` (`arrangement_card_id`);

				CREATE INDEX `idx_aft_card_type_field_id`
					ON `arrangement_field_text` (`card_type_field_id`);";

			return execNonQuery(sql, path, null);
		}

		#region Card Types

		/// <summary>Creates a new card type.</summary>
		/// <param name="context">The context of the card type, whether standalone or a list.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="id">The database ID of the card type.</param>
		/// <returns>Any error messages.</returns>
		public static string newCardType(CardTypeContext context, string path, out string id)
		{
			string errorMessage = string.Empty;

			// get new name
			string nameSql = "SELECT `name` FROM `card_type` WHERE `name` LIKE @name;";

			List<string> names;
			errorMessage += execReadField(nameSql, path, createParam("@name", DbType.String, NewCardTypeNameLike), out names, "name");

			string name = findNextName(names, NewCardTypeName, NewCardTypeNameStart, NewCardTypeNameEnd, NewCardTypeNameIndex);

			// insert record
			string sql = @"
				INSERT INTO `card_type` (`name`, `context`) VALUES (@name, @context);
				SELECT LAST_INSERT_ROWID() AS `id`;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@name", DbType.String, name),
				createParam("@context", DbType.Int64, (int)context)
			};

			errorMessage += execReadField(sql, path, parameters, out id, "id");

			// add title field
			errorMessage += saveCardType(id, new CardTypeChg(CardTypeChange.CardTypeFieldAdd, "Name"), path);

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
				case CardTypeChange.CardTypeColorChange:
					errorMessage += changeCardTypeColor(cardTypeID, (int)change.Parameters[0], path);
					break;
				case CardTypeChange.CardTypeRemove:
					errorMessage += removeCardType(cardType, path);
					break;
				case CardTypeChange.CardTypeFieldAdd:
					errorMessage += addCardTypeField(cardType.ID, ((change.Parameters == null || change.Parameters.Length == 0) ? null : (string)change.Parameters[0]), path);
					break;
				case CardTypeChange.CardTypeFieldNameChange:
					sql = "UPDATE `card_type_field` SET `name` = @name WHERE `id` = @id;";
					errorMessage += execNonQuery(sql, path, createParam("@name", DbType.String, (string)change.Parameters[1]), createParam("@id", DbType.Int64, (string)change.Parameters[0]));
					break;
				case CardTypeChange.CardTypeFieldTypeChange:
					errorMessage += changeCardTypeFieldType((string)change.Parameters[0], (DataType)change.Parameters[1], path);
					break;
				case CardTypeChange.CardTypeFieldCardTypeChange:
					errorMessage += changeCardTypeFieldCardType((string)change.Parameters[0], (string)change.Parameters[1], path);
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

			string descendents;
			errorMessage += getCardTypeDescendents(cardType.ID, path, out descendents);

			// remove old inherited fields
			if (!string.IsNullOrEmpty(cardType.ParentID))
			{
				List<string> temp;
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
						SELECT `c`.`id`, `ctf`.`id`, ''
						FROM `card` `c`
							JOIN `card_type_field` `ctf` ON `ctf`.`id` IN (" + fieldText.ToString() + @")
						WHERE `c`.`card_type_id` IN (" + descendents + ");");
				}

				if (fieldCard.Length > 0)
				{
					sql.Append(@"
						INSERT INTO `field_card` (`card_id`, `card_type_field_id`, `value`)
						SELECT `c`.`id`, `ctf`.`id`, NULL
						FROM `card` `c`
							JOIN `card_type_field` `ctf` ON `ctf`.`id` IN (" + fieldCard.ToString() + @")
						WHERE `c`.`card_type_id` IN (" + descendents + ");");
				}
			}

			// execute query
			errorMessage += execNonQuery(sql.ToString(), path, parameters);

			return errorMessage;
		}

		/// <summary>Changes a card type's color.</summary>
		/// <param name="cardTypeID">The database ID of the card type to change.</param>
		/// <param name="color">The new color.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		private static string changeCardTypeColor(string cardTypeID, int color, string path)
		{
			string errorMessage = string.Empty;

			string sql = "UPDATE `card_type` SET `color` = @color WHERE `id` = @id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@id", DbType.Int64, cardTypeID),
				createParam("@color", DbType.Int64, color)
			};

			errorMessage += execNonQuery(sql, path, parameters);

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
		/// <param name="cardTypeID">The database ID of the owning card type.</param>
		/// <param name="name">The name of the field.</param>
		/// <param name="path">The path of the curent database.</param>
		/// <returns>Any error messages.</returns>
		private static string addCardTypeField(string cardTypeID, string name, string path)
		{
			string errorMessage = string.Empty;

			// get new name
			if (string.IsNullOrEmpty(name))
			{
				string nameSql = "SELECT `name` FROM `card_type_field` WHERE `card_type_id` = @card_type_id AND `name` LIKE @name;";

				SQLiteParameter[] nameParams = new SQLiteParameter[]
				{
					createParam("@card_type_id", DbType.Int64, cardTypeID),
					createParam("@name", DbType.String, NewCardTypeFieldNameLike)
				};

				List<string> names;
				errorMessage += execReadField(nameSql, path, nameParams, out names, "name");

				name = findNextName(names, NewCardTypeFieldName, NewCardTypeFieldNameStart, NewCardTypeFieldNameEnd, NewCardTypeFieldNameIndex);
			}

			// get all affected card type ids
			string descendents;
			errorMessage += getCardTypeDescendents(cardTypeID, path, out descendents);

			// insert card type field and card records
			string sql = @"
				INSERT INTO `card_type_field` (`card_type_id`, `name`, `field_type`, `sort_order`)
				VALUES (@card_type_id, @name, @field_type, (SELECT COALESCE(MAX(`sort_order`), 0) + 1 FROM `card_type_field` WHERE `card_type_id` = @card_type_id));

				CREATE TEMPORARY TABLE `ctf_id`(`id` INTEGER PRIMARY KEY);
				INSERT INTO `ctf_id` VALUES (LAST_INSERT_ROWID());

				INSERT INTO `field_text` (`card_id`, `card_type_field_id`, `value`)
				SELECT `c`.`id`, `ctf_id`.`id`, '' FROM `card` `c` JOIN `ctf_id` WHERE `c`.`card_type_id` IN (" + descendents + @");

				INSERT INTO `arrangement_field_text` (`arrangement_card_id`, `card_type_field_id`)
				SELECT `ac`.`id`, `ctf_id`.`id`
				FROM `arrangement_card` `ac`
					JOIN `card` `c` ON `c`.`id` = `ac`.`card_id`
					JOIN `ctf_id`
				WHERE `c`.`card_type_id` IN (" + descendents + ");";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@card_type_id", DbType.Int64, cardTypeID),
				createParam("@name", DbType.String, name),
				createParam("@field_type", DbType.Int64, (int)DataType.Text)
			};

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

			CardTypeField oldField;
			string cardTypeID;
			errorMessage += getCardTypeField(fieldID, path, out oldField, out cardTypeID);

			StringBuilder sql = new StringBuilder(@"
						UPDATE `card_type_field` SET `field_type` = @field_type, `ref_card_type_id` = NULL WHERE `id` = @card_type_field_id;");
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_type_field_id", DbType.Int64, fieldID));
			parameters.Add(createParam("@card_type_id", DbType.Int64, cardTypeID));
			parameters.Add(createParam("@field_type", DbType.Int64, (int)newType));
			
			// remove old fields
			switch (oldField.FieldType)
			{
				case DataType.Text:
					sql.Append(@"
						DELETE FROM `field_text` WHERE `card_type_field_id` = @card_type_field_id;
						DELETE FROM `arrangement_field_text` WHERE `card_type_field_id` = @card_type_field_id;");
					break;
				case DataType.Card:
					sql.Append(@"
						DELETE FROM `field_card` WHERE `card_type_field_id` = @card_type_field_id;");
					break;
				case DataType.List:
					sql.Append(@"
						DELETE FROM `card_type` WHERE `id` = @list_type_id;
						DELETE FROM `field_list` WHERE `card_type_field_id` = @card_type_field_id;");
					parameters.Add(createParam("@list_type_id", DbType.Int64, oldField.RefCardTypeID));
					break;
				default:
					errorMessage += "Unkown field type: " + oldField.FieldType.ToString();
					break;
			}

			// insert new fields
			string descendents;
			errorMessage += getCardTypeDescendents(cardTypeID, path, out descendents);

			switch (newType)
			{
				case DataType.Text:
					sql.Append(@"
						INSERT INTO `field_text` (`card_id`, `card_type_field_id`, `value`)
						SELECT `id`, @card_type_field_id, '' FROM `card` WHERE `card_type_id` = @card_type_id;

						INSERT INTO `arrangement_field_text` (`arrangement_card_id`, `card_type_field_id`)
						SELECT `ac`.`id`, @card_type_field_id FROM `arrangement_card` `ac` JOIN `card` `c` ON `c`.`id` = `ac`.`card_id` WHERE `c`.`card_type_id` = @card_type_id;");
					break;
				case DataType.Card:
					sql.Append(@"
						INSERT INTO `field_card` (`card_id`, `card_type_field_id`, `value`)
						SELECT `id`, @card_type_field_id, NULL FROM `card` WHERE `card_type_id` = @card_type_id;");
					break;
				case DataType.List:
					sql.Append(@"
						INSERT INTO `card_type` (`context`) VALUES (@list_context);
						UPDATE `card_type_field` SET `ref_card_type_id` = LAST_INSERT_ROWID() WHERE `id` = @card_type_field_id;");
					parameters.Add(createParam("@list_context", DbType.Int64, (int)CardTypeContext.List));
					break;
				default:
					errorMessage += "Unknown field type: " + newType.ToString();
					break;
			}

			// execute sql
			errorMessage += execNonQuery(sql.ToString(), path, parameters);

			return errorMessage;
		}

		/// <summary>Changes a field's referred card type ID (for Card type fields).</summary>
		/// <param name="fieldID">The database ID of the field.</param>
		/// <param name="newTypeID">The database ID of the card type to refer to.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		private static string changeCardTypeFieldCardType(string fieldID, string newTypeID, string path)
		{
			string errorMessage = string.Empty;

			CardTypeField field;
			errorMessage += getCardTypeField(fieldID, path, out field);

			if (field.FieldType != DataType.Card)
				return "You Cannot change ref_card_type_id on a non-card type field.";

			StringBuilder sql = new StringBuilder();
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();

			CardType newType = null;
			if (!string.IsNullOrEmpty(newTypeID))
				errorMessage += getCardType(newTypeID, path, out newType);

			// clear out invalidated values
			if (newType != null)
			{
				string newDescendents;
				errorMessage += getCardTypeDescendents(newType.ID, path, out newDescendents);

				sql.Append(@"
					UPDATE `field_card` SET `value` = NULL
					WHERE `card_type_field_id` = @card_type_field_id
						AND `value` IS NOT NULL
						AND `value` NOT IN (SELECT `id` FROM `card` WHERE `card_type_id` IN (" + newDescendents + "));");

				parameters.Add(createParam("@ref_card_type_id", DbType.Int64, newTypeID));
			}
			else
			{
				parameters.Add(createParam("@ref_card_type_id", DbType.Int64, DBNull.Value));
			}

			sql.Append(@"
				UPDATE `card_type_field` SET `ref_card_type_id` = @ref_card_type_id WHERE `id` = @card_type_field_id;");

			parameters.Add(createParam("@card_type_field_id", DbType.Int64, fieldID));
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
			return getCardType(id, path, out cardType, false);
		}

		/// <summary>Retrieves a card type.</summary>
		/// <param name="id">The database id of one of the card type's fields.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="cardType">The requested card type.</param>
		/// <returns>Any error messages..</returns>
		public static string getCardTypeFromFieldID(string id, string path, out CardType cardType)
		{
			return getCardType(id, path, out cardType, true);
		}

		/// <summary>Retrieves a card type.</summary>
		/// <param name="id">The database id of the card type or one of its fields.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="cardType">The requested card type.</param>
		/// <param name="fromFieldID">Whether or not the id supplied is the database ID of one of the card type's fields.</param>
		/// <returns>Any error messages..</returns>
		private static string getCardType(string id, string path, out CardType cardType, bool fromFieldID)
		{
			string errorMessage = string.Empty;

			cardType = null;

			// get card type
			string sql = fromFieldID
				? "SELECT `ct`.* FROM `card_type` `ct` JOIN `card_type_field` `ctf` ON `ctf`.`card_type_id` = `ct`.`id` WHERE `ctf`.`id` = @id;"
				: "SELECT * FROM `card_type` WHERE `id` = @id;";
			List<SQLiteParameter> parameters = new List<SQLiteParameter>() { createParam("@id", DbType.Int64, id) };
			string[] cardTypeResult;
			errorMessage += execReadField(sql, path, parameters, out cardTypeResult, "id", "name", "parent_id", "context", "color");

			cardType = new CardType(cardTypeResult[0], cardTypeResult[1], cardTypeResult[2], (CardTypeContext)int.Parse(cardTypeResult[3]), int.Parse(cardTypeResult[4]), 0);

			// get fields
			parameters.Clear();
			parameters.Add(createParam("@id", DbType.Int64, cardType.ID));

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

			// oldest first
			ancestry.Reverse();

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
				errorMessage += execReadField(sql, path, createParam("@id", DbType.Int64, parentID), out parentID, "parent_id");
			}

			ancestry.Reverse();

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

			StringBuilder ids = new StringBuilder();
			ids.Append(cardTypeID);
			bool hasChildren = true;

			while (hasChildren)
			{
				List<string> results;
				errorMessage += execReadField((sql + ids.ToString() + ");"), path, (List<SQLiteParameter>)null, out results, "id");
				hasChildren = results.Count > 0;
				foreach (string result in results)
				{
					descendents.Add(result);

					ids.Append(",");
					ids.Append(result);
				}
			}

			return errorMessage;
		}

		/// <summary>Gets a card type and all of its descendents' database IDs.</summary>
		/// <param name="cardTypeID">The parent card type.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="descendents">The retrieved card types' database IDs.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTypeDescendents(string cardTypeID, string path, out string descendents)
		{
			string errorMessage = string.Empty;

			string sql = "SELECT `id` FROM `card_type` WHERE `parent_id` IN (";

			StringBuilder ids = new StringBuilder();
			ids.Append(cardTypeID);
			bool hasChildren = true;

			while (hasChildren)
			{
				List<string> results;
				errorMessage += execReadField((sql + ids.ToString() + ");"), path, (List<SQLiteParameter>)null, out results, "id");
				hasChildren = results.Count > 0;
				foreach (string result in results)
				{
					ids.Append(",");
					ids.Append(result);
				}
			}

			descendents = ids.ToString();

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

		/// <summary>Gets the IDs and names of all card types exept the specified card type and its descendents.</summary>
		/// <param name="cardTypeID">The card type to ignore.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="results">A list of two-dimensional arrays. [0] = ID; [1] = Name.</param>
		/// <returns>Any error messages.</returns>
		public static string getAllButDescendents(string cardTypeID, string path, out List<string[]> results)
		{
			string errorMessage = string.Empty;

			string descendents;
			errorMessage += getCardTypeDescendents(cardTypeID, path, out descendents);

			string sql = "SELECT `id`, `name` FROM `card_type` WHERE `id` NOT IN (" + descendents + ");";
			errorMessage += execReadField(sql, path, (IEnumerable<SQLiteParameter>)null, out results, "id", "name");

			return errorMessage;
		}

		#endregion Card Types

		#region Cards

		/// <summary>Creates a new card.</summary>
		/// <param name="cardTypes">The type of card and its ancestors.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="card">The new card.</param>
		/// <returns>Any error messages.</returns>
		public static string newCard(List<CardType> cardTypes, string path, out string cardID)
		{
			string errorMessage = string.Empty;

			StringBuilder sql = new StringBuilder();
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			resetParamNames();

			sql.Append(@"
				INSERT INTO `card` (`card_type_id`) VALUES (@card_type_id);
				SELECT LAST_INSERT_ROWID() AS `id`;");
			parameters.Add(createParam("@card_type_id", DbType.Int64, cardTypes[cardTypes.Count - 1].ID));

			errorMessage += execReadField(sql.ToString(), path, parameters, out cardID, "id");

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

		/// <summary>Saves a card text field.</summary>
		/// <param name="value">The value to save to the card text field.</param>
		/// <param name="cardID">The card's database ID.</param>
		/// <param name="cardTypeFieldID">The card type field's database ID.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string saveCardTextField(string value, string cardID, string cardTypeFieldID, string path)
		{
			string errorMessage = string.Empty;

			string sql = "UPDATE `field_text` SET `value` = @value WHERE `card_id` = @card_id AND `card_type_field_id` = @card_type_field_id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@value", DbType.String, value),
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID)
			};

			errorMessage += execNonQuery(sql, path, parameters);

			return errorMessage;
		}

		/// <summary>Saves a card card field.</summary>
		/// <param name="value">The value to save to the card card field.</param>
		/// <param name="cardID">The card's database ID.</param>
		/// <param name="cardTypeFieldID">The card type field's database ID.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string saveCardCardField(string value, string cardID, string cardTypeFieldID, string path)
		{
			string errorMessage = string.Empty;

			string sql = "UPDATE `field_card` SET `value` = @value WHERE `card_id` = @card_id AND `card_type_field_id` = @card_type_field_id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@value", DbType.String, value),
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID)
			};

			errorMessage += execNonQuery(sql, path, parameters);

			return errorMessage;
		}

		/// <summary>Retrieves a card from the database.</summary>
		/// <param name="id">The database id of the card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="card">The card.</param>
		/// <param name="cardTypes">The card's card type and its ancestors.</param>
		/// <returns>Any error messages.</returns>
		public static string getCard(string id, string path, out Card card, List<CardType> cardTypes)
		{
			string errorMessage = string.Empty;
			card = new Card(cardTypes[cardTypes.Count - 1], id);

			List<SQLiteParameter> parameters = new List<SQLiteParameter>();

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
					List<string> textFieldResult;
					string sql = @"
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
							card.Fields[pastFields + i] = textFieldResult[j];
							j++;
						}
					}
				}

				// get card fields
				if (hasCardField)
				{
					List<string> cardFieldResult;
					string sql = @"
					SELECT `fc`.*
					FROM `field_card` `fc`
						JOIN `card_type_field` `ctf` ON `ctf`.`id` = `fc`.`card_type_field_id`
					WHERE `fc`.`card_id` = @card_id
						AND `ctf`.`card_type_id` = @card_type_id
					ORDER BY `ctf`.`sort_order` ASC;";
					errorMessage += execReadField(sql, path, parameters, out cardFieldResult, "value");

					// fill card fields
					for (int i = 0, j = 0; i < ct.Fields.Count && j < cardFieldResult.Count; i++)
					{
						if (ct.Fields[i].FieldType == DataType.Card)
						{
							card.Fields[pastFields + i] = cardFieldResult[j];
							j++;
						}
					}
				}

				// get list fields
				if (hasListField)
				{
					List<string[]> listFieldResult;
					string sql = @"
					SELECT `fl`.`value`, `ctf`.`sort_order` AS `ctf_sort_order`
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
								card.Fields[pastFields + fieldIndex] = items;
								items = new List<Card>();
							}

							do
							{
								fieldIndex++;
							} while (ct.Fields[fieldIndex].FieldType != DataType.List);
						}

						// add list item
						Card listCard;
						errorMessage += getCard(listFieldResult[i][0], path, out listCard, new List<CardType>() { ct.Fields[fieldIndex].ListType });
						items.Add(listCard);
					}

					// add last items
					if (items.Count > 0)
						card.Fields[pastFields + fieldIndex] = items;

					// fill empty lists
					for (int i = 0; i < ct.Fields.Count; i++)
					{
						fieldIndex = pastFields + i;
						if (ct.Fields[i].FieldType == DataType.List && card.Fields[fieldIndex] == null)
							card.Fields[fieldIndex] = new List<Card>();
					}
				}

				pastFields += ct.Fields.Count;
			}

			return errorMessage;
		}

		/// <summary>Gets the database ID of a card's card type.</summary>
		/// <param name="id">The database ID of the card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="cardTypeID">The database ID of the card type.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardCardTypeID(string id, string path, out string cardTypeID)
		{
			string errorMessage = string.Empty;

			string sql = "SELECT `card_type_id` FROM `card` WHERE `id` = @card_id;";

			errorMessage += execReadField(sql, path, createParam("@card_id", DbType.Int64, id), out cardTypeID, "card_type_id");

			return errorMessage;
		}

		/// <summary>Determines whether or not a card exists in the database.</summary>
		/// <param name="id">The database ID of the card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="errorMessage">Any error messages.</param>
		/// <returns>Whether or not the card exists in the database.</returns>
		public static bool cardExists(string id, string path, ref string errorMessage)
		{
			string sql = "SELECT `id` FROM `card` WHERE `id` = @id LIMIT 1;";

			List<string> result;
			errorMessage += execReadField(sql, path, createParam("@id", DbType.Int64, id), out result, "id");

			return result.Count > 0;
		}

		/// <summary>Adds a new list item.</summary>
		/// <param name="card">The card that owns the list.</param>
		/// <param name="cardTypeField">The list to add to.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string newListItem(Card card, CardTypeField cardTypeField, string path, out string id)
		{
			string errorMessage = string.Empty;

			// get next sort order
			string sql = "SELECT COALESCE(MAX(`sort_order`), 0) + 1 AS `next_sort_order` FROM `field_list` WHERE `card_id` = @card_id AND `card_type_field_id` = @card_type_field_id;";
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_id", DbType.Int64, card.ID));
			parameters.Add(createParam("@card_type_field_id", DbType.Int64, cardTypeField.ID));

			string orderResult;
			errorMessage += execReadField(sql, path, parameters, out orderResult, "next_sort_order");

			// add new card
			errorMessage += newCard(new List<CardType>() { cardTypeField.ListType }, path, out id);

			// add field_list record
			sql = @"
				INSERT INTO `field_list` (`card_id`, `card_type_field_id`, `value`, `sort_order`)
				VALUES (@card_id, @card_type_field_id, @value, @sort_order);

				INSERT INTO `arrangement_card` (`arrangement_id`, `card_id`)
				SELECT `arrangement_id`, @value FROM `arrangement_card` WHERE `card_id` = @card_id;

				INSERT INTO `arrangement_card_list` (`arrangement_card_id`)
				SELECT `id` FROM `arrangement_card` WHERE `card_id` = @value;

				INSERT INTO `arrangement_field_text` (`arrangement_card_id`, `card_type_field_id`)
				SELECT `ac`.`id`, `ctf`.`id`
				FROM `card_type_field` `ctf`
					JOIN `card_type` `ct` ON `ct`.`id` = `ctf`.`card_type_id`
					JOIN `card` `c` ON `c`.`card_type_id` = `ct`.`id`
					JOIN `arrangement_card` `ac` ON `ac`.`card_id` = `c`.`id`
				WHERE `c`.`id` = @value
					AND `ctf`.`field_type` = @text_type;";

			parameters.Add(createParam("@value", DbType.Int64, id));
			parameters.Add(createParam("@sort_order", DbType.Int64, orderResult));
			parameters.Add(createParam("@text_type", DbType.Int64, (int)DataType.Text));

			errorMessage += execNonQuery(sql, path, parameters);

			return errorMessage;
		}

		/// <summary>Search for a specific string query.</summary>
		/// <param name="query">The search query.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string search(string query, string cardTypes, string path, out string[] cardIDs)
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
				if (string.IsNullOrEmpty(cardTypes))
				{
					sql.Insert(0, @"
					SELECT DISTINCT COALESCE(`fl`.`card_id`, `c`.`id`) AS `id`
					FROM `field_text` `ft`
						JOIN `card` `c` ON `c`.`id` = `ft`.`card_id`
						LEFT JOIN `field_list` `fl` ON `fl`.`value` = `c`.`id`
					WHERE ");
				}
				else
				{
					sql.Insert(0, @"
					SELECT DISTINCT COALESCE(`c2`.`id`, `c`.`id`) AS `id`
					FROM `field_text` `ft`
						JOIN `card` `c` ON `c`.`id` = `ft`.`card_id`
						LEFT JOIN `field_list` `fl` ON `fl`.`value` = `c`.`id`
						LEFT JOIN `card` `c2` ON `c2`.`id` = `fl`.`card_id`
					WHERE (`c`.`card_type_id` IN (" + cardTypes + @")
						OR `c2`.`card_type_id` IN (" + cardTypes + @"))
						AND ");
				}

				sql.Append(";");

				List<string> results;
				errorMessage += execReadField(sql.ToString(), path, (SQLiteParameter)null, out results, "id");

				cardIDs = new string[results.Count];
				for (int i = 0; i < cardIDs.Length; i++)
				{
					cardIDs[i] = results[i];
				}
			}

			return errorMessage;
		}

		/// <summary>Gets the names of a list of cards.</summary>
		/// <param name="ids">The database IDs of the cards.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="names">The names of the cards.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardNames(IEnumerable<string> ids, string path, out string[] names)
		{
			string errorMessage = string.Empty;

			string sql = @"
				SELECT `ft`.`value`
				FROM `field_text` `ft`
					JOIN `card_type_field` `ctf` ON `ctf`.`id` = `ft`.`card_type_field_id`
					JOIN `card_type` `ct` ON `ct`.`id` = `ctf`.`card_type_id` AND `ct`.`parent_id` IS NULL
					LEFT JOIN `card_type_field` `ctf2` ON `ctf2`.`card_type_id` = `ctf`.`card_type_id` AND `ctf2`.`sort_order` < `ctf`.`sort_order`
				WHERE `ctf2`.`id` IS NULL
					AND `ft`.`card_id` IN (" + string.Join(",", ids) + ");";

			List<string> temp;
			errorMessage += execReadField(sql, path, (IEnumerable<SQLiteParameter>)null, out temp, "value");

			names = new string[temp.Count];
			for (int i = 0; i < names.Length; i++)
			{
				names[i] = temp[i];
			}

			return errorMessage;
		}

		/// <summary>Deletes a card from the database.</summary>
		/// <param name="cardID">The card's database ID.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string deleteCard(string cardID, string path)
		{
			string errorMessage = string.Empty;

			string sql = "DELETE FROM `card` WHERE `id` = @id;";

			errorMessage += execNonQuery(sql, path, createParam("@id", DbType.Int64, cardID));

			return errorMessage;
		}

		#endregion Cards

		#region Arrangements

		/// <summary>Adds a new arrangement.</summary>
		/// <param name="name">The name of the new arrangmenet.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="id">The database ID of the new arrangement.</param>
		/// <param name="newName">The name if the new arrangement (same as name unless name is null).</param>
		/// <returns>Any error messages.</returns>
		public static string addArrangement(string name, string path, out string id, out string newName)
		{
			string errorMessage = string.Empty;
			
			// get new name
			if (string.IsNullOrEmpty(name))
			{
				string nameSql = "SELECT `name` FROM `arrangement` WHERE `name` LIKE @name;";

				List<string> names;
				errorMessage += execReadField(nameSql, path, createParam("@name", DbType.String, NewArrangementNameLike), out names, "name");

				name = findNextName(names, NewArrangementName, NewArrangementNameStart, NewArrangementNameEnd, NewArrangementNameIndex);
			}

			string sql = @"
				INSERT INTO `arrangement` (`name`) VALUES (@name);
				SELECT LAST_INSERT_ROWID() AS `id`;";

			errorMessage += execReadField(sql, path, createParam("@name", DbType.String, name), out id, "id");

			newName = name;

			return errorMessage;
		}

		/// <summary>Removes an arrangement from the database.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string removeArrangement(string arrangementID, string path)
		{
			string errorMessage = string.Empty;

			string sql = "DELETE FROM `arrangement` WHERE `id` = @id;";
			errorMessage += execNonQuery(sql, path, createParam("@id", DbType.Int64, arrangementID));

			return errorMessage;
		}

		/// <summary>Gets the IDs and names of all arrangements.</summary>
		/// <param name="path">The path of the current database.</param>
		/// <param name="results">The results.</param>
		/// <returns>Any error messages.</returns>
		public static string getArrangementIDsAndNames(string path, out List<string[]> results)
		{
			string errorMessage = string.Empty;

			string sql = "SELECT `id`, `name` FROM `arrangement`;";

			errorMessage += execReadField(sql, path, (IEnumerable<SQLiteParameter>)null, out results, "id", "name");

			return errorMessage;
		}

		/// <summary>Retrieves an arrangement from the database.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="cards">The cards in the arrangement.</param>
		/// <returns>Any error messages.</returns>
		public static string getArrangement(string arrangementID, string path, out ArrangementCardStandalone[] cards)
		{
			string errorMessage = string.Empty;

			string sql = @"
				SELECT `ac`.`id`, `ac`.`card_id`, `acs`.`x`, `acs`.`y`, `acs`.`width`
				FROM `arrangement_card` `ac`
					JOIN `arrangement_card_standalone` `acs` ON `acs`.`arrangement_card_id` = `ac`.`id`
				WHERE `ac`.`arrangement_id` = @id;";

			List<string[]> results;
			errorMessage += execReadField(sql, path, createParam("@id", DbType.Int64, arrangementID), out results, "id", "card_id", "x", "y", "width");

			cards = new ArrangementCardStandalone[results.Count];

			SQLiteParameter[] listParams = new SQLiteParameter[]
			{
				null,
				createParam("@arrangement_id", DbType.Int64, arrangementID)
			};

			for (int i = 0; i < results.Count; i++)
			{
				// build arrangement card
				string[] result = results[i];
				ArrangementCardStandalone card = new ArrangementCardStandalone(result[0], result[1], null, int.Parse(result[2]), int.Parse(result[3]), int.Parse(result[4]));

				// get text fields
				card.TextFields = getArrangementCardTextFields(card.ID, path, ref errorMessage);

				// get list items
				sql = @"
					SELECT `ac`.`id`, `fl`.`card_id`
					FROM `field_list` `fl`
						JOIN `card_type_field` `ctf` ON `ctf`.`id` = `fl`.`card_type_field_id`
						JOIN `arrangement_card` `ac` ON `ac`.`card_id` = `fl`.`value`
						JOIN `arrangement_card_list` `acl` ON `acl`.`arrangement_card_id` = `ac`.`id`
					WHERE `fl`.`card_id` = @card_id
						AND `ac`.`arrangement_id` = @arrangement_id
					ORDER BY `ctf`.`sort_order`, `fl`.`sort_order`;";

				listParams[0] = createParam("@card_id", DbType.Int64, card.CardID);
				List<string[]> listFields;
				errorMessage += execReadField(sql, path, listParams, out listFields, "id", "card_id");

				if (listFields.Count > 0)
				{
					card.ListItems = new ArrangementCardList[listFields.Count];

					for (int listIndex = 0; listIndex < listFields.Count; listIndex++)
					{
						string[] f = listFields[listIndex];
						card.ListItems[listIndex] = new ArrangementCardList(f[0], f[1], getArrangementCardTextFields(f[0], path, ref errorMessage));
					}

				}

				// finish card
				cards[i] = card;
			}

			return errorMessage;
		}

		/// <summary>Retrieves the arrangement card text field settings.</summary>
		/// <param name="arrangementCardID">The database ID of the owning arrangement card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="fields">The text field settings.</param>
		/// <param name="errorMessage">Any error messages.</param>
		/// <returns>The text field settings.</returns>
		private static ArrangementFieldText[] getArrangementCardTextFields(string arrangementCardID, string path, ref string errorMessage)
		{
			string sql = @"
				SELECT `ctf`.`id`, `aft`.`height_increase`
				FROM `arrangement_field_text` `aft`
					JOIN `card_type_field` `ctf` ON `ctf`.`id` = `aft`.`card_type_field_id`
				WHERE `aft`.`arrangement_card_id` = @arrangement_card_id;";
			List<string[]> textFields;
			errorMessage += execReadField(sql, path, createParam("@arrangement_card_id", DbType.Int64, arrangementCardID), out textFields, "id", "height_increase");

			ArrangementFieldText[] fields = new ArrangementFieldText[textFields.Count];
			for (int j = 0; j < textFields.Count; j++)
			{
				fields[j] = new ArrangementFieldText(textFields[j][0], int.Parse(textFields[j][1]));
			}

			return fields;
		}

		/// <summary>Gets the database ID of an arrangement card.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="arrangementCardID">The database ID of the arrangement card.</param>
		/// <returns>Any error messages.</returns>
		public static string getArrangementCardID(string arrangementID, string cardID, string path, out string arrangementCardID)
		{
			string errorMessage = string.Empty;

			string sql = "SELECT `id` FROM `arrangement_card` WHERE `arrangement_id` = @arrangement_id AND `card_id` = @card_id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@arrangement_id", DbType.Int64, arrangementID),
				createParam("@card_id", DbType.Int64, cardID)
			};

			errorMessage += execReadField(sql, path, parameters, out arrangementCardID, "id");

			return errorMessage;
		}

		/// <summary>Gets an arrangement list card's database ID.</summary>
		/// <param name="owningArrCardID">The arrangement card ID of the owning card.</param>
		/// <param name="cardID">The database ID of the list item.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="errorMessage">Any error messages.</param>
		/// <returns>The arrangement list card's database ID.</returns>
		public static string getArrangementListCardID(string owningArrCardID, string cardID, string path, ref string errorMessage)
		{
			string sql = @"
				SELECT `ac2`.`id`
				FROM `arrangement_card` `ac`
					JOIN `arrangement_card` `ac2` ON `ac2`.`arrangement_id` = `ac`.`arrangement_id`
				WHERE `ac2`.`card_id` = @card_id
					AND `ac`.`id` = @owner_id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@owner_id", DbType.Int64, owningArrCardID),
				createParam("@card_id", DbType.Int64, cardID)
			};

			string result;
			errorMessage += execReadField(sql, path, parameters, out result, "id");

			return result;
		}

		/// <summary>Gets a list of all arrangement list card IDs within a specific arrangement card.</summary>
		/// <param name="owningArrCardID">The arrangement card ID of the owning card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="errorMessage">Any error messages.</param>
		/// <returns>A list of all arrangement list card IDs within a specific arrangement card.</returns>
		public static List<string> getArrangementListCardIDs(string owningArrCardID, string path, ref string errorMessage)
		{
			string sql = @"
				SELECT `ac2`.`id`
				FROM `arrangement_card` `ac`
					JOIN `field_list` `fl` ON `fl`.`card_id` = `ac`.`card_id`
					JOIN `arrangement_card` `ac2` ON `ac2`.`card_id` = `fl`.`value` AND `ac2`.`arrangement_id` = `ac`.`arrangement_id`
				WHERE `ac`.`id` = @owner_id
				ORDER BY `fl`.`sort_order` ASC;";

			List<string> ids;
			errorMessage += execReadField(sql, path, createParam("@owner_id", DbType.Int64, owningArrCardID), out ids, "id");

			return ids;
		}

		/// <summary>Sets a card's position and size in an arrangement.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="x">The x-coordinate of the card in the arrangement.</param>
		/// <param name="y">The y-coordinate of the card in the arrangement.</param>
		/// <param name="width">The width of the card in the arrangement.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string setCardPosAndSize(string arrangementID, string cardID, int x, int y, int width, string path)
		{
			string errorMessage = string.Empty;

			string sql = "UPDATE `arrangement_card_standalone` SET `x` = @x, `y` = @y, `width` = @width WHERE `arrangement_card_id` = (SELECT `id` FROM `arrangement_card` WHERE `arrangement_id` = @arrangement_id AND `card_id` = @card_id);";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@arrangement_id", DbType.Int64, arrangementID),
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@x", DbType.Int64, x),
				createParam("@y", DbType.Int64, y),
				createParam("@width", DbType.Int64, width)
			};

			errorMessage += execNonQuery(sql, path, parameters);

			return errorMessage;
		}

		/// <summary>Sets a text field's height increase for an arrangement.</summary>
		/// <param name="arrangementCardID">The arrangement card's database ID.</param>
		/// <param name="cardTypeFieldID">The card type field's database ID.</param>
		/// <param name="heightIncrease">The text field's height increase.</param>
		/// <param name="path">The current path of the database.</param>
		/// <returns>Any error messages.</returns>
		public static string setFieldTextHeightIncrease(string arrangementCardID, string cardTypeFieldID, int heightIncrease, string path)
		{
			string errorMessage = string.Empty;

			string sql = "UPDATE `arrangement_field_text` SET `height_increase` = @height_increase WHERE `card_type_field_id` = @card_type_field_id AND `arrangement_card_id` = @arrangement_card_id;";

			errorMessage += execNonQuery(sql, path,
				createParam("@arrangement_card_id", DbType.Int64, arrangementCardID),
				createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID),
				createParam("@height_increase", DbType.Int64, heightIncrease));

			return errorMessage;
		}

		/// <summary>Adds a card to an arrangement.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="x">The x-coordinate of the card in the arrangement.</param>
		/// <param name="y">The y-coordinate of the card in the arrangement.</param>
		/// <param name="width">The width of the card in the arrangement.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="id">The database id of the arrangement card.</param>
		/// <returns>Any error messages.</returns>
		public static string arrangementAddCard(string arrangementID, string cardID, int x, int y, int width, string path, out string id)
		{
			string errorMessage = string.Empty;

			string sql = @"
				INSERT INTO `arrangement_card` (`arrangement_id`, `card_id`)
				VALUES (@arrangement_id, @card_id);

				CREATE TEMPORARY TABLE `ac_id`(`id` INTEGER PRIMARY KEY);
				INSERT INTO `ac_id`
				VALUES (LAST_INSERT_ROWID());

				INSERT INTO `arrangement_card_standalone` (`arrangement_card_id`, `x`, `y`, `width`)
				SELECT `id`, @x, @y, @width FROM `ac_id`;

				INSERT INTO `arrangement_field_text` (`arrangement_card_id`, `card_type_field_id`)
				SELECT `ac_id`.`id`, `ctf`.`id` FROM `card_type_field` `ctf` JOIN `card_type` `ct` ON `ct`.`id` = `ctf`.`card_type_id` JOIN `card` `c` ON `c`.`card_type_id` = `ct`.`id` JOIN `ac_id` WHERE `c`.`id` = @card_id AND `ctf`.`field_type` = @text_type;

				INSERT INTO `arrangement_card` (`arrangement_id`, `card_id`)
				SELECT @arrangement_id, `value` FROM `field_list` `fl` WHERE `fl`.`card_id` = @card_id;

				INSERT INTO `arrangement_card_list` (`arrangement_card_id`)
				SELECT `ac`.`id`
				FROM `arrangement_card` `ac`
					JOIN `field_list` `fl` ON `fl`.`value` = `ac`.`card_id`
				WHERE `arrangement_id` = @arrangement_id
					AND `fl`.`card_id` = @card_id;

				SELECT `id` FROM `ac_id`;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@arrangement_id", DbType.Int64, arrangementID),
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@x", DbType.Int64, x),
				createParam("@y", DbType.Int64, y),
				createParam("@width", DbType.Int64, width),
				createParam("@text_type", DbType.Int64, (int)DataType.Text)
			};

			errorMessage += execReadField(sql, path, parameters, out id, "id");

			return errorMessage;
		}

		/// <summary>Removes a card from an arrangement.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string arrangementRemoveCard(string arrangementID, string cardID, string path)
		{
			string errorMessage = string.Empty;

			string sql = @"
				DELETE FROM `arrangement_card`
				WHERE `arrangement_id` = @arrangement_id
					AND `card_id` IN (
						SELECT @card_id

						UNION ALL

						SELECT `value`
						FROM `field_list`
						WHERE `card_id` = @card_id);";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@arrangement_id", DbType.Int64, arrangementID),
				createParam("@card_id", DbType.Int64, cardID)
			};

			errorMessage += execNonQuery(sql, path, parameters);

			return errorMessage;
		}

		/// <summary>Changes an arrangement's name.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="newName">The arrangement's new name.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string arrangementChangeName(string arrangementID, string newName, string path)
		{
			string errorMessage = string.Empty;

			string sql = "UPDATE `arrangement` SET `name` = @name WHERE `id` = @id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@id", DbType.Int64, arrangementID),
				createParam("@name", DbType.String, newName)
			};

			errorMessage += execNonQuery(sql, path, parameters);

			return errorMessage;
		}

		#endregion Arrangements

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
							if (reader.Read())
							{
								result = new string[fieldName.Length];

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
		/// <param name="result">The return value.</param>
		/// <param name="fieldName">The name of the field to return.</param>
		/// <returns>Any error messages.</returns>
		public static string execReadField(string sql, string path, SQLiteParameter parameter, out string result, string fieldName)
		{
			return execReadField(sql, path, new SQLiteParameter[] { parameter }, out result, fieldName);
		}

		/// <summary>Executes an SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return value.</param>
		/// <param name="fieldName">The name of the field to return.</param>
		/// <returns>Any error messages.</returns>
		public static string execReadField(string sql, string path, IEnumerable<SQLiteParameter> parameters, out string result, string fieldName)
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
							if (reader.Read())
							{
								result = reader[fieldName].ToString();
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

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameter">A parameter used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The name of the field to return.</param>
		public static string execReadField(string sql, string path, SQLiteParameter parameter, out List<string> result, string fieldName)
		{
			return execReadField(sql, path, (parameter == null ? null : new SQLiteParameter[] { parameter }), out result, fieldName);
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The name of the field to return.</param>
		/// <returns>Any error messages.</returns>
		public static string execReadField(string sql, string path, IEnumerable<SQLiteParameter> parameters, out List<string> result, string fieldName)
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
							result = new List<string>();

							while (reader.Read())
							{
								result.Add(reader[fieldName].ToString());
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

		/// <summary>Cleans up the database.</summary>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string vacuum(string path)
		{
			string errorMessage = string.Empty;

			string sql = "VACUUM;";

			try
			{
				using (SQLiteConnection con = new SQLiteConnection(genConnectionString(path)))
				{
					con.Open();

					using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
					{
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

		/// <summary>Finds the next name</summary>
		/// <param name="names"></param>
		/// <param name="newName"></param>
		/// <param name="newNameStart"></param>
		/// <param name="newNameEnd"></param>
		/// <param name="newNameIndex"></param>
		/// <returns></returns>
		public static string findNextName(List<string> names, string newName, string newNameStart, string newNameEnd, int newNameIndex)
		{
			int nameNum = 1;
			foreach (string r in names)
			{
				string name = r;

				int temp;
				if (name.StartsWith(newNameStart) && name.EndsWith(newNameEnd) && int.TryParse(name.Substring(newNameIndex, name.Length - newNameEnd.Length - newNameIndex), out temp) && temp >= nameNum)
					nameNum = temp + 1;
			}

			return newNameStart + nameNum.ToString() + newNameEnd;
		}

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
