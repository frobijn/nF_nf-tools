// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Build.Framework;
using nanoFramework.Targeting.Tooling;

namespace nanoFramework.Versioning.MSBuild
{
    public sealed class NF_VerifyNuGetPackageVersion : NF_TaskBase
    {
        #region Task parameters
        /// <summary>
        /// Gets or sets the full path to the project file.
        /// Corresponds to the MSBuild $(MSBuildProjectFullPath) property.
        /// </summary>
        [Required]
        public string ProjectFilePath { get; set; } = null!;
        #endregion

        #region Task implementation
        /// <summary>
        /// Method to be called in unit tests
        /// </summary>
        /// <param name="logger">Logger to pass information to MSBuild</param>
        public override void Execute(LogMessenger logger)
        {
            var configuration = NanoDevicesConfiguration.Read(Path.GetDirectoryName(ProjectFilePath)!);
            if (configuration?.NuGetPackageList is null)
            {
                return;
            }

            var list = NuGetPackageList.Read(configuration.NuGetPackageList, logger);
            list?.Validate(ProjectFilePath, logger);
        }
        #endregion
    }
}
