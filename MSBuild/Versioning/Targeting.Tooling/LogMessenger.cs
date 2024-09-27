// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.Targeting.Tooling
{
    /// <summary>
    /// The log level.
    /// </summary>
    public enum LoggingLevel
    {
        None = 0,

        Detailed = 1,

        Verbose = 2,

        Warning = 3,

        Error = 4
    }

    /// <summary>
    /// Method used to pass a log message to the caller
    /// </summary>
    /// <param name="level">Level at which to log the message</param>
    /// <param name="message">Message to log</param>
    public delegate void LogMessenger(LoggingLevel level, string message);
}
