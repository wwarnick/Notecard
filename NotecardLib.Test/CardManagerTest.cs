using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotecardLib;
using System.Collections.Generic;
using System.Data.SQLite;

namespace NotecardLib.Test
{
	[TestClass]
	public class CardManagerTest
	{
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

			CardManager.execReadField("SELECT 1 AS `test` FROM `global_settings` LIMIT 1;", CardManager.DBPath, ref errorMessage, (IEnumerable<SQLiteParameter>)null, "test");

			Assert.AreEqual(errorMessage, string.Empty, "Error reading from file: " + errorMessage);
		}

		[TestMethod]
		public void updateDbVersion()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);
			CardManager.updateDbVersion(ref errorMessage);

			Assert.AreEqual(errorMessage, string.Empty, "Error updating DB version: " + errorMessage);
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
	}
}
