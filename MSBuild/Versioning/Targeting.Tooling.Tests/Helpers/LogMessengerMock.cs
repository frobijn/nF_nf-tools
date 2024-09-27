// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using nanoFramework.Targeting.Tooling;

namespace Targeting.Tooling.Tests.Helpers
{
    /// <summary>
    /// Mock to pass as logger argument
    /// </summary>
    public sealed class LogMessengerMock
    {
        #region Conversion
        /// <summary>
        /// Conversion of the mock to the argument type
        /// </summary>
        /// <param name="mock"></param>
        public static implicit operator LogMessenger?(LogMessengerMock mock)
        {
            if (mock is null)
            {
                return null;
            }
            else
            {
                return (level, message) =>
                        mock._messages.Add((level, message));
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the logged messages
        /// </summary>
        public IReadOnlyList<(LoggingLevel level, string message)> Messages
            => _messages;
        private readonly List<(LoggingLevel level, string message)> _messages = [];
        #endregion

        #region Helpers
        /// <summary>
        /// Get all messages as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Join("\n",
                        from m in Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n';
        }

        /// <summary>
        /// Assert the messages
        /// </summary>
        /// <param name="expectedMessages">Expected messages in the format of <see cref="ToString"/></param>
        /// <param name="minimalLevel">Minimal level of messages to include</param>
        public void AssertEqual(string expectedMessages, LoggingLevel minimalLevel = LoggingLevel.Detailed)
        {
            Assert.AreEqual(
               (expectedMessages?.Trim() ?? "").Replace("\r\n", "\n") + '\n',
               string.Join("\n",
                       from m in Messages
                       where m.level >= minimalLevel
                       select $"{m.level}: {m.message}"
                   ) + '\n'
           );
        }
        #endregion


    }
}
