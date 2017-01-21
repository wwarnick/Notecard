using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace NotecardLib.Test
{
	[TestClass]
	public class CardManagerTest
	{
		#region File

		[TestMethod]
		public void getNoteCardVersion()
		{
			string errorMessage = string.Empty;
			string version = CardManager.getNoteCardVersion(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error getting Notecard version: " + errorMessage);
			Assert.IsFalse(string.IsNullOrWhiteSpace(version), "version should be filled.");
		}

		[TestMethod]
		public void getUpdateScripts()
		{
			string errorMessage = string.Empty;

			// change the version to 0
			string sql = "UPDATE `global_settings` SET `version` = 0;";
			CardManager.execNonQuery(sql, CardManager.DBPath, ref errorMessage);

			string scripts = CardManager.getUpdateScripts(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error getting update scripts: " + errorMessage);
			Assert.IsFalse(string.IsNullOrWhiteSpace(scripts), "No scripts were returned.");
		}

		[TestMethod]
		public void createNewFile()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error creating new file: " + errorMessage);

			string readString = CardManager.execReadField("SELECT 1 AS `test` FROM `global_settings` LIMIT 1;", CardManager.DBPath, ref errorMessage, (IEnumerable<SQLiteParameter>)null, "test");

			Assert.AreEqual(errorMessage, string.Empty, "Error reading from file: " + errorMessage);
			Assert.AreEqual(readString, "1", "Invalid return value: " + readString);

			// cleanup
			CardManager.clearCurrentDir(ref errorMessage);
		}

		[TestMethod]
		public void updateDbVersion()
		{
			// This test recreates the database as it was when the automated version updates
			// were first implemented, then attempts to update it to the current version.

			string errorMessage = string.Empty;

			#region Create old file

			try
			{
				SQLiteConnection.CreateFile(CardManager.DBPath);
			}
			catch (Exception ex)
			{
				errorMessage += "Could not create file at \"" + CardManager.DBPath + "\": " + ex.Message + "\n\n";
				return;
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

			CardManager.execNonQuery(sql, CardManager.DBPath, ref errorMessage, null);

			#endregion Create old file

			CardManager.updateDbVersion(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error updating DB version: " + errorMessage);

			// cleanup
			CardManager.clearCurrentDir(ref errorMessage);
		}

		[TestMethod]
		public void refreshOldLastModifiedDate()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			CardManager.refreshOldLastModifiedDate(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error refreshing OldLastModifiedDate: " + errorMessage);
		}

		[TestMethod]
		public void hasUnsavedChanges()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			bool unsaved = CardManager.hasUnsavedChanges(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error refreshing OldLastModifiedDate: " + errorMessage);
			Assert.IsFalse(unsaved, "There should be no unsaved changes.");

			string name;
			CardManager.addArrangement("test arrangement", out name, ref errorMessage);

			unsaved = CardManager.hasUnsavedChanges(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error refreshing OldLastModifiedDate: " + errorMessage);
			Assert.IsTrue(unsaved, "There should be unsaved changes.");
		}

		[TestMethod]
		public void createCurrentDir()
		{
			string errorMessage = string.Empty;

			CardManager.createCurrentDir(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error creating the current directory: " + errorMessage);
		}

		[TestMethod]
		public void clearCurrentDir()
		{
			string errorMessage = string.Empty;

			CardManager.clearCurrentDir(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error clearing the current directory: " + errorMessage);
		}

		[TestMethod]
		public void cleanOrphanedFiles()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			CardManager.cleanOrphanedFiles(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error cleaning orphaned files: " + errorMessage);
		}

		[TestMethod]
		public void getFileVersion()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			string version = CardManager.getFileVersion(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error getting file version: " + errorMessage);
			Assert.IsFalse(string.IsNullOrWhiteSpace(version), "version should be filled.");
		}

		[TestMethod]
		public void save()
		{
			string errorMessage = string.Empty;
			string path = Path.GetTempPath() + "temp.crd";

			CardManager.createNewFile(ref errorMessage);
			CardManager.save(path, ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error saving file: " + errorMessage);

			if (File.Exists(path))
				File.Delete(path);
		}

		[TestMethod]
		public void open()
		{
			string errorMessage = string.Empty;
			string path = Path.GetTempPath() + "temp.crd";

			CardManager.createNewFile(ref errorMessage);
			CardManager.save(path, ref errorMessage);
			CardManager.open(path, ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error opening file: " + errorMessage);

			if (File.Exists(path))
				File.Delete(path);
		}

		#endregion File

		#region Card Types

		[TestMethod]
		public void giveAllTypesTitleFields()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			CardManager.giveAllTypesTitleFields(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error giving all types title fields: " + errorMessage);
		}

		[TestMethod]
		public void refreshCardTypes()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			CardManager.refreshCardTypes(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error refreshing card types: " + errorMessage);
		}

		[TestMethod]
		public void newCardType()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			string id = null;

			CardTypeContext[] contexts = (CardTypeContext[])Enum.GetValues(typeof(CardTypeContext));
			foreach (CardTypeContext context in contexts)
			{
				id = CardManager.newCardType(context, ref errorMessage);
				Assert.AreEqual(errorMessage, string.Empty, "Error creating new " + context.ToString() + " card type: " + errorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(id), "id should be filled after creating a " + context.ToString() + " card type.");
			}
		}

		// I am planning to rewrite the way card types are saved, so I won't write the tests yet.
		/*[TestMethod]
		public void saveCardType()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			string id = null;

			CardTypeChange[] changes = (CardTypeChange[])Enum.GetValues(typeof(CardTypeChange));
			CardTypeContext[] contexts = (CardTypeContext[])Enum.GetValues(typeof(CardTypeContext));

			foreach (CardTypeContext context in contexts)
			{
				id = CardManager.newCardType(CardTypeContext.Standalone, ref errorMessage);

				foreach (CardTypeChange change in changes)
				{
					switch (change)
					{
						default:
							throw new Exception("Unknown CardTypeChange value: " + change.ToString());
					}
				}

				Assert.AreEqual(errorMessage, string.Empty, "Error creating new standalone card type: " + errorMessage);
				Assert.IsFalse(string.IsNullOrWhiteSpace(id), "id should be filled after creating a standalone card type.");
			}
		}*/

		[TestMethod]
		public void getCardType()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			string id = CardManager.newCardType(CardTypeContext.Standalone, ref errorMessage);
			CardType ct = CardManager.getCardType(id, ref errorMessage);
			Assert.AreEqual(errorMessage, string.Empty, "Error getting card type: " + errorMessage);
			Assert.IsNotNull(ct, "No card type was returned.");
		}

		#endregion Card Types
	}
}
