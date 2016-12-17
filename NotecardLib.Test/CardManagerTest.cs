using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotecardLib;

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

			Assert.AreEqual<string>(errorMessage, string.Empty, "errorMessage should be empty.");
			Assert.IsFalse(string.IsNullOrWhiteSpace(version), "version should be filled.");
		}
	}
}
