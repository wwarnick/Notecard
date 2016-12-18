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

			Assert.AreEqual<string>(errorMessage, string.Empty, "Error getting Notecard version: " + errorMessage);
			Assert.IsFalse(string.IsNullOrWhiteSpace(version), "version should be filled.");
		}

		[TestMethod]
		public void createNewFile()
		{
			string errorMessage = string.Empty;

			CardManager.createNewFile(ref errorMessage);

			Assert.AreEqual<string>(errorMessage, string.Empty, "Error creating new file: " + errorMessage);

			CardManager.execReadField("SELECT 1 AS `test` FROM `global_settings` LIMIT 1;", CardManager.DBPath, ref errorMessage, (IEnumerable<SQLiteParameter>)null, "test");

			Assert.AreEqual<string>(errorMessage, string.Empty, "Error reading from file: " + errorMessage);
		}
	}
}
