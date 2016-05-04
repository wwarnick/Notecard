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
				DROP TABLE IF EXISTS `card_type_field`;
				DROP TABLE IF EXISTS `card_type`;

				CREATE TABLE IF NOT EXISTS `card_type` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`visible_name` TEXT NOT NULL,
					`table_name` TEXT NOT NULL UNIQUE,
					`inherits_card_type_id` INTEGER NULL DEFAULT NULL
						REFERENCES `card_type` (`id`)
						ON UPDATE CASCADE ON DELETE SET NULL
						DEFERRABLE INITIALLY DEFERRED
				);

				CREATE INDEX `idx_ct_inherits_card_type_id`
					ON `card_type` (`inherits_card_type_id`);

				CREATE TABLE IF NOT EXISTS `card_type_field` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_type_id` INTEGER NOT NULL
						REFERENCES `card_type`
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`field_type` INTEGER NOT NULL,
					`visible_name` TEXT NOT NULL,
					`column_name` TEXT NULL DEFAULT NULL,
					`sort_order` INTEGER NOT NULL,
					`ref_card_type_id` INTEGER NULL DEFAULT NULL
						REFERENCES `card_type` (`id`)
						ON UPDATE CASCADE ON DELETE SET NULL
						DEFERRABLE INITIALLY DEFERRED,
					`list_table_name` TEXT NULL DEFAULT NULL,
					UNIQUE (`card_type_id`, `visible_name`)
				);

				CREATE INDEX `idx_ctf_card_type_id`
					ON `card_type_field` (`card_type_id`);

				CREATE INDEX `idx_ctf_ref_card_type_id`
					ON `card_type_field` (`ref_card_type_id`);

				CREATE TABLE IF NOT EXISTS `card_list_field` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_type_field_id` INTEGER NOT NULL
						REFERENCES `card_type_field`
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`field_type` INTEGER NOT NULL,
					`visible_name` TEXT NOT NULL,
					`column_name` TEXT NOT NULL,
					`sort_order` INTEGER NOT NULL,
					`ref_card_type_id` INTEGER NULL DEFAULT NULL
						REFERENCES `card_type` (`id`)
						ON UPDATE CASCADE ON DELETE SET NULL
						DEFERRABLE INITIALLY DEFERRED,
					UNIQUE (`card_type_field_id`, `column_name`),
					UNIQUE(`card_type_field_id`, `visible_name`)
				);

				CREATE INDEX `idx_clf_card_type_field_id`
					ON `card_list_field` (`card_type_field_id`);

				CREATE INDEX `idx_clf_ref_card_type_id`
					ON `card_list_field` (`ref_card_type_id`);";

			return execNonQuery(sql, path, null);
		}

		/// <summary>Saves a new card type to the database.</summary>
		/// <param name="cardType">The card type to save.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string saveNewCardType(CardType cardType, string path)
		{
			string errorMessage = string.Empty;
			bool isChild = !string.IsNullOrEmpty(cardType.InheritsCardTypeID);

			// insert card type record
			string sql = string.Format(@"
				INSERT INTO `card_type` (`visible_name`{0}) VALUES (@visible_name{1});
				SELECT LAST_INSERT_ROWID() AS `id`",
				(isChild ? ", `inherits_card_type_id`" : ""),
				(isChild ? ", @inherits_card_type_id" : ""));

			List<SQLiteParameter> parameters = new List<SQLiteParameter>() { createParam("@visible_name", DbType.String, cardType.VisibleName) };

			if (isChild)
				parameters.Add(createParam("@inherits_card_type_id", DbType.Int32, cardType.InheritsCardTypeID));

			// execute
			string[] result;
			errorMessage += execReadField(sql.ToString(), path, parameters, out result, "id");

			cardType.ID = result[0];
			cardType.TableName = "c" + cardType.ID;

			// update card type table name
			StringBuilder bSql = new StringBuilder("UPDATE `card_type` SET `table_name` = @table_name WHERE `id` = @card_type_id;\n\n");
			parameters.Clear();
			parameters.Add(createParam("@table_name", DbType.String, cardType.TableName));
			parameters.Add(createParam("@card_type_id", DbType.Int32, cardType.ID));
			parameters.Add(createParam("@list_type_id", DbType.Int32, (int)DataType.List));

			// insert card type field records
			resetParamNames();

			for (int i = 0; i < cardType.Fields.Count; i++)
			{
				CardTypeField f = cardType.Fields[i];

				if (i > 0)
					bSql.Append(",");

				bSql.Append(@"
						(@card_type_id, " + getNextParamName("field_type") + ", ");
				parameters.Add(createParam(CurParamName, DbType.Int32, f.FieldType));

				bSql.Append(getNextParamName("visible_name") + ", ");
				parameters.Add(createParam(CurParamName, DbType.String, f.VisibleName));

				bSql.Append(getNextParamName("column_name") + ", ");
				parameters.Add(createParam(CurParamName, DbType.String, f.ColumnName));

				bSql.Append(getNextParamName("sort_order") + ", ");
				parameters.Add(createParam(CurParamName, DbType.Int32, i + 1));

				bSql.Append(getNextParamName("ref_card_type_id") + ", ");
				parameters.Add(createParam(CurParamName, DbType.Int32, f.RefCardTypeID));

				bSql.Append(getNextParamName("list_table_name") + ")");
				parameters.Add(createParam(CurParamName, DbType.String, f.ListTableName));
			}

			// complete insert query
			if (bSql.Length > 0)
			{
				bSql.Insert(0, @"
					INSERT INTO `card_type_field` (`card_type_id`, `field_type`, `visible_name`, `column_name`, `sort_order`, `ref_card_type_id`, `list_table_name`) VALUES");

				bSql.Append(@";");
			}

			// update column_name and list_table_name, and get field ids and list table names
			bSql.Append(@"

					UPDATE `card_type_field` SET `column_name` = ('c' || `id`), `list_table_name` = IF(`field_type` = @list_type_id, ('l' || `id`), NULL) WHERE `card_type_id` = @card_type_id;
					SELECT `id` FROM `card_type_field` WHERE `card_type_id` = @card_type_id ORDER BY `sort_order` ASC;");

			// execute
			List<string[]> fieldIDs;
			errorMessage += execReadField(bSql.ToString(), path, parameters, out fieldIDs, "id");

			bSql.Clear();
			parameters.Clear();
			StringBuilder listIDs = new StringBuilder();

			for (int i = 0; i < cardType.Fields.Count; i++)
			{
				// get field ids
				CardTypeField f = cardType.Fields[i];
				f.ID = fieldIDs[i][0];

				// build list field insert SQL
				if (f.FieldType == DataType.List)
				{
					listIDs.Append((listIDs.Length > 0 ? ", " : "") + f.ID); // build list of ids

					string fieldIDParam = getNextParamName("card_type_field_id");
					parameters.Add(createParam(fieldIDParam, DbType.Int32, f.ID));

					for (int j = 0; j < f.ListFields.Count; j++)
					{
						CardTypeField l = f.ListFields[j];

						if (j > 0)
							bSql.Append(",");

						bSql.Append(@"
						(" + fieldIDParam + ", " + getNextParamName("field_type"));
						parameters.Add(createParam(CurParamName, DbType.Int32, l.FieldType));

						bSql.Append(getNextParamName("visible_name") + ", ");
						parameters.Add(createParam(CurParamName, DbType.String, l.VisibleName));

						bSql.Append(getNextParamName("sort_order") + ", ");
						parameters.Add(createParam(CurParamName, DbType.Int32, j + 1));

						bSql.Append(getNextParamName("ref_card_type_id") + ")");
						parameters.Add(createParam(CurParamName, DbType.Int32, l.RefCardTypeID));
					}
				}
			}

			// insert list field records
			if (bSql.Length > 0)
			{
				bSql.Append(@"
					INSERT INTO `card_list_field` (`card_type_field_id`, `field_type`, `visible_name`, `sort_order`, `ref_card_type_id`) VALUES");

				bSql.Append(@";

					UPDATE `card_list_field` SET `column_name` = ('c' || `id`) WHERE `card_type_field` IN (" + listIDs.ToString() + ");");

				errorMessage += execNonQuery(bSql.ToString(), path, parameters);
			}

			// load completed and saved card type from database
			errorMessage = getCardType(cardType.ID, path, out cardType);

			bSql.Clear();

			// get card type table create SQL
			bSql.Append(getCardTableCreateSql(cardType, path));

			// get list field table create SQL
			foreach (CardTypeField f in cardType.Fields)
			{
				if (f.FieldType == DataType.List)
					bSql.Append("\n\n" + getListTableCreateSql(cardType, f, path));
			}

			errorMessage += execNonQuery(bSql.ToString(), path, null);

			return errorMessage;
		}

		public static string saveCardType(CardType cardType, string path, IEnumerable<CardTypeChg> changes)
		{
			string errorMessage = string.Empty;

			errorMessage += getCardType(cardType.ID, path, out cardType);

			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			StringBuilder typeUpdate = new StringBuilder();

			foreach (CardTypeChg chg in changes)
			{
				switch (chg.ChgType)
				{
					case CardTypeChange.DeleteCardType:

						break;
					case CardTypeChange.NameChange:
						typeUpdate.Append((typeUpdate.Length > 0 ? ", " : "") + "`visible_name` = @type_visible_name");
						parameters.Add(createParam("@type_visible_name", DbType.String, (string)chg.Parameters));
						break;
					case CardTypeChange.InheritChange:
						typeUpdate.Append((typeUpdate.Length > 0 ? ", " : "") + "`inherits_card_type_id` = @type_inherits_card_type_id");
						parameters.Add(createParam("@type_inherits_card_type_id", DbType.Int32, (string)chg.Parameters));

						// TODO: apply change to existing data
						break;
					case CardTypeChange.FieldOrderChange:

						break;
					case CardTypeChange.FieldRemove:

						break;
					case CardTypeChange.FieldAdd:

						break;
					case CardTypeChange.FieldNameChange:

						break;
					case CardTypeChange.FieldTypeChange:

						break;
					case CardTypeChange.ListFieldOrderChange:

						break;
					case CardTypeChange.ListFieldRemove:

						break;
					case CardTypeChange.ListFieldNameChange:

						break;
					case CardTypeChange.ListFieldTypeChange:

						break;
					default:
						errorMessage += "Unknown changed type: " + chg.ToString();
						break;
				}
			}

			return errorMessage;
		}

		/// <summary>Builds a SQL script to create the card type's table.</summary>
		/// <param name="type">The card type to build a table for.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string getCardTableCreateSql(CardType type, string path)
		{
			string errorMessage = string.Empty;

			StringBuilder sql = new StringBuilder();
			StringBuilder indexes = new StringBuilder();

			sql.Append(@"
				CREATE TABLE `" + type.TableName + @"` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT");

			// add parent table foreign key
			if (!string.IsNullOrEmpty(type.InheritsCardTypeID))
			{
				CardType parent = null;

				errorMessage += getCardType(type.InheritsCardTypeID, path, out parent);

				sql.Append(@",
					`parent_id` INTEGER NOT NULL
						REFERENCES `" + parent.TableName + @"` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED");

				indexes.Append(@"
				CREATE INDEX `idx_" + type.TableName + @"_parent_id`
					ON `" + type.TableName + @"` (`parent_id`);");
			}

			// add fields
			foreach (CardTypeField f in type.Fields)
			{
				sql.Append(@",
					`" + f.ColumnName + "` ");

				switch (f.FieldType)
				{
					case DataType.Card: // card ref type
						CardType refType = null;
						errorMessage += getCardType(f.RefCardTypeID, path, out refType);
						sql.Append(
							@"INTEGER NULL DEFAULT NULL
								REFERENCES `" + refType.TableName + @"` (`id`)
								ON UPDATE CASCADE ON DELETE SET NULL
								DEFERRABLE INITIALLY DEFERRED");

						indexes.Append(@"

				CREATE INDEX (`idx_" + type.TableName + "_" + f.ColumnName + @"`
					ON `" + type.TableName + "` (`" + f.ColumnName + "`);");
						break;
					case DataType.List:
						// the list is contained in its own table
						break;
					case DataType.Text:
						sql.Append("TEXT NULL DEFAULT NULL");
						break;
					default:
						errorMessage += "Unknown field type: " + f.ToString();
						break;
				}
			}

			// finish table
			sql.Append(@"
			);");

			// add indexes
			sql.Append(indexes.ToString());

			return errorMessage;
		}

		/// <summary>Builds a SQL script to create the list field's table.</summary>
		/// <param name="type">The card type that contains the list.</param>
		/// <param name="field">The list field.</param>
		/// <param name="path">The path of the current database.</param>
		/// <returns>Any error messages.</returns>
		public static string getListTableCreateSql(CardType type, CardTypeField field, string path)
		{
			string errorMessage = string.Empty;

			StringBuilder sql = new StringBuilder();
			StringBuilder indexes = new StringBuilder();

			sql.Append(@"
				CREATE TABLE `" + field.ListTableName + @"` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT
					`owner_id` INTEGER NOT NULL
						REFERENCES `" + type.TableName + @"` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED");

			indexes.Append(@"
				CREATE INDEX `idx_" + field.ListTableName + @"_owner_id`
					ON `" + field.ListTableName + @"` (`owner_id`);");

			// add fields
			foreach (CardTypeField f in field.ListFields)
			{
				sql.Append(@"
					`" + f.ColumnName + "` ");

				switch (f.FieldType)
				{
					case DataType.Card: // card ref type
						CardType refType = null;
						errorMessage += getCardType(f.RefCardTypeID, path, out refType);
						sql.Append(
							@"INTEGER NULL DEFAULT NULL
								REFERENCES `" + refType.TableName + @"` (`id`)
								ON UPDATE CASCADE ON DELETE SET NULL
								DEFERRABLE INITIALLY DEFERRED");

						indexes.Append(@"

				CREATE INDEX (`idx_" + field.ListTableName + "_" + f.ColumnName + @"
					ON `" + field.ListTableName + "` (`" + f.ColumnName + "`);");
						break;
					case DataType.List:
						errorMessage += "List types aren't allowed within lists.";
						break;
					case DataType.Text:
						sql.Append("TEXT NULL DEFAULT NULL");
						break;
					default:
						errorMessage += "Unknown field type: " + f.ToString();
						break;
				}
			}

			// finish table
			sql.Append(@"
			);");

			// add indexes
			sql.Append(indexes.ToString());

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
			string[] cardResults = null;
			List<string[]> fieldResults = null;

			// build card type
			string sql = "SELECT * FROM `card_type` WHERE `id` = @id;";
			SQLiteParameter[] sqlParam = new SQLiteParameter[] { createParam("@id", DbType.Int32, id) };

			errorMessage += execReadField(sql, path, sqlParam, out cardResults, "visible_name", "table_name", "inherits_card_type_id");

			if (string.IsNullOrEmpty(errorMessage))
			{
				cardType = new CardType(id, cardResults[0], cardResults[1], cardResults[2]);

				// retrieve field data
				sql = "SELECT * FROM `card_type_field` WHERE `card_type_id` = @id ORDER BY `sort_order` ASC;";

				errorMessage += execReadField(sql, path, sqlParam, out fieldResults, "id", "field_type", "visible_name", "column_name", "sort_order", "ref_card_type_id", "list_table_name");
			}

			// build fields
			if (string.IsNullOrEmpty(errorMessage) && fieldResults.Count > 0)
			{
				foreach (string[] f in fieldResults)
				{
					CardTypeField field = new CardTypeField(f[0], (DataType)int.Parse(f[1]), f[2], f[3], f[4], f[5], f[6]);

					// build list fields, if applicable
					if (field.FieldType == DataType.List)
					{
						sql = "SELECT * FROM `card_type_list_field` WHERE `card_type_field_id` = @id ORDER BY `sort_order`;";
						sqlParam[0] = createParam("@id", DbType.Int32, field.ID);
						List<string[]> listResults;
						errorMessage += execReadField(sql, path, sqlParam, out listResults, "id", "field_type", "visible_name", "column_name", "sort_order", "ref_card_type_id");

						if (string.IsNullOrEmpty(errorMessage))
						{
							foreach (string[] l in listResults)
							{
								field.ListFields.Add(new CardTypeField(l[0], (DataType)int.Parse(l[1]), l[2], l[3], l[4], l[5]));
							}
						}
					}

					cardType.Fields.Add(field);
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
		public static string execNonQuery(string sql, string path, IEnumerable<SQLiteParameter> parameters)
		{
			string errorMessage = string.Empty;

			using (SQLiteConnection con = new SQLiteConnection(genConnectionString(path)))
			{
				try
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
				catch (Exception ex)
				{
					errorMessage += ex.Message;
				}
				finally
				{
					if (con.State == ConnectionState.Open)
						con.Close();
				}
			}

			return errorMessage;
		}

		/// <summary>Executes an SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
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

		/// <summary>Executes an SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
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
