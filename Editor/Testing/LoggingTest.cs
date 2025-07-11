using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Microsoft.Unity.VisualStudio.Editor.Tests
{
	public class LoggingTest
	{
		[Test]
		public void OpenProjectFile_LogsConfirmationMessage()
		{
			// Arrange
			LogAssert.ignoreFailingMessages = true;

			// Act
			VisualStudioIntegration.OpenProjectFile();

			// Assert
			LogAssert.Expect(LogType.Log, "[Cursor Integration] Project files generated for Cursor.");

			LogAssert.ignoreFailingMessages = false;
		}
	}
} 