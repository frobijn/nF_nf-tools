// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Build.Utilities;
using nanoFramework.Targeting.Tooling;

#if DEBUG
#if LAUNCHDEBUGGER
using System.Diagnostics;
#endif
#endif

namespace nanoFramework.Versioning.MSBuild
{
    public abstract class NF_TaskBase : Task
    {
        #region MSBuild task
        /// <summary>
        /// Method called by MSBuild
        /// </summary>
        /// <returns>Always returns <c>true</c>; the build will stop because errors are reported.
        /// If an exception occurs, the result is also <c>true</c> so that faulty code cannot block the build process.
        /// </returns>
        public override bool Execute()
        {
#if DEBUG
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
#endif
            void logger(LoggingLevel level, string message)
            {
                if (level == LoggingLevel.Error)
                {
                    Log.LogError(message);
                }
            }
            try
            {
                Execute(logger);
            }
            catch (Exception ex)
            {
                logger(LoggingLevel.Error, $"Task '{GetType().FullName}' encountered an unexpected error: {ex.Message}");
            }
            return true;
        }
        #endregion

        #region To be implemented by derived classes
        /// <summary>
        /// Implementation of the task. This method is also callable by unit tests.
        /// </summary>
        /// <param name="logger">Logger to pass information to MSBuild</param>
        public abstract void Execute(LogMessenger logger);
        #endregion
    }
}
