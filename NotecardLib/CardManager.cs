using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotecardLib
{
	public class CardManager
	{
		#region Members

		/// <summary>The path of the current database.</summary>
		public const string DBPath = @"current\newcardfile.sqlite";

		/// <summary>The current file path.</summary>
		private static string currentFilePath;

		/// <summary>The current file path.</summary>
		public static string CurrentFilePath
		{
			get { return currentFilePath; }
			private set
			{
				currentFilePath = value;
				FileName = string.IsNullOrEmpty(currentFilePath)
					? string.Empty
					: currentFilePath.Substring(currentFilePath.LastIndexOf(@"\") + 1);
			}
		}

		/// <summary>The name of the current file.</summary>
		public static string FileName { get; private set; }

		/// <summary>The 'last modified' date of the current file when it was first opened or last saved.</summary>
		public static DateTime CurrentFileOldLastModified { get; private set; }

		/// <summary>The card types.</summary>
		public static CardType[] CardTypes { get; private set; }

		/// <summary>The card types, organized by database ID.</summary>
		public static Dictionary<string, CardType> CardTypeByID { get; private set; }

		/// <summary>The current parameter number.  Used for creating parameter names.</summary>
		private static int paramNum;

		/// <summary>The current parameter name.</summary>
		private static string paramCurName;

		/// <summary>The current parameter name.</summary>
		public static string CurParamName
		{
			get { return paramCurName; }
		}

		/// <summary>Used for any asynchronous threads.</summary>
		private static Thread thread;

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
			// initialize lists
			CardTypes = null;
			CardTypeByID = new Dictionary<string, CardType>();

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

		#region File

		/// <summary>Gets the current version of NoteCard.</summary>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The current version of NoteCard.</returns>
		public static string getNoteCardVersion(ref string userMessage)
		{
			string sql = "SELECT `version` FROM `versions` ORDER BY `version` DESC LIMIT 1;";
			return execReadField(sql, "settings.sqlite", ref userMessage, (IEnumerable<SQLiteParameter>)null, "version");
		}

		/// <summary>Gets the update scripts to update the current file.</summary>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The update scripts to update the current file.</returns>
		public static string getUpdateScripts(ref string userMessage)
		{
			// get current file version
			string version = getFileVersion(ref userMessage);

			// get update scripts
			string sql = "SELECT `update_sql` FROM `versions` WHERE `version` > @version;";
			List<string> scripts = execReadListField(sql, "settings.sqlite", ref userMessage, createParam("@version", DbType.String, version), "update_sql");

			// compile scripts
			StringBuilder finalScript = new StringBuilder();
			foreach (string script in scripts)
			{
				finalScript.Append(script);
			}

			return finalScript.ToString();
		}

		/// <summary>Initializes a new card database.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void createNewFile(ref string userMessage)
		{
			CurrentFilePath = null;
			clearCurrentDir(ref userMessage);

			try
			{
				SQLiteConnection.CreateFile(DBPath);
			}
			catch (Exception ex)
			{
				userMessage += "Could not create file at \"" + DBPath + "\": " + ex.Message + "\n\n";
				return;
			}

			string sql = @"
				CREATE TABLE `global_settings` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`version` TEXT NOT NULL
				);

				INSERT INTO `global_settings` (`version`) VALUES (@version);

				CREATE TABLE `card_type` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`name` TEXT NULL DEFAULT NULL,
					`context` INTEGER NOT NULL,
					`color` INTEGER NOT NULL DEFAULT 32768,
					UNIQUE (`name`)
				);

				CREATE TABLE `card_type_field` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_type_id` INTEGER NOT NULL
						REFERENCES `card_type`
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`name` TEXT NOT NULL,
					`field_type` INTEGER NOT NULL,
					`sort_order` INTEGER NOT NULL,
					`show_label` INTEGER NOT NULL DEFAULT 1,
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

				CREATE TABLE `field_image` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_id` INTEGER NOT NULL
						REFERENCES `card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`card_type_field_id` INTEGER NOT NULL
						REFERENCES `card_type_field` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					UNIQUE (`card_id`, `card_type_field_id`)
				);

				CREATE INDEX `idx_fi_card_id`
					ON `field_image` (`card_id`);

				CREATE INDEX `idx_fi_card_type_field_id`
					ON `field_image` (`card_type_field_id`);

				CREATE TABLE `field_checkbox` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`card_id` INTEGER NOT NULL
						REFERENCES `card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`card_type_field_id` INTEGER NOT NULL
						REFERENCES `card_type_field` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`value` INTEGER NOT NULL DEFAULT 0,
					UNIQUE (`card_id`, `card_type_field_id`)
				);

				CREATE INDEX `idx_fch_card_id`
					ON `field_checkbox` (`card_id`);

				CREATE INDEX `idx_fch_card_type_field_id`
					ON `field_checkbox` (`card_type_field_id`);

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
						DEFERRABLE INITIALLY DEFERRED,
					`minimized` INTEGER NOT NULL DEFAULT 1
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
					ON `arrangement_field_text` (`card_type_field_id`);

				CREATE TABLE `arrangement_field_list` (
					`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
					`arrangement_card_id` INTEGER NOT NULL
						REFERENCES `arrangement_card` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`card_type_field_id` INTEGER NOT NULL
						REFERENCES `card_type_field` (`id`)
						ON UPDATE CASCADE ON DELETE CASCADE
						DEFERRABLE INITIALLY DEFERRED,
					`minimized` INTEGER NOT NULL DEFAULT 1,
					UNIQUE (`arrangement_card_id`, `card_type_field_id`)
				);

				CREATE INDEX `idx_afl_arrangement_card_id`
					ON `arrangement_field_list` (`arrangement_card_id`);

				CREATE INDEX `idx_afl_card_type_field_id`
					ON `arrangement_field_list` (`card_type_field_id`);";

			execNonQuery(sql, DBPath, ref userMessage, createParam("@version", DbType.String, getNoteCardVersion(ref userMessage)));

			refreshOldLastModifiedDate(ref userMessage);
		}

		/// <summary>Update the version of the database.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void updateDbVersion(ref string userMessage)
		{
			// check to see if the file has a version number
			string sql = "SELECT `name` FROM `sqlite_master` WHERE `type` = 'table' AND `name` = 'global_settings' LIMIT 1;";

			List<string> result = execReadListField(sql, DBPath, ref userMessage, (IEnumerable<SQLiteParameter>)null, "name");

			// if the file doesn't have a version number yet, add it
			if (result.Count == 0)
			{
				sql = @"
					CREATE TABLE `global_settings` (
						`id` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
						`version` TEXT NOT NULL
					);

					INSERT INTO `global_settings` (`version`) VALUES ('0.1.0.0');";

				execNonQuery(sql, DBPath, ref userMessage);
			}

			// get file update scripts
			sql = getUpdateScripts(ref userMessage);

			// if out of date, run scripts
			if (!string.IsNullOrEmpty(sql))
			{
				execNonQueryWithoutBeginCommit(sql, DBPath, ref userMessage);

				// update version number in file
				sql = "UPDATE `global_settings` SET `version` = @version;";
				execNonQuery(sql, DBPath, ref userMessage, createParam("@version", DbType.String, getNoteCardVersion(ref userMessage)));
			}
		}

		/// <summary>Refreshes CurrentFileOldLastModified.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void refreshOldLastModifiedDate(ref string userMessage)
		{
			try
			{
				CurrentFileOldLastModified = File.GetLastWriteTimeUtc(DBPath);
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Determines whether or not there are unsaved changes.</summary>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>Whether or not there are unsaved changes.</returns>
		public static bool hasUnsavedChanges(ref string userMessage)
		{
			try
			{
				return File.GetLastWriteTimeUtc(DBPath) != CurrentFileOldLastModified;
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}

			return false;
		}

		/// <summary>Create the current directory.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void createCurrentDir(ref string userMessage)
		{
			try
			{
				var sec = new System.Security.AccessControl.DirectorySecurity();//Directory.GetAccessControl(path);
				var everyone = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);
				sec.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(everyone, System.Security.AccessControl.FileSystemRights.Modify | System.Security.AccessControl.FileSystemRights.Synchronize, System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit, System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));

				Directory.CreateDirectory("current", sec);
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Clears the current working directory.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void clearCurrentDir(ref string userMessage)
		{
			// make sure it exists
			createCurrentDir(ref userMessage);

			try
			{
				// delete all files in it if it already exists
				DirectoryInfo di = new DirectoryInfo("current");

				foreach (FileInfo file in di.GetFiles())
				{
					file.Delete();
				}

				foreach (DirectoryInfo dir in di.GetDirectories())
				{
					dir.Delete(true);
				}
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Removes orphaned files from the current directory.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void cleanOrphanedFiles(ref string userMessage)
		{
			List<string> imageIDs = CardManager.getImageIDs(ref userMessage);

			// make sure it exists
			createCurrentDir(ref userMessage);

			try
			{
				// delete all orphaned files in it if it already exists
				DirectoryInfo di = new DirectoryInfo("current");

				foreach (FileInfo file in di.GetFiles())
				{
					if (!file.Name.EndsWith(".sqlite") && !imageIDs.Contains(file.Name))
						file.Delete();
				}
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Gets the version of the current file.</summary>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The version of the current file.</returns>
		public static string getFileVersion(ref string userMessage)
		{
			string sql = "SELECT `version` FROM `global_settings` LIMIT 1;";
			return execReadField(sql, DBPath, ref userMessage, (IEnumerable<SQLiteParameter>)null, "version");
		}

		/// <summary>Saves the file to CurrentFilePath.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void save(string savePath, ref string userMessage)
		{
			vacuum(ref userMessage);
			cleanOrphanedFiles(ref userMessage);

			try
			{
				// delete the file path if it already exists
				if (File.Exists(savePath))
					File.Delete(savePath);

				CurrentFilePath = savePath;

				ZipFile.CreateFromDirectory("current", CurrentFilePath);
				refreshOldLastModifiedDate(ref userMessage);
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Opens an existing file.</summary>
		/// <param name="openPath">The path of the file to open.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void open(string openPath, ref string userMessage)
		{
			CurrentFilePath = openPath;
			clearCurrentDir(ref userMessage);

			try
			{
				ZipFile.ExtractToDirectory(CurrentFilePath, "current");
				File.SetLastWriteTimeUtc(DBPath, File.GetLastWriteTimeUtc(CurrentFilePath));
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}

			refreshOldLastModifiedDate(ref userMessage);
			updateDbVersion(ref userMessage);
		}

		#endregion File

		#region Card Types

		/// <summary>Makes sure all card types have title fields.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void giveAllTypesTitleFields(ref string userMessage)
		{
			string sql = @"
				SELECT `ct`.`id`
				FROM `card_type` `ct`
					LEFT JOIN `card_type_field` `ctf` ON `ctf`.`card_type_id` = `ct`.`id` AND `ctf`.`field_type` = @text_type
					LEFT JOIN `card_type_field` `ctf2` ON `ctf2`.`card_type_id` = `ctf`.`card_type_id` AND `ctf2`.`sort_order` < `ctf`.`sort_order`
				WHERE `ctf`.`id` IS NULL
					AND `ctf2`.`id` IS NULL;";

			List<string> ids = execReadListField(sql, DBPath, ref userMessage, createParam("@text_type", DbType.Int64, (int)DataType.Text), "id");

			if (ids.Count > 0)
			{
				// add title fields
				foreach (string id in ids)
				{
					addCardTypeField(id, null, true, ref userMessage);
				}

				// add title text
				fillBlankCardTitles(ref userMessage);
			}
		}

		/// <summary>Refreshes the card type information.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void refreshCardTypes(ref string userMessage)
		{
			CardTypeByID.Clear();

			string sql = "SELECT `id` FROM `card_type` WHERE `context` = " + (int)CardTypeContext.Standalone + " ORDER BY UPPER(`name`);";
			List<string> ids = execReadListField(sql, DBPath, ref userMessage, (IEnumerable<SQLiteParameter>)null, "id");

			CardTypes = new CardType[ids.Count];

			// get card types
			for (int i = 0; i < ids.Count; i++)
			{
				CardType ct = getCardType(ids[i], ref userMessage);

				CardTypeByID.Add(ct.ID, ct);
				CardTypes[i] = ct;
			}
		}

		/// <summary>Creates a new card type.</summary>
		/// <param name="context">The context of the card type, whether standalone or a list.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The database ID of the card type.</returns>
		public static string newCardType(CardTypeContext context, ref string userMessage)
		{
			// get new name
			string nameSql = "SELECT `name` FROM `card_type` WHERE `name` LIKE @name;";

			List<string> names = execReadListField(nameSql, DBPath, ref userMessage, createParam("@name", DbType.String, NewCardTypeNameLike), "name");

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

			string id = execReadField(sql, DBPath, ref userMessage, parameters, "id");

			// add title field
			saveCardType(id, new CardTypeChg(CardTypeChange.CardTypeFieldAdd, "Name"), ref userMessage);

			return id;
		}

		/// <summary>Saves changes to an existing card type.</summary>
		/// <param name="cardTypeID">The database ID of the card type to update.</param>
		/// <param name="changes">The changes to make.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void saveCardType(string cardTypeID, CardTypeChg change, ref string userMessage)
		{
			CardType cardType = getCardType(cardTypeID, ref userMessage);
			string sql = null;

			switch (change.ChgType)
			{
				case CardTypeChange.CardTypeNameChange:
					sql = "UPDATE `card_type` SET `name` = @name WHERE `id` = @id;";
					execNonQuery(sql, DBPath, ref userMessage, createParam("@name", DbType.String, (string)change.Parameters[0]), createParam("@id", DbType.Int64, cardType.ID));
					break;
				case CardTypeChange.CardTypeColorChange:
					changeCardTypeColor(cardTypeID, (int)change.Parameters[0], ref userMessage);
					break;
				case CardTypeChange.CardTypeRemove:
					removeCardType(cardType, ref userMessage);
					break;
				case CardTypeChange.CardTypeFieldAdd:
					addCardTypeField(cardType.ID, ((change.Parameters == null || change.Parameters.Length == 0) ? null : (string)change.Parameters[0]), ((change.Parameters == null || change.Parameters.Length < 2) ? false : (bool)change.Parameters[1]), ref userMessage);
					break;
				case CardTypeChange.CardTypeFieldNameChange:
					sql = "UPDATE `card_type_field` SET `name` = @name WHERE `id` = @id;";
					execNonQuery(sql, DBPath, ref userMessage, createParam("@name", DbType.String, (string)change.Parameters[1]), createParam("@id", DbType.Int64, (string)change.Parameters[0]));
					break;
				case CardTypeChange.CardTypeFieldTypeChange:
					changeCardTypeFieldType((string)change.Parameters[0], (DataType)change.Parameters[1], ref userMessage);
					break;
				case CardTypeChange.CardTypeFieldCardTypeChange:
					changeCardTypeFieldCardType((string)change.Parameters[0], (string)change.Parameters[1], ref userMessage);
					break;
				case CardTypeChange.CardTypeFieldShowLabelChange:
					sql = "UPDATE `card_type_field` SET `show_label` = @show_label WHERE `id` = @id;";
					execNonQuery(sql, DBPath, ref userMessage, createParam("@show_label", DbType.Int64, ((bool)change.Parameters[1] ? "1" : "0")), createParam("@id", DbType.Int64, (string)change.Parameters[0]));
					break;
				case CardTypeChange.CardTypeFieldSwap:
					swapCardTypeFields((string)change.Parameters[0], (string)change.Parameters[1], ref userMessage);
					break;
				case CardTypeChange.CardTypeFieldRemove:
					removeCardTypeField((string)change.Parameters[0], ref userMessage);
					break;
				default:
					userMessage += "Unknown change type: " + change.ChgType.ToString();
					break;
			}
		}

		/// <summary>Changes a card type's color.</summary>
		/// <param name="cardTypeID">The database ID of the card type to change.</param>
		/// <param name="color">The new color.</param>
		/// <param name="userMessage">Any user messages.</param>
		private static void changeCardTypeColor(string cardTypeID, int color, ref string userMessage)
		{
			string sql = "UPDATE `card_type` SET `color` = @color WHERE `id` = @id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@id", DbType.Int64, cardTypeID),
				createParam("@color", DbType.Int64, color)
			};

			execNonQuery(sql, DBPath, ref userMessage, parameters);
		}

		/// <summary>Removes a card type from the database.</summary>
		/// <param name="cardType">The card type to remove.</param>
		/// <param name="userMessage">Any user messages.</param>
		private static void removeCardType(CardType cardType, ref string userMessage)
		{
			StringBuilder sql = new StringBuilder();
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_type_id", DbType.Int64, cardType.ID));

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

			execNonQuery(sql.ToString(), DBPath, ref userMessage, parameters);
		}

		/// <summary>Adds a card type field to the database.</summary>
		/// <param name="cardTypeID">The database ID of the owning card type.</param>
		/// <param name="name">The name of the field.</param>
		/// <param name="moveToFront">Whether or not to put the new field at the front of the card type (make it the title).</param>
		/// <param name="userMessage">Any user messages.</param>
		private static void addCardTypeField(string cardTypeID, string name, bool moveToFront, ref string userMessage)
		{
			// get new name
			if (string.IsNullOrEmpty(name))
			{
				string nameSql = "SELECT `name` FROM `card_type_field` WHERE `card_type_id` = @card_type_id AND `name` LIKE @name;";

				SQLiteParameter[] nameParams = new SQLiteParameter[]
				{
					createParam("@card_type_id", DbType.Int64, cardTypeID),
					createParam("@name", DbType.String, NewCardTypeFieldNameLike)
				};

				List<string> names = execReadListField(nameSql, DBPath, ref userMessage, nameParams, "name");

				name = findNextName(names, NewCardTypeFieldName, NewCardTypeFieldNameStart, NewCardTypeFieldNameEnd, NewCardTypeFieldNameIndex);
			}

			// insert card type field and card records
			StringBuilder sql = new StringBuilder();

			if (moveToFront)
			{
				sql.Append(@"
				UPDATE `card_type_field` SET `sort_order` = `sort_order` + 1 WHERE `card_type_id` = @card_type_id;

				INSERT INTO `card_type_field` (`card_type_id`, `name`, `field_type`, `sort_order`)
				VALUES (@card_type_id, @name, @field_type, 1);");
			}
			else
			{
				sql.Append(@"
				INSERT INTO `card_type_field` (`card_type_id`, `name`, `field_type`, `sort_order`)
				VALUES (@card_type_id, @name, @field_type, (SELECT COALESCE(MAX(`sort_order`), 0) + 1 FROM `card_type_field` WHERE `card_type_id` = @card_type_id));");
			}

			sql.Append(@"

				CREATE TEMPORARY TABLE `ctf_id`(`id` INTEGER PRIMARY KEY);
				INSERT INTO `ctf_id` VALUES (LAST_INSERT_ROWID());

				INSERT INTO `field_text` (`card_id`, `card_type_field_id`, `value`)
				SELECT `c`.`id`, `ctf_id`.`id`, '' FROM `card` `c` JOIN `ctf_id` WHERE `c`.`card_type_id` = " + cardTypeID + @";

				INSERT INTO `arrangement_field_text` (`arrangement_card_id`, `card_type_field_id`)
				SELECT `ac`.`id`, `ctf_id`.`id`
				FROM `arrangement_card` `ac`
					JOIN `card` `c` ON `c`.`id` = `ac`.`card_id`
					JOIN `ctf_id`
				WHERE `c`.`card_type_id` = " + cardTypeID + ";");

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@card_type_id", DbType.Int64, cardTypeID),
				createParam("@name", DbType.String, name),
				createParam("@field_type", DbType.Int64, (int)DataType.Text)
			};

			// execute sql
			execNonQuery(sql.ToString(), DBPath, ref userMessage, parameters);
		}

		/// <summary>Changes a card type field's field type.</summary>
		/// <param name="fieldID">The database ID of the card type field.</param>
		/// <param name="newType">The type to change it to.</param>
		/// <param name="userMessage">Any user messages.</param>
		private static void changeCardTypeFieldType(string fieldID, DataType newType, ref string userMessage)
		{
			CardTypeField oldField;
			string cardTypeID;
			oldField = getCardTypeField(fieldID, ref userMessage, out cardTypeID);

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
						DELETE FROM `field_list` WHERE `card_type_field_id` = @card_type_field_id;
						DELETE FROM `arrangement_field_list` WHERE `card_type_field_id` = @card_type_field_id;");
					parameters.Add(createParam("@list_type_id", DbType.Int64, oldField.RefCardTypeID));
					break;
				case DataType.Image:
					sql.Append(@"
						DELETE FROM `field_image` WHERE `card_type_field_id` = @card_type_field_id;");
					break;
				case DataType.CheckBox:
					sql.Append(@"
						DELETE FROM `field_checkbox` WHERE `card_type_field_id` = card_type_field_id;");
					break;
				default:
					userMessage += "Unkown field type: " + oldField.FieldType.ToString();
					break;
			}

			// insert new fields
			switch (newType)
			{
				case DataType.Text:
					sql.Append(@"
						INSERT INTO `field_text` (`card_id`, `card_type_field_id`, `value`)
						SELECT `id`, @card_type_field_id, '' FROM `card` WHERE `card_type_id` = " + cardTypeID + @";

						INSERT INTO `arrangement_field_text` (`arrangement_card_id`, `card_type_field_id`)
						SELECT `ac`.`id`, @card_type_field_id FROM `arrangement_card` `ac` JOIN `card` `c` ON `c`.`id` = `ac`.`card_id` WHERE `c`.`card_type_id` = " + cardTypeID + @";");
					break;
				case DataType.Card:
					sql.Append(@"
						INSERT INTO `field_card` (`card_id`, `card_type_field_id`, `value`)
						SELECT `id`, @card_type_field_id, NULL FROM `card` WHERE `card_type_id` = " + cardTypeID + ";");
					break;
				case DataType.List:
					sql.Append(@"
						UPDATE `card_type_field` SET `show_label` = 1 WHERE `id` = @card_type_field_id;

						INSERT INTO `card_type` (`context`) VALUES (@list_context);
						UPDATE `card_type_field` SET `ref_card_type_id` = LAST_INSERT_ROWID() WHERE `id` = @card_type_field_id;

						INSERT INTO `arrangement_field_list` (`arrangement_card_id`, `card_type_field_id`)
						SELECT `ac`.`id`, @card_type_field_id FROM `arrangement_card` `ac` JOIN `card` `c` ON `c`.`id` = `ac`.`card_id` WHERE `c`.`card_type_id` = " + cardTypeID + @";");
					parameters.Add(createParam("@list_context", DbType.Int64, (int)CardTypeContext.List));
					break;
				case DataType.Image:
					// do nothing
					break;
				case DataType.CheckBox:
					sql.Append(@"
						INSERT INTO `field_checkbox` (`card_id`, `card_type_field_id`, `value`)
						SELECT `id`, @card_type_field_id, 0 FROM `card` WHERE `card_type_id` = " + cardTypeID + ";");
					break;
				default:
					userMessage += "Unknown field type: " + newType.ToString();
					break;
			}

			// execute sql
			execNonQuery(sql.ToString(), DBPath, ref userMessage, parameters);

			// if it's a list, add the first field
			if (newType == DataType.List)
			{
				string tempSql = "SELECT `ref_card_type_id` FROM `card_type_field` WHERE `id` = @card_type_field_id;";
				string id = execReadField(tempSql, DBPath, ref userMessage, createParam("@card_type_field_id", DbType.Int64, fieldID), "ref_card_type_id");
				saveCardType(id, new CardTypeChg(CardTypeChange.CardTypeFieldAdd, "Field 1"), ref userMessage);
			}
		}

		/// <summary>Changes a field's referred card type ID (for Card type fields).</summary>
		/// <param name="fieldID">The database ID of the field.</param>
		/// <param name="newTypeID">The database ID of the card type to refer to.</param>
		/// <param name="userMessage">Any user messages.</param>
		private static void changeCardTypeFieldCardType(string fieldID, string newTypeID, ref string userMessage)
		{
			CardTypeField field;
			field = getCardTypeField(fieldID, ref userMessage);

			if (field.FieldType != DataType.Card)
			{
				userMessage += "You Cannot change ref_card_type_id on a non-card type field.";
				return;
			}

			StringBuilder sql = new StringBuilder();
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();

			CardType newType = null;
			if (!string.IsNullOrEmpty(newTypeID))
				newType = getCardType(newTypeID, ref userMessage);

			// clear out invalidated values
			if (newType != null)
			{
				sql.Append(@"
					UPDATE `field_card` SET `value` = NULL
					WHERE `card_type_field_id` = @card_type_field_id
						AND `value` IS NOT NULL
						AND `value` NOT IN (SELECT `id` FROM `card` WHERE `card_type_id` = " + newType.ID + ");");

				parameters.Add(createParam("@ref_card_type_id", DbType.Int64, newTypeID));
			}
			else
			{
				parameters.Add(createParam("@ref_card_type_id", DbType.Int64, DBNull.Value));
			}

			sql.Append(@"
				UPDATE `card_type_field` SET `ref_card_type_id` = @ref_card_type_id WHERE `id` = @card_type_field_id;");

			parameters.Add(createParam("@card_type_field_id", DbType.Int64, fieldID));
			execNonQuery(sql.ToString(), DBPath, ref userMessage, parameters);
		}

		/// <summary>Swaps the sort order of two card type fields (the fields must be adjacent, and field 1 must be before field 2.</summary>
		/// <param name="field1ID">The database ID of the first field.</param>
		/// <param name="field2ID">The database ID of the second field.</param>
		/// <param name="userMessage">Any user messages.</param>
		private static void swapCardTypeFields(string field1ID, string field2ID, ref string userMessage)
		{
			string sql = @"
				UPDATE `card_type_field`
				SET `sort_order` = (SELECT `sort_order` FROM `card_type_field` WHERE `id` = @id1)
				WHERE `id` = @id2;

				UPDATE `card_type_field`
				SET `sort_order` = `sort_order` + 1
				WHERE `id` = @id1;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
				{
					createParam("@id1", DbType.Int64, field1ID),
					createParam("@id2", DbType.Int64, field2ID)
				};

			execNonQuery(sql, DBPath, ref userMessage, parameters);
		}

		/// <summary>Removes a card type field from the database.</summary>
		/// <param name="fieldID">The database ID of the card type field.</param>
		/// <param name="userMessage">Any user messages.</param>
		private static void removeCardTypeField(string fieldID, ref string userMessage)
		{
			// delete card_type_field record
			StringBuilder sql = new StringBuilder(@"
				DELETE FROM `card_type_field` WHERE `id` = @card_type_field_id;");
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_type_field_id", DbType.Int64, fieldID));

			// remove list type if list
			CardTypeField field;
			field = getCardTypeField(fieldID, ref userMessage);
			if (field.FieldType == DataType.List)
			{
				sql.Append(@"
				DELETE FROM `card_type` WHERE `id` = @list_type_id;");

				parameters.Add(createParam("@list_type_id", DbType.Int64, field.RefCardTypeID));
			}

			execNonQuery(sql.ToString(), DBPath, ref userMessage, parameters);
		}

		/// <summary>Retrieves a card type.</summary>
		/// <param name="id">The database id of the card type.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The requested card type.</returns>
		public static CardType getCardType(string id, ref string userMessage)
		{
			return getCardType(id, ref userMessage, false);
		}

		/// <summary>Retrieves a card type.</summary>
		/// <param name="id">The database id of the card type or one of its fields.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="fromFieldID">Whether or not the id supplied is the database ID of one of the card type's fields.</param>
		/// <returns>The requested card type.</returns>
		private static CardType getCardType(string id, ref string userMessage, bool fromFieldID)
		{
			CardType cardType = null;

			// get card type
			string sql = fromFieldID
				? "SELECT `ct`.* FROM `card_type` `ct` JOIN `card_type_field` `ctf` ON `ctf`.`card_type_id` = `ct`.`id` WHERE `ctf`.`id` = @id;"
				: "SELECT * FROM `card_type` WHERE `id` = @id;";
			List<SQLiteParameter> parameters = new List<SQLiteParameter>() { createParam("@id", DbType.Int64, id) };
			string[] cardTypeResult = execReadFields(sql, DBPath, ref userMessage, parameters, "id", "name", "context", "color");

			cardType = new CardType(cardTypeResult[0], cardTypeResult[1], (CardTypeContext)int.Parse(cardTypeResult[2]), int.Parse(cardTypeResult[3]), 0);

			// get fields
			parameters.Clear();
			parameters.Add(createParam("@id", DbType.Int64, cardType.ID));

			sql = "SELECT * FROM `card_type_field` WHERE `card_type_id` = @id ORDER BY `sort_order` ASC;";
			List<string[]> fieldResult = execReadListFields(sql, DBPath, ref userMessage, parameters, "id", "name", "field_type", "show_label", "ref_card_type_id");

			for (int i = 0; i < fieldResult.Count; i++)
			{
				string[] f = fieldResult[i];
				CardTypeField field = new CardTypeField(f[0], f[1], (DataType)int.Parse(f[2]), (i + 1).ToString(), f[3] != "0", f[4]);

				// get list type
				if (field.FieldType == DataType.List)
				{
					field.ListType = getCardType(field.RefCardTypeID, ref userMessage);
					field.ListType.Color = cardType.Color; // give it the same color as its owner
				}

				cardType.Fields.Add(field);
			}

			// get field count
			cardType.NumFields += cardType.Fields.Count;

			return cardType;
		}

		/// <summary>Retrieves a card type field.</summary>
		/// <param name="id">The database ID of the card type field.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The retrieved card type field.</returns>
		public static CardTypeField getCardTypeField(string id, ref string userMessage)
		{
			string cardTypeID;
			return getCardTypeField(id, ref userMessage, out cardTypeID);
		}

		/// <summary>Retrieves a card type field.</summary>
		/// <param name="id">The database ID of the card type field.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="cardTypeID">The database ID of the owning card type.</param>
		/// <returns>The retrieved card type field.</returns>
		public static CardTypeField getCardTypeField(string id, ref string userMessage, out string cardTypeID)
		{
			string sql = "SELECT * FROM `card_type_field` WHERE `id` = @id;";
			string[] result = execReadFields(sql, DBPath, ref userMessage, createParam("@id", DbType.Int64, id), "card_type_id", "name", "field_type", "sort_order", "show_label", "ref_card_type_id");

			CardTypeField field = new CardTypeField(id, result[1], (DataType)int.Parse(result[2]), result[3], result[4] != "0", result[5]);
			cardTypeID = result[0];

			// get list type if it's a list
			if (field.FieldType == DataType.List)
				field.ListType = getCardType(field.RefCardTypeID, ref userMessage);

			return field;
		}

		/// <summary>Gets the IDs and names of all card types.</summary>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>A list of two-dimensional arrays. [0] = ID; [1] = Name.</returns>
		public static List<string[]> getCardTypeIDsAndNames(ref string userMessage)
		{
			string sql = "SELECT `id`, `name` FROM `card_type` WHERE `context` = @context ORDER BY UPPER(`name`) ASC;";
			return execReadListFields(sql, DBPath, ref userMessage, createParam("@context", DbType.Int64, (int)CardTypeContext.Standalone), "id", "name");
		}

		/// <summary>Gets the IDs and names of all fields in a card type.</summary>
		/// <param name="cardTypeID">The database ID of the owning card type.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>A list of two-dimensional arrays. [0] = ID; [1] = Name.</returns>
		public static List<string[]> getCardTypeFieldIDsAndNames(string cardTypeID, ref string userMessage)
		{
			string sql = "SELECT `id`, `name` FROM `card_type_field` WHERE `card_type_id` = @card_type_id ORDER BY `sort_order` ASC;";
			return execReadListFields(sql, DBPath, ref userMessage, createParam("@card_type_id", DbType.Int64, cardTypeID), "id", "name");
		}

		/// <summary>Returns the database ID of the card type of the specified card.</summary>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The database ID of the card type of the specified card.</returns>
		public static string getCardTypeIDFromCardID(string cardID, ref string userMessage)
		{
			string sql = "SELECT `card_type_id` FROM `card` WHERE `id` = @id;";
			return execReadField(sql, DBPath, ref userMessage, createParam("@id", DbType.Int64, cardID), "card_type_id");
		}

		#endregion Card Types

		#region Cards

		/// <summary>Fills all blank card titles.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void fillBlankCardTitles(ref string userMessage)
		{
			SQLiteParameter[] parameters = new SQLiteParameter[2];

			string sql = @"
				SELECT `ft`.`id` AS `field_id`, `ct`.`id` AS `card_type_id`, `ct`.`name`
				FROM `card` `c`
					JOIN `field_text` `ft` ON `ft`.`card_id` = `c`.`id`
					JOIN `card_type_field` `ctf` ON `ctf`.`id` = `ft`.`card_type_field_id`
					LEFT JOIN `card_type_field` `ctf2` ON `ctf2`.`card_type_id` = `ctf`.`card_type_id` AND `ctf2`.`sort_order` < `ctf`.`sort_order`
					JOIN `card_type` `ct` ON `ct`.`id` = `c`.`card_type_id`
				WHERE `ctf2`.`id` IS NULL
					AND `ft`.`value` IS NULL OR `ft`.`value` = ''
				GROUP BY `c`.`id`;";

			List<string[]> cards = execReadListFields(sql, DBPath, ref userMessage, (IEnumerable<SQLiteParameter>)null, "field_id", "card_type_id", "name");

			sql = "UPDATE `field_text` SET `value` = @value WHERE `id` = @id;";
			int nextNum = 0;
			foreach (string[] card in cards)
			{
				string fieldID = card[0];
				string cardTypeID = card[1];
				string name = card[2];

				string title;

				do
				{
					nextNum++;
					title = name + " " + nextNum.ToString();
				} while (cardTitleExists(title, cardTypeID, ref userMessage));

				execNonQuery(sql, DBPath, ref userMessage, createParam("@value", DbType.String, title), createParam("@id", DbType.Int64, fieldID));
			}
		}

		/// <summary>Creates a new card.</summary>
		/// <param name="cardType">The type of card.</param>
		/// <param name="path">The path of the current database.</param>
		/// <param name="userMessage">Any user messages</param>
		/// <returns>The database ID of the new card.</returns>
		public static string newCard(CardType cardType, ref string userMessage)
		{
			StringBuilder sql = new StringBuilder();
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			resetParamNames();

			// get title
			string title = null;
			if (cardType.Context == CardTypeContext.Standalone)
			{
				for (int i = 0; ; i++)
				{
					title = cardType.Name + " " + (i + 1).ToString();
					if (!cardTitleExists(title, cardType.ID, ref userMessage))
						break;
				}
			}

			// insert record
			sql.Append(@"
				INSERT INTO `card` (`card_type_id`) VALUES (@card_type_id);
				SELECT LAST_INSERT_ROWID() AS `id`;");
			parameters.Add(createParam("@card_type_id", DbType.Int64, cardType.ID));

			string cardID = execReadField(sql.ToString(), DBPath, ref userMessage, parameters, "id");

			// insert fields
			StringBuilder fieldText = new StringBuilder();
			StringBuilder fieldCard = new StringBuilder();
			StringBuilder fieldCheckBox = new StringBuilder();
			parameters.Clear();

			bool titleField = cardType.Context == CardTypeContext.Standalone;
			if (titleField)
				parameters.Add(createParam("@title", DbType.String, title));
			
			foreach (CardTypeField f in cardType.Fields)
			{
				switch (f.FieldType)
				{
					case DataType.Text:
						fieldText.Append((fieldText.Length > 0 ? ", " : "") + @"
							(@card_id, " + getNextParamName("card_type_field_id") + ", " + (titleField ? "@title" : "''") + ")");
						parameters.Add(createParam(CurParamName, DbType.Int64, f.ID));
						titleField = false;
						break;
					case DataType.Card:
						fieldCard.Append((fieldCard.Length > 0 ? ", " : "") + @"
							(@card_id, " + getNextParamName("card_type_field_id") + ")");
						parameters.Add(createParam(CurParamName, DbType.Int64, f.ID));
						break;
					case DataType.List:
					case DataType.Image:
						// do nothing
						break;
					case DataType.CheckBox:
						fieldCheckBox.Append((fieldCheckBox.Length > 0 ? ", " : "") + @"
							(@card_id, " + getNextParamName("card_type_field_id") + ")");
						parameters.Add(createParam(CurParamName, DbType.Int64, f.ID));
						break;
					default:
						userMessage += "Unknown field type: " + f.FieldType;
						break;
				}
			}

			sql.Clear();

			if (fieldText.Length > 0)
			{
				sql.Append(@"
					INSERT INTO `field_text` (`card_id`, `card_type_field_id`, `value`) VALUES" + fieldText.ToString() + ";");
			}

			if (fieldCard.Length > 0)
			{
				sql.Append(@"
					INSERT INTO `field_card` (`card_id`, `card_type_field_id`) VALUES" + fieldCard.ToString() + ";");
			}

			if (fieldCheckBox.Length > 0)
			{
				sql.Append(@"
					INSERT INTO `field_checkbox` (`card_id`, `card_type_field_id`) VALUES" + fieldCheckBox.ToString() + ";");
			}

			if (sql.Length > 0)
			{
				parameters.Add(createParam("@card_id", DbType.Int64, cardID));
				execNonQuery(sql.ToString(), DBPath, ref userMessage, parameters);
			}

			return cardID;
		}

		/// <summary>Saves a card text field.</summary>
		/// <param name="value">The value to save to the card text field.</param>
		/// <param name="cardID">The card's database ID.</param>
		/// <param name="cardTypeFieldID">The card type field's database ID.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void saveCardTextField(string value, string cardID, string cardTypeFieldID, ref string userMessage)
		{
			string sql = "UPDATE `field_text` SET `value` = @value WHERE `card_id` = @card_id AND `card_type_field_id` = @card_type_field_id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@value", DbType.String, value),
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID)
			};

			execNonQuery(sql, DBPath, ref userMessage, parameters);
		}

		/// <summary>Saves a card card field.</summary>
		/// <param name="value">The value to save to the card card field.</param>
		/// <param name="cardID">The card's database ID.</param>
		/// <param name="cardTypeFieldID">The card type field's database ID.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void saveCardCardField(string value, string cardID, string cardTypeFieldID, ref string userMessage)
		{
			string sql = "UPDATE `field_card` SET `value` = @value WHERE `card_id` = @card_id AND `card_type_field_id` = @card_type_field_id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@value", DbType.String, value),
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID)
			};

			execNonQuery(sql, DBPath, ref userMessage, parameters);
		}

		/// <summary>Saves a card checkbox field.</summary>
		/// <param name="value">The value to save to the card checkbox field.</param>
		/// <param name="cardID">The card's database ID.</param>
		/// <param name="cardTypeFieldID">The card type field's database ID.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void saveCardCheckBoxField(bool value, string cardID, string cardTypeFieldID, ref string userMessage)
		{
			string sql = "UPDATE `field_checkbox` SET `value` = @value WHERE `card_id` = @card_id AND `card_type_field_id` = @card_type_field_id;";
			execNonQuery(sql, DBPath, ref userMessage, createParam("@value", DbType.Int64, (value ? 1 : 0)), createParam("@card_id", DbType.Int64, cardID), createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID));
		}

		/// <summary>Adds an image to a card field.</summary>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="cardTypeFieldID">The database IF of the card type field.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The database ID of the field image.</returns>
		public static string addCardImage(string cardID, string cardTypeFieldID, ref string userMessage)
		{
			string sql = @"
				INSERT INTO `field_image` (`card_id`, `card_type_field_id`) VALUES (@card_id, @card_type_field_id);
				SELECT LAST_INSERT_ROWID() AS `id`;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID)
			};

			return execReadField(sql, DBPath, ref userMessage, parameters, "id");
		}

		/// <summary>Removes an image from a card field.</summary>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="cardTypeFieldID">The database IF of the card type field.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void removeCardImage(string cardID, string cardTypeFieldID, ref string userMessage)
		{
			string sql = "DELETE FROM `field_image` WHERE `card_id` = @card_id AND `card_type_field_id` = @card_type_field_id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID)
			};

			execNonQuery(sql, DBPath, ref userMessage, parameters);
		}

		/// <summary>Gets a list of all image ids.</summary>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>A list of all image ids.</returns>
		public static List<string> getImageIDs(ref string userMessage)
		{
			string sql = "SELECT `id` FROM `field_image`;";
			return execReadListField(sql, DBPath, ref userMessage, (IEnumerable<SQLiteParameter>)null, "id");
		}

		/// <summary>Retrieves a card from the database.</summary>
		/// <param name="id">The database id of the card.</param>
		/// <param name="cardType">The card's card type.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The card.</returns>
		public static Card getCard(string id, CardType cardType, ref string userMessage)
		{
			Card card = new Card(cardType, id);

			List<SQLiteParameter> parameters = new List<SQLiteParameter>();

			parameters.Clear();
			parameters.Add(createParam("@card_id", DbType.Int64, id));
			parameters.Add(createParam("@card_type_id", DbType.Int64, cardType.ID));

			bool hasTextField = false;
			bool hasCardField = false;
			bool hasListField = false;
			bool hasImageField = false;
			bool hasCheckBoxField = false;

			foreach (CardTypeField f in cardType.Fields)
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
					case DataType.Image:
						hasImageField = true;
						break;
					case DataType.CheckBox:
						hasCheckBoxField = true;
						break;
					default:
						userMessage += "Unknown field type: " + f.FieldType.ToString();
						break;
				}
			}

			// get text fields
			if (hasTextField)
			{
				string sql = @"
					SELECT `ft`.`value`
					FROM `field_text` `ft`
						JOIN `card_type_field` `ctf` ON `ctf`.`id` = `ft`.`card_type_field_id`
					WHERE `ft`.`card_id` = @card_id
						AND `ctf`.`card_type_id` = @card_type_id
					ORDER BY `ctf`.`sort_order` ASC;";

				List<string> textFieldResult = execReadListField(sql, DBPath, ref userMessage, parameters, "value");

				// fill text fields
				for (int i = 0, j = 0; i < cardType.Fields.Count && j < textFieldResult.Count; i++)
				{
					if (cardType.Fields[i].FieldType == DataType.Text)
					{
						card.Fields[i] = textFieldResult[j];
						j++;
					}
				}
			}

			// get card fields
			if (hasCardField)
			{
				string sql = @"
					SELECT `fc`.`value`
					FROM `field_card` `fc`
						JOIN `card_type_field` `ctf` ON `ctf`.`id` = `fc`.`card_type_field_id`
					WHERE `fc`.`card_id` = @card_id
						AND `ctf`.`card_type_id` = @card_type_id
					ORDER BY `ctf`.`sort_order` ASC;";

				List<string> cardFieldResult = execReadListField(sql, DBPath, ref userMessage, parameters, "value");

				// fill card fields
				for (int i = 0, j = 0; i < cardType.Fields.Count && j < cardFieldResult.Count; i++)
				{
					if (cardType.Fields[i].FieldType == DataType.Card)
					{
						card.Fields[i] = cardFieldResult[j];
						j++;
					}
				}
			}

			// get list fields
			if (hasListField)
			{
				string sql = @"
					SELECT `fl`.`value`, `ctf`.`sort_order` AS `ctf_sort_order`
					FROM `field_list` `fl`
						JOIN `card_type_field` `ctf` ON `ctf`.`id` = `fl`.`card_type_field_id`
					WHERE `fl`.`card_id` = @card_id
						AND `ctf`.`card_type_id` = @card_type_id
					ORDER BY `ctf`.`sort_order` ASC, `fl`.`sort_order` ASC;";

				List<string[]> listFieldResult = execReadListFields(sql, DBPath, ref userMessage, parameters, "value", "ctf_sort_order");

				List<Card> items = new List<Card>();
				string lastSortOrder = null;

				int fieldIndex = -1;
				for (int i = 0; i < listFieldResult.Count && fieldIndex < cardType.Fields.Count; i++)
				{
					// if current list field is complete
					if (listFieldResult[i][1] != lastSortOrder)
					{
						lastSortOrder = listFieldResult[i][1];

						if (items.Count > 0)
						{
							card.Fields[fieldIndex] = items;
							items = new List<Card>();
						}

						do
						{
							fieldIndex++;
						} while (cardType.Fields[fieldIndex].FieldType != DataType.List);
					}

					// add list item
					Card listCard = getCard(listFieldResult[i][0], cardType.Fields[fieldIndex].ListType, ref userMessage);
					items.Add(listCard);
				}

				// add last items
				if (items.Count > 0)
					card.Fields[fieldIndex] = items;

				// fill empty lists
				for (int i = 0; i < cardType.Fields.Count; i++)
				{
					if (cardType.Fields[i].FieldType == DataType.List && card.Fields[i] == null)
						card.Fields[i] = new List<Card>();
				}
			}

			// get image fields
			if (hasImageField)
			{
				string sql = @"
					SELECT `fi`.`id`
					FROM `field_image` `fi`
						JOIN `card_type_field` `ctf` ON `ctf`.`id` = `fi`.`card_type_field_id`
					WHERE `fi`.`card_id` = @card_id
						AND `ctf`.`card_type_id` = @card_type_id
					ORDER BY `ctf`.`sort_order` ASC;";

				List<string> imageFieldResult = execReadListField(sql, DBPath, ref userMessage, parameters, "id");

				// fill image fields
				for (int i = 0, j = 0; i < cardType.Fields.Count && j < imageFieldResult.Count; i++)
				{
					if (cardType.Fields[i].FieldType == DataType.Image)
					{
						card.Fields[i] = imageFieldResult[j];
						j++;
					}
				}
			}

			// get checkbox fields
			if (hasCheckBoxField)
			{
				string sql = @"
						SELECT `fch`.`value`
						FROM `field_checkbox` `fch`
							JOIN `card_type_field` `ctf` ON `ctf`.`id` = `fch`.`card_type_field_id`
						WHERE `fch`.`card_id` = @card_id
							AND `ctf`.`card_type_id` = @card_type_id
						ORDER BY `ctf`.`sort_order` ASC;";

				List<string> checkBoxFieldResult = execReadListField(sql, DBPath, ref userMessage, parameters, "value");

				// fill checkbox fields
				for (int i = 0, j = 0; i < cardType.Fields.Count && j < checkBoxFieldResult.Count; i++)
				{
					if (cardType.Fields[i].FieldType == DataType.CheckBox)
					{
						card.Fields[i] = checkBoxFieldResult[j] != "0";
						j++;
					}
				}
			}

			return card;
		}

		/// <summary>Gets the database ID of a card's card type.</summary>
		/// <param name="id">The database ID of the card.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The database ID of the card type.</returns>
		public static string getCardCardTypeID(string id, ref string userMessage)
		{
			string sql = "SELECT `card_type_id` FROM `card` WHERE `id` = @card_id;";
			return execReadField(sql, DBPath, ref userMessage, createParam("@card_id", DbType.Int64, id), "card_type_id");
		}

		/// <summary>Determines whether or not a card exists in the database.</summary>
		/// <param name="id">The database ID of the card.</param>
		/// <param name="userMessage">Any error messages.</param>
		/// <returns>Whether or not the card exists in the database.</returns>
		public static bool cardExists(string id, ref string userMessage)
		{
			string sql = "SELECT `id` FROM `card` WHERE `id` = @id LIMIT 1;";

			List<string> result = execReadListField(sql, DBPath, ref userMessage, createParam("@id", DbType.Int64, id), "id");

			return result.Count > 0;
		}

		/// <summary>Adds a new list item.</summary>
		/// <param name="card">The card that owns the list.</param>
		/// <param name="cardTypeField">The list to add to.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The database ID of the list item.</returns>
		public static string newListItem(Card card, CardTypeField cardTypeField, ref string userMessage)
		{
			return newListItem(card.ID, cardTypeField.ID, cardTypeField.ListType, ref userMessage);
		}

		/// <summary>Adds a new list item.</summary>
		/// <param name="cardID">The database ID of the card that owns the list.</param>
		/// <param name="cardTypeFieldID">The database ID of the list field to add to.</param>
		/// <param name="listType">The type of list item.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The database ID of the list item.</returns>
		public static string newListItem(string cardID, string cardTypeFieldID, CardType listType, ref string userMessage)
		{
			// get next sort order
			string sql = "SELECT COALESCE(MAX(`sort_order`), 0) + 1 AS `next_sort_order` FROM `field_list` WHERE `card_id` = @card_id AND `card_type_field_id` = @card_type_field_id;";
			List<SQLiteParameter> parameters = new List<SQLiteParameter>();
			parameters.Add(createParam("@card_id", DbType.Int64, cardID));
			parameters.Add(createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID));

			string orderResult = execReadField(sql, DBPath, ref userMessage, parameters, "next_sort_order");

			// add new card
			string id = newCard(listType, ref userMessage);

			// add field_list record
			sql = @"
				-- insert the field_list record
				INSERT INTO `field_list` (`card_id`, `card_type_field_id`, `value`, `sort_order`)
				VALUES (@card_id, @card_type_field_id, @value, @sort_order);

				-- insert arrangement cards for the new list item to every relevant arrangement
				INSERT INTO `arrangement_card` (`arrangement_id`, `card_id`)
				SELECT `arrangement_id`, @value FROM `arrangement_card` WHERE `card_id` = @card_id;

				INSERT INTO `arrangement_card_list` (`arrangement_card_id`)
				SELECT `id` FROM `arrangement_card` WHERE `card_id` = @value;

				-- insert arrangement records for all text fields in the item
				INSERT INTO `arrangement_field_text` (`arrangement_card_id`, `card_type_field_id`)
				SELECT `ac`.`id`, `ctf`.`id`
				FROM `card_type_field` `ctf`
					JOIN `card_type` `ct` ON `ct`.`id` = `ctf`.`card_type_id`
					JOIN `card` `c` ON `c`.`card_type_id` = `ct`.`id`
					JOIN `arrangement_card` `ac` ON `ac`.`card_id` = `c`.`id`
				WHERE `c`.`id` = @value
					AND `ctf`.`field_type` = @text_type;

				-- insert arrangement records for all list fields in the item
				INSERT INTO `arrangement_field_list` (`arrangement_card_id`, `card_type_field_id`)
				SELECT `ac`.`id`, `ctf`.`id`
				FROM `card_type_field` `ctf`
					JOIN `card_type` `ct` ON `ct`.`id` = `ctf`.`card_type_id`
					JOIN `card` `c` ON `c`.`card_type_id` = `ct`.`id`
					JOIN `arrangement_card` `ac` ON `ac`.`card_id` = `c`.`id`
				WHERE `c`.`id` = @value
					AND `ctf`.`field_type` = @list_type;";

			parameters.Add(createParam("@value", DbType.Int64, id));
			parameters.Add(createParam("@sort_order", DbType.Int64, orderResult));
			parameters.Add(createParam("@text_type", DbType.Int64, (int)DataType.Text));
			parameters.Add(createParam("@list_type", DbType.Int64, (int)DataType.List));

			execNonQuery(sql, DBPath, ref userMessage, parameters);

			return id;
		}

		/// <summary>Swap two list items (the items must be adjacent and item1 must come before item2).</summary>
		/// <param name="listItem1ID">The database ID of the first list item's card ID.</param>
		/// <param name="listItem2ID">The database ID of the second list item's card ID.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void swapListItems(string listItem1ID, string listItem2ID, ref string userMessage)
		{
			string sql = @"
				UPDATE `field_list`
				SET `sort_order` = (SELECT `sort_order` FROM `field_list` WHERE `value` = @list_item1)
				WHERE `value` = @list_item2;

				UPDATE `field_list`
				SET `sort_order` = `sort_order` + 1
				WHERE `value` = @list_item1;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
				{
					createParam("@list_item1", DbType.Int64, listItem1ID),
					createParam("@list_item2", DbType.Int64, listItem2ID)
				};

			execNonQuery(sql, DBPath, ref userMessage, parameters);
		}

		/// <summary>Search for a specific string query.</summary>
		/// <param name="query">The search query.</param>
		/// <param name="cardTypes">A comma-delimited list of card type IDs to search.</param>
		/// <param name="includeCardType">Whether or not to include the card type names in the results.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The search results.</returns>
		public static SearchResult[] search(string query, string cardTypes, bool includeCardType, ref string userMessage)
		{
			// TODO: sort by relevance (number of keyword matches)

			SearchResult[] searchResults;

			// process keywords
			string[] keywords = query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			SQLiteParameter[] parameters = new SQLiteParameter[keywords.Length];

			StringBuilder bSql = new StringBuilder();
			for (int i = 0; i < keywords.Length; i++)
			{
				bSql.Append((bSql.Length > 0 ? " AND " : "") + "`ft`.`value` LIKE @keyword" + i.ToString());
				parameters[i] = createParam("@keyword" + i.ToString(), DbType.String, "%" + keywords[i].Replace("%", string.Empty) + "%");
			}

			if (bSql.Length == 0)
			{
				searchResults = new SearchResult[0];
			}
			else
			{
				// get ids
				if (string.IsNullOrEmpty(cardTypes))
				{
					bSql.Insert(0, @"
					SELECT DISTINCT COALESCE(`fl`.`card_id`, `c`.`id`) AS `id`
					FROM `field_text` `ft`
						JOIN `card` `c` ON `c`.`id` = `ft`.`card_id`
						LEFT JOIN `field_list` `fl` ON `fl`.`value` = `c`.`id`
					WHERE ");
				}
				else
				{
					bSql.Insert(0, @"
					SELECT DISTINCT COALESCE(`c2`.`id`, `c`.`id`) AS `id`
					FROM `field_text` `ft`
						JOIN `card` `c` ON `c`.`id` = `ft`.`card_id`
						LEFT JOIN `field_list` `fl` ON `fl`.`value` = `c`.`id`
						LEFT JOIN `card` `c2` ON `c2`.`id` = `fl`.`card_id`
					WHERE (`c`.`card_type_id` IN (" + cardTypes + @")
						OR `c2`.`card_type_id` IN (" + cardTypes + @"))
						AND ");
				}

				bSql.Append(";");

				IEnumerable<string> ids = execReadListField(bSql.ToString(), DBPath, ref userMessage, parameters, "id");

				// get card information
				string sql = @"
				SELECT `ft`.`card_id`, " + (includeCardType ? ("`ct`.`name` || ' - ' || ") : "") + @"`ft`.`value` AS `name`, `ct`.`color`
				FROM `field_text` `ft`
					JOIN `card_type_field` `ctf` ON `ctf`.`id` = `ft`.`card_type_field_id`
					JOIN `card_type` `ct` ON `ct`.`id` = `ctf`.`card_type_id`
					LEFT JOIN `card_type_field` `ctf2` ON `ctf2`.`card_type_id` = `ctf`.`card_type_id` AND `ctf2`.`sort_order` < `ctf`.`sort_order`
				WHERE `ctf2`.`id` IS NULL
					AND `ft`.`card_id` IN (" + string.Join(",", ids) + ");";

				List<string[]> results = execReadListFields(sql, DBPath, ref userMessage, (IEnumerable<SQLiteParameter>)null, "card_id", "name", "color");
				searchResults = new SearchResult[results.Count];

				for (int i = 0; i < searchResults.Length; i++)
				{
					byte red, green, blue;
					getColorsFromInt(int.Parse(results[i][2]), out red, out green, out blue);

					searchResults[i] = new SearchResult()
					{
						ID = results[i][0],
						Title = results[i][1],
						Color = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(red, green, blue))
					};
				}
			}

			return searchResults;
		}

		/// <summary>Gets the title of a card.</summary>
		/// <param name="id">The database ID of the card.</param>
		/// <param name="includeCardType">Whether or not to include the card type name in the result.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The ids and names of the cards.</returns>
		public static string getCardTitle(string id, bool includeCardType, ref string userMessage)
		{
			string sql = @"
				SELECT `ft`.`card_id`, " + (includeCardType ? ("`ct`.`name` || ' - ' || ") : "") + @"`ft`.`value` AS `title`
				FROM `field_text` `ft`
					JOIN `card_type_field` `ctf` ON `ctf`.`id` = `ft`.`card_type_field_id`
					" + (includeCardType ? "JOIN `card_type` `ct` ON `ct`.`id` = `ctf`.`card_type_id`" : "") + @"
					LEFT JOIN `card_type_field` `ctf2` ON `ctf2`.`card_type_id` = `ctf`.`card_type_id` AND `ctf2`.`sort_order` < `ctf`.`sort_order`
				WHERE `ctf2`.`id` IS NULL
					AND `ft`.`card_id` = @id;";

			return execReadField(sql, DBPath, ref userMessage, createParam("@id", DbType.Int64, id), "title");
		}

		/// <summary>Returns whether or not a card of the specified card type with the specified title exists.</summary>
		/// <param name="title">The title to search for.</param>
		/// <param name="cardTypeID">The database ID of the card type to search.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>Whether or not a card of the specified card type with the specified title exists.</returns>
		public static bool cardTitleExists(string title, string cardTypeID, ref string userMessage)
		{
			string sql = @"
				SELECT `ft`.`id`
				FROM `card` `c`
					JOIN `field_text` `ft` ON `ft`.`card_id` = `c`.`id`
					JOIN `card_type_field` `ctf` ON `ctf`.`id` = `ft`.`card_type_field_id`
					LEFT JOIN `card_type_field` `ctf2` ON `ctf2`.`card_type_id` = `ctf`.`card_type_id` AND `ctf2`.`sort_order` < `ctf`.`sort_order`
				WHERE `c`.`card_type_id` = " + cardTypeID + @"
					AND `ctf2`.`id` IS NULL
					AND `ft`.`value` = @title
				LIMIT 1;";

			return execReadListField(sql, DBPath, ref userMessage, createParam("@title", DbType.String, title), "id").Count != 0;
		}

		/// <summary>Deletes a card from the database.</summary>
		/// <param name="cardID">The card's database ID.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void deleteCard(string cardID, ref string userMessage)
		{
			string sql = "DELETE FROM `card` WHERE `id` = @id;";
			execNonQuery(sql, DBPath, ref userMessage, createParam("@id", DbType.Int64, cardID));
		}

		#endregion Cards

		#region Arrangements

		/// <summary>Adds a new arrangement.</summary>
		/// <param name="name">The name of the new arrangmenet.</param>
		/// <param name="newName">The name of the new arrangement (same as name unless name is null).</param>
		/// <param name="userMessage">Any user messages</param>
		/// <returns>The database ID of the new arrangement.</returns>
		public static string addArrangement(string name, out string newName, ref string userMessage)
		{
			// get new name
			if (string.IsNullOrEmpty(name))
			{
				string nameSql = "SELECT `name` FROM `arrangement` WHERE `name` LIKE @name;";

				List<string> names = execReadListField(nameSql, DBPath, ref userMessage, createParam("@name", DbType.String, NewArrangementNameLike), "name");

				name = findNextName(names, NewArrangementName, NewArrangementNameStart, NewArrangementNameEnd, NewArrangementNameIndex);
			}

			newName = name;

			string sql = @"
				INSERT INTO `arrangement` (`name`) VALUES (@name);
				SELECT LAST_INSERT_ROWID() AS `id`;";

			return execReadField(sql, DBPath, ref userMessage, createParam("@name", DbType.String, name), "id");
		}

		/// <summary>Removes an arrangement from the database.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void removeArrangement(string arrangementID, ref string userMessage)
		{
			string sql = "DELETE FROM `arrangement` WHERE `id` = @id;";
			execNonQuery(sql, DBPath, ref userMessage, createParam("@id", DbType.Int64, arrangementID));
		}

		/// <summary>Gets the IDs and names of all arrangements.</summary>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The results.</returns>
		public static List<string[]> getArrangementIDsAndNames(ref string userMessage)
		{
			string sql = "SELECT `id`, `name` FROM `arrangement` ORDER BY UPPER(`name`);";
			return execReadListFields(sql, DBPath, ref userMessage, (IEnumerable<SQLiteParameter>)null, "id", "name");
		}

		/// <summary>Retrieves an arrangement card from the database.</summary>
		/// <param name="arrangementCardID">The database ID of the arrangement card.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The arrangement card.</returns>
		public static ArrangementCardStandalone getArrangementCard(string arrangementCardID, ref string userMessage)
		{
			string sql = @"
				SELECT `ac`.`card_id`, `acs`.`x`, `acs`.`y`, `acs`.`width`, `ac`.`arrangement_id`
				FROM `card` `c`
					JOIN `arrangement_card` `ac` ON `ac`.`card_id` = `c`.`id`
					JOIN `arrangement_card_standalone` `acs` ON `acs`.`arrangement_card_id` = `ac`.`id`
				WHERE `ac`.`id` = @id;";

			string[] result = execReadFields(sql, DBPath, ref userMessage, createParam("@id", DbType.Int64, arrangementCardID), "card_id", "x", "y", "width", "arrangement_id");

			string arrangementID = result[4];

			// build arrangement card
			ArrangementCardStandalone card = new ArrangementCardStandalone(arrangementCardID, result[0], null, int.Parse(result[1]), int.Parse(result[2]), int.Parse(result[3]));

			// get text fields
			card.TextFields = getArrangementCardTextFields(card.ID, ref userMessage);

			// get list fields
			card.ListFields = getArrangementCardListFields(card.ID, ref userMessage);

			// get list items
			card.ListItems = getArrangementCardListItems(arrangementID, card.CardID, ref userMessage);

			return card;
		}

		/// <summary>Retrieves an arrangement from the database.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The cards in the arrangement.</returns>
		public static ArrangementCardStandalone[] getArrangement(string arrangementID, ref string userMessage)
		{
			string sql = @"
				SELECT `ac`.`id`, `ac`.`card_id`, `acs`.`x`, `acs`.`y`, `acs`.`width`
				FROM `arrangement_card` `ac`
					JOIN `arrangement_card_standalone` `acs` ON `acs`.`arrangement_card_id` = `ac`.`id`
				WHERE `ac`.`arrangement_id` = @id;";

			List<string[]> results = execReadListFields(sql, DBPath, ref userMessage, createParam("@id", DbType.Int64, arrangementID), "id", "card_id", "x", "y", "width");

			ArrangementCardStandalone[] cards = new ArrangementCardStandalone[results.Count];

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

				sql = "SELECT `card_type_id` FROM `card` WHERE `id` = @id;";
				string cardTypeID = execReadField(sql, DBPath, ref userMessage, createParam("@id", DbType.Int64, card.CardID), "card_type_id");

				// get text fields
				card.TextFields = getArrangementCardTextFields(card.ID, ref userMessage);

				// get list fields
				card.ListFields = getArrangementCardListFields(card.ID, ref userMessage);

				// get list items
				card.ListItems = getArrangementCardListItems(arrangementID, card.CardID, ref userMessage);

				// finish card
				cards[i] = card;
			}

			return cards;
		}

		/// <summary>Retrieves the arrangement card list item settings.</summary>
		/// <param name="arrangementID">The database ID of the owning arrangement.</param>
		/// <param name="cardID">The database ID of the owning card.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The arrangement card list item settings.</returns>
		private static ArrangementCardList[] getArrangementCardListItems(string arrangementID, string cardID, ref string userMessage)
		{
			string sql = @"
				SELECT `ac`.`id`, `fl`.`card_id`, `acl`.`minimized`, `ctf`.`sort_order` AS `field_order`, `fl`.`sort_order` AS `item_order`
				FROM `field_list` `fl`
					JOIN `card_type_field` `ctf` ON `ctf`.`id` = `fl`.`card_type_field_id`
					JOIN `card` `c` ON `c`.`id` = `fl`.`value`
					JOIN `arrangement_card` `ac` ON `ac`.`card_id` = `c`.`id`
					JOIN `arrangement_card_list` `acl` ON `acl`.`arrangement_card_id` = `ac`.`id`
				WHERE `fl`.`card_id` = @card_id
					AND `ac`.`arrangement_id` = @arrangement_id
				ORDER BY `field_order`, `item_order`;";

			SQLiteParameter[] listParams = new SQLiteParameter[]
			{
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@arrangement_id", DbType.Int64, arrangementID)
			};

			List<string[]> listItems = execReadListFields(sql, DBPath, ref userMessage, listParams, "id", "card_id", "minimized");

			ArrangementCardList[] results = null;
			if (listItems.Count > 0)
			{
				results = new ArrangementCardList[listItems.Count];

				for (int listIndex = 0; listIndex < listItems.Count; listIndex++)
				{
					string[] f = listItems[listIndex];
					results[listIndex] = new ArrangementCardList(f[0], f[1], getArrangementCardTextFields(f[0], ref userMessage), f[2] != "0");
				}
			}

			return results;
		}

		/// <summary>Retrieves the arrangement card list field settings.</summary>
		/// <param name="arrangementCardID">The database ID of the owning arrangement card.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The arrangement card list field settings.</returns>
		private static ArrangementFieldList[] getArrangementCardListFields(string arrangementCardID, ref string userMessage)
		{
			string sql = @"
				SELECT `ctf`.`id`, `afl`.`minimized`, `ctf`.`sort_order`
				FROM `arrangement_field_list` `afl`
					JOIN `card_type_field` `ctf` ON `ctf`.`id` = `afl`.`card_type_field_id`
				WHERE `afl`.`arrangement_card_id` = @arrangement_card_id
				ORDER BY `sort_order`;";

			List<string[]> listFields = execReadListFields(sql, DBPath, ref userMessage, createParam("@arrangement_card_id", DbType.Int64, arrangementCardID), "id", "minimized");

			ArrangementFieldList[] results = new ArrangementFieldList[listFields.Count];
			for (int j = 0; j < listFields.Count; j++)
			{
				results[j] = new ArrangementFieldList(listFields[j][0], listFields[j][1] != "0");
			}

			return results;
		}

		/// <summary>Retrieves the arrangement card text field settings.</summary>
		/// <param name="arrangementCardID">The database ID of the owning arrangement card.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The text field settings.</returns>
		private static ArrangementFieldText[] getArrangementCardTextFields(string arrangementCardID, ref string userMessage)
		{
			string sql = @"
				SELECT `ctf`.`id`, `aft`.`height_increase`, `ctf`.`sort_order`
				FROM `arrangement_field_text` `aft`
					JOIN `card_type_field` `ctf` ON `ctf`.`id` = `aft`.`card_type_field_id`
				WHERE `aft`.`arrangement_card_id` = @arrangement_card_id
				ORDER BY `sort_order`;";

			List<string[]> textFields = execReadListFields(sql, DBPath, ref userMessage, createParam("@arrangement_card_id", DbType.Int64, arrangementCardID), "id", "height_increase");

			ArrangementFieldText[] fields = new ArrangementFieldText[textFields.Count];
			for (int j = 0; j < textFields.Count; j++)
			{
				fields[j] = new ArrangementFieldText(textFields[j][0], int.Parse(textFields[j][1]));
			}

			return fields;
		}

		/// <summary>Gets an arrangement list card's database ID.</summary>
		/// <param name="owningArrCardID">The arrangement card ID of the owning card.</param>
		/// <param name="cardID">The database ID of the list item.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The arrangement list card's database ID.</returns>
		public static string getArrangementListCardID(string owningArrCardID, string cardID, ref string userMessage)
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

			return execReadField(sql, DBPath, ref userMessage, parameters, "id");
		}

		/// <summary>Gets a list of all arrangement list card IDs within a specific arrangement card.</summary>
		/// <param name="owningArrCardID">The arrangement card ID of the owning card.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>A list of all arrangement list card IDs within a specific arrangement card.</returns>
		public static List<string> getArrangementListCardIDs(string owningArrCardID, ref string userMessage)
		{
			string sql = @"
				SELECT `ac2`.`id`
				FROM `arrangement_card` `ac`
					JOIN `field_list` `fl` ON `fl`.`card_id` = `ac`.`card_id`
					JOIN `arrangement_card` `ac2` ON `ac2`.`card_id` = `fl`.`value` AND `ac2`.`arrangement_id` = `ac`.`arrangement_id`
				WHERE `ac`.`id` = @owner_id
				ORDER BY `fl`.`sort_order` ASC;";

			return execReadListField(sql, DBPath, ref userMessage, createParam("@owner_id", DbType.Int64, owningArrCardID), "id");
		}

		/// <summary>Sets a card's position and size in an arrangement.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="x">The x-coordinate of the card in the arrangement.</param>
		/// <param name="y">The y-coordinate of the card in the arrangement.</param>
		/// <param name="width">The width of the card in the arrangement.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void setCardPosAndSize(string arrangementID, string cardID, int x, int y, int width, ref string userMessage)
		{
			string sql = "UPDATE `arrangement_card_standalone` SET `x` = @x, `y` = @y, `width` = @width WHERE `arrangement_card_id` = (SELECT `id` FROM `arrangement_card` WHERE `arrangement_id` = @arrangement_id AND `card_id` = @card_id);";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@arrangement_id", DbType.Int64, arrangementID),
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@x", DbType.Int64, x),
				createParam("@y", DbType.Int64, y),
				createParam("@width", DbType.Int64, width)
			};

			execNonQuery(sql, DBPath, ref userMessage, parameters);
		}

		/// <summary>Sets a text field's height increase for an arrangement.</summary>
		/// <param name="arrangementCardID">The arrangement card's database ID.</param>
		/// <param name="cardTypeFieldID">The card type field's database ID.</param>
		/// <param name="heightIncrease">The text field's height increase.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void setFieldTextHeightIncrease(string arrangementCardID, string cardTypeFieldID, int heightIncrease, ref string userMessage)
		{
			string sql = "UPDATE `arrangement_field_text` SET `height_increase` = @height_increase WHERE `card_type_field_id` = @card_type_field_id AND `arrangement_card_id` = @arrangement_card_id;";

			execNonQuery(sql, DBPath, ref userMessage,
				createParam("@arrangement_card_id", DbType.Int64, arrangementCardID),
				createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID),
				createParam("@height_increase", DbType.Int64, heightIncrease));
		}

		/// <summary>Sets whether or not a list field is minimized in an arrangement.</summary>
		/// <param name="arrangementCardID">The arrangement card's database ID.</param>
		/// <param name="cardTypeFieldID">The card type field's database ID.</param>
		/// <param name="minimized">Whether or not the list field is minimized in the arrangement.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void setFieldListMinimized(string arrangementCardID, string cardTypeFieldID, bool minimized, ref string userMessage)
		{
			string sql = "UPDATE `arrangement_field_list` SET `minimized` = @minimized WHERE `card_type_field_id` = @card_type_field_id AND `arrangement_card_id` = @arrangement_card_id;";

			execNonQuery(sql, DBPath, ref userMessage,
				createParam("@arrangement_card_id", DbType.Int64, arrangementCardID),
				createParam("@card_type_field_id", DbType.Int64, cardTypeFieldID),
				createParam("@minimized", DbType.Int64, (minimized ? "1" : "0")));
		}

		/// <summary>Sets whether or not a list item is minimized in an arrangement.</summary>
		/// <param name="arrangementCardID">The database ID of the arrangement card.</param>
		/// <param name="minimized">Whether or not the list item is minimized.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void setListItemMinimized(string arrangementCardID, bool minimized, ref string userMessage)
		{
			string sql = "UPDATE `arrangement_card_list` SET `minimized` = @minimized WHERE `arrangement_card_id` = @arrangement_card_id;";
			execNonQuery(sql, DBPath, ref userMessage, createParam("@minimized", DbType.Int64, (minimized ? 1 : 0)), createParam("@arrangement_card_id", DbType.Int64, arrangementCardID));
		}

		/// <summary>Adds a card to an arrangement.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="x">The x-coordinate of the card in the arrangement.</param>
		/// <param name="y">The y-coordinate of the card in the arrangement.</param>
		/// <param name="width">The width of the card in the arrangement.</param>
		/// <param name="listFieldsMinimized">Whether or not all list fields are minimized.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <returns>The database ID of the arrangement card.</returns>
		public static string arrangementAddCard(string arrangementID, string cardID, int x, int y, int width, bool listFieldsMinimized, ref string userMessage)
		{
			string type = getCardTypeIDFromCardID(cardID, ref userMessage);

			string sql = @"
				INSERT INTO `arrangement_card` (`arrangement_id`, `card_id`)
				VALUES (@arrangement_id, @card_id);

				CREATE TEMPORARY TABLE `ac_id`(`id` INTEGER PRIMARY KEY);
				INSERT INTO `ac_id`
				VALUES (LAST_INSERT_ROWID());

				INSERT INTO `arrangement_card_standalone` (`arrangement_card_id`, `x`, `y`, `width`)
				SELECT `id`, @x, @y, @width FROM `ac_id`;

				INSERT INTO `arrangement_field_text` (`arrangement_card_id`, `card_type_field_id`)
				SELECT `ac_id`.`id`, `ctf`.`id`
				FROM `card_type_field` `ctf`
					JOIN `ac_id`
				WHERE `ctf`.`field_type` = @text_type
					AND `ctf`.`card_type_id` = " + type + @";

				INSERT INTO `arrangement_field_list` (`arrangement_card_id`, `card_type_field_id`, `minimized`)
				SELECT `ac_id`.`id`, `ctf`.`id`, @list_minimized
				FROM `card_type_field` `ctf`
					JOIN `ac_id`
				WHERE `ctf`.`field_type` = @list_type
					AND `ctf`.`card_type_id` = " + type + @";

				INSERT INTO `arrangement_card` (`arrangement_id`, `card_id`)
				SELECT @arrangement_id, `value` FROM `field_list` `fl` WHERE `fl`.`card_id` = @card_id;

				INSERT INTO `arrangement_card_list` (`arrangement_card_id`)
				SELECT `ac`.`id`
				FROM `arrangement_card` `ac`
					JOIN `field_list` `fl` ON `fl`.`value` = `ac`.`card_id`
				WHERE `ac`.`arrangement_id` = @arrangement_id
					AND `fl`.`card_id` = @card_id;

				INSERT INTO `arrangement_field_text` (`arrangement_card_id`, `card_type_field_id`)
				SELECT `ac`.`id`, `ctf`.`id`
				FROM `field_list` `fl`
					JOIN `card` `c` ON `c`.`id` = `fl`.`value`
					JOIN `arrangement_card` `ac` ON `ac`.`card_id` = `c`.`id`
					JOIN `card_type_field` `ctf` ON `ctf`.`card_type_id` = `c`.`card_type_id`
				WHERE `ac`.`arrangement_id` = @arrangement_id
					AND `fl`.`card_id` = @card_id
					AND `ctf`.`field_type` = @text_type;

				SELECT `id` FROM `ac_id`;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@arrangement_id", DbType.Int64, arrangementID),
				createParam("@card_id", DbType.Int64, cardID),
				createParam("@x", DbType.Int64, x),
				createParam("@y", DbType.Int64, y),
				createParam("@width", DbType.Int64, width),
				createParam("@list_minimized", DbType.Int64, (listFieldsMinimized ? 1 : 0)),
				createParam("@text_type", DbType.Int64, (int)DataType.Text),
				createParam("@list_type", DbType.Int64, (int)DataType.List)
			};

			return execReadField(sql, DBPath, ref userMessage, parameters, "id");
		}

		/// <summary>Removes a card from an arrangement.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="cardID">The database ID of the card.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void arrangementRemoveCard(string arrangementID, string cardID, ref string userMessage)
		{
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

			execNonQuery(sql, DBPath, ref userMessage, parameters);
		}

		/// <summary>Changes an arrangement's name.</summary>
		/// <param name="arrangementID">The database ID of the arrangement.</param>
		/// <param name="newName">The arrangement's new name.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void arrangementChangeName(string arrangementID, string newName, ref string userMessage)
		{
			string sql = "UPDATE `arrangement` SET `name` = @name WHERE `id` = @id;";

			SQLiteParameter[] parameters = new SQLiteParameter[]
			{
				createParam("@id", DbType.Int64, arrangementID),
				createParam("@name", DbType.String, newName)
			};

			execNonQuery(sql, DBPath, ref userMessage, parameters);
		}

		public static List<string[]> getArrangementCardConnections(string arrangementID, ref string userMessage)
		{
			string sql = @"
				SELECT `ac`.`card_id`, `fc`.`value`
				FROM `arrangement_card` `ac`
					LEFT JOIN `field_list` `fl` ON `fl`.`card_id` = `ac`.`card_id`
					JOIN `field_card` `fc` ON `fc`.`card_id` IN (`ac`.`card_id`, `fl`.`value`)
					JOIN `arrangement_card` `ac2` ON `ac2`.`card_id` = `fc`.`value` AND `ac2`.`arrangement_id` = `ac`.`arrangement_id`
				WHERE `ac`.`arrangement_id` = @arrangement_id;";

			return execReadListFields(sql, DBPath, ref userMessage, createParam("@arrangement_id", DbType.Int64, arrangementID), "card_id", "value");
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

		/// <summary>Waits for the thread to finish (if one is running).</summary>
		private static void waitForThread()
		{
			if (thread != null && thread.ThreadState != ThreadState.Stopped)
				thread.Join();
		}

		/// <summary>Executes a nonquery SQL string without the BEGIN and COMMIT commands around it.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		public static void execNonQueryWithoutBeginCommit(string sql, string path, ref string userMessage)
		{
			try
			{
				using (SQLiteConnection con = new SQLiteConnection(genConnectionString(path)))
				using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
				{
					con.Open();
					cmd.ExecuteNonQuery();
				}
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}
		}

		/// <summary>Executes a nonquery SQL string.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		public static void execNonQuery(string sql, string path, ref string userMessage, params SQLiteParameter[] parameters)
		{
			execNonQuery(sql, path, ref userMessage, (IEnumerable<SQLiteParameter>)parameters);
		}

		/// <summary>Executes a nonquery SQL string.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		public static void execNonQuery(string sql, string path, ref string userMessage, IEnumerable<SQLiteParameter> parameters)
		{
#if !DEBUG
			// This runs asynchronously since the code that called it doesn't need anything in return.
			// Any db calls while the thread is running will be blocked.

			waitForThread();

			thread = new Thread(new ParameterizedThreadStart(execNonQueryThread));

			List<object> p = new List<object>()
				{
					sql,
					path,
					parameters
				};

			// start the thread
			thread.Start(p);
#else
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
				userMessage += ex.Message;
			}
#endif
		}

		/// <summary>The method to run for asynchronous execNonQuery calls.</summary>
		/// <param name="pm">The parameters.</param>
		private static void execNonQueryThread(object pm)
		{
			List<object> list = (List<object>)pm;
			string sql = (string)list[0];
			string path = (string)list[1];
			IEnumerable<SQLiteParameter> parameters = (IEnumerable<SQLiteParameter>)list[2];

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
				// TODO: find a way to report these errors
			}
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameter">A parameter used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
		/// <returns>The requested fields.</returns>
		public static string[] execReadFields(string sql, string path, ref string userMessage, SQLiteParameter parameter, params string[] fieldName)
		{
			return execReadFields(sql, path, ref userMessage, new SQLiteParameter[] { parameter }, fieldName);
		}

		/// <summary>Executes an SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
		/// <returns>The requested fields.</returns>
		public static string[] execReadFields(string sql, string path, ref string userMessage, IEnumerable<SQLiteParameter> parameters, params string[] fieldName)
		{
			string[] result = null;

#if !DEBUG
			waitForThread();
#endif
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
								userMessage += "Field `" + fieldName + "` was empty.";
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}

			return result;
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameter">A parameter used by the SQL string.</param>
		/// <param name="result">The return value.</param>
		/// <param name="fieldName">The name of the field to return.</param>
		/// <returns>The requested field.</returns>
		public static string execReadField(string sql, string path, ref string userMessage, SQLiteParameter parameter, string fieldName)
		{
			return execReadField(sql, path, ref userMessage, new SQLiteParameter[] { parameter }, fieldName);
		}

		/// <summary>Executes an SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return value.</param>
		/// <param name="fieldName">The name of the field to return.</param>
		/// <returns>The requested field.</returns>
		public static string execReadField(string sql, string path, ref string userMessage, IEnumerable<SQLiteParameter> parameters, string fieldName)
		{
			string result = null;

#if !DEBUG
			waitForThread();
#endif
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
								userMessage += "Field `" + fieldName + "` was empty.";
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				userMessage += ex.Message;
			}

			return result;
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameter">A parameter used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
		/// <returns>The requested list of fields.</returns>
		public static List<string[]> execReadListFields(string sql, string path, ref string userMessage, SQLiteParameter parameter, params string[] fieldName)
		{
			return execReadListFields(sql, path, ref userMessage, (parameter == null ? null : new SQLiteParameter[] { parameter }), fieldName);
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The names of the fields to return.</param>
		/// <returns>The requested list of fields.</returns>
		public static List<string[]> execReadListFields(string sql, string path, ref string userMessage, IEnumerable<SQLiteParameter> parameters, params string[] fieldName)
		{
			List<string[]> result = null;

#if !DEBUG
			waitForThread();
#endif
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
				userMessage += ex.Message;
				result = null;
			}

			return result;
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameter">A parameter used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The name of the field to return.</param>
		/// <returns>The requested list.</returns>
		public static List<string> execReadListField(string sql, string path, ref string userMessage, SQLiteParameter parameter, string fieldName)
		{
			return execReadListField(sql, path, ref userMessage, (parameter == null ? null : new SQLiteParameter[] { parameter }), fieldName);
		}

		/// <summary>Executes a SQL string and returns the requested value.</summary>
		/// <param name="sql">The SQL to execute.</param>
		/// <param name="path">The path of the database to access.</param>
		/// <param name="userMessage">Any user messages.</param>
		/// <param name="parameters">Any parameters used by the SQL string.</param>
		/// <param name="result">The return values.</param>
		/// <param name="fieldName">The name of the field to return.</param>
		/// <returns>The requested list.</returns>
		public static List<string> execReadListField(string sql, string path, ref string userMessage, IEnumerable<SQLiteParameter> parameters, string fieldName)
		{
			List<string> result = null;

#if !DEBUG
			waitForThread();
#endif
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
				userMessage += ex.Message;
				result = null;
			}

			return result;
		}

		/// <summary>Cleans up the database.</summary>
		/// <param name="userMessage">Any user messages.</param>
		public static void vacuum(ref string userMessage)
		{
			string sql = "VACUUM;";

#if !DEBUG
			waitForThread();
#endif
			try
			{
				using (SQLiteConnection con = new SQLiteConnection(genConnectionString(DBPath)))
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
				userMessage += ex.Message;
			}
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

		/// <summary>Converts a color int to the individual color values.</summary>
		/// <param name="color">An integer representing a color.</param>
		/// <param name="red">The red value.</param>
		/// <param name="green">The green value.</param>
		/// <param name="blue">The blue value.</param>
		public static void getColorsFromInt(int color, out byte red, out byte green, out byte blue)
		{
			red = (byte)(color / 65536);
			green = (byte)((color - (red * 65536)) / 256);
			blue = (byte)(color - (red * 65536) - (green * 256));
		}

		#endregion General Tools

		#endregion Methods
	}
}
