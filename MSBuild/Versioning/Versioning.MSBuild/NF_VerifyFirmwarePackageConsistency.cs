// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using nanoFramework.Targeting.Tooling;

namespace nanoFramework.Versioning.MSBuild
{
    public sealed class NF_VerifyFirmwarePackageConsistency : NF_TaskBase
    {
        #region Task parameters
        /// <summary>
        /// Gets or sets the full path to the project directory.
        /// Corresponds to the MSBuild $(MSBuildProjectDirectory) property.
        /// </summary>
        [Required]
        public string ProjectDirectory { get; set; } = null!;

        /// <summary>
        /// Gets or sets the path to the directory where cache files are stored.
        /// It is an absolute path, or relative to the <see cref="ProjectDirectory"/>.
        /// Typically $(IntermediateOutputPath)nF is passed.
        /// </summary>
        [Required]
        public string CacheFilesDirectory { get; set; } = null!;

        /// <summary>
        /// Gets or sets the path to the assembly that is being built.
        /// It is an absolute path, or relative to the <see cref="ProjectDirectory"/>.
        /// Pass "$(OutputPath)$(AssemblyName).pe" as value.
        /// </summary>
        [Required]
        public string AssemblyFilePath { get; set; } = null!;

        /// <summary>
        /// Gets or sets the assemblies that are referenced as dependency.
        /// Pass "@(ReferencePath)" as value.
        /// </summary>
        [Required]
        public ITaskItem[] ReferencedAssemblies { get; set; } = [];
        #endregion

        #region Task implementation
        /// <summary>
        /// Method to be called in unit tests
        /// </summary>
        /// <param name="logger">Logger to pass information to MSBuild</param>
        public override void Execute(LogMessenger logger)
        {
            #region Configuration
            var configuration = NanoDevicesConfiguration.Read(ProjectDirectory);
            if (configuration is null)
            {
                return;
            }
            var cacheDirectoryPath = Path.IsPathRooted(CacheFilesDirectory)
                ? CacheFilesDirectory
                : Path.Combine(ProjectDirectory, CacheFilesDirectory);
            #endregion

            #region Get the deployment targets; read from cache if possible
            var deploymentTargets = new DeploymentTargets(configuration, logger);
            if (!deploymentTargets.HasDeploymentTargets)
            {
                return;
            }

            string targetsCacheFile = Path.Combine(cacheDirectoryPath, "DeploymentTargets.json");
            string implementationCacheFile = Path.Combine(cacheDirectoryPath, "Implementations.json");
            IReadOnlyList<ImplementedNativeAssemblyVersion>? implemented;

            if (deploymentTargets.AreDeploymentTargetsEqual(targetsCacheFile))
            {
                implemented = DeploymentTargets.ReadImplementedNativeAssemblyMetadata(implementationCacheFile);
            }
            else
            {
                deploymentTargets.SaveDeploymentTargets(targetsCacheFile);
                implemented = deploymentTargets.GetImplementedNativeAssemblyMetadata(implementationCacheFile, logger)
                                    .GetAwaiter().GetResult();
            }
            if (implemented is null)
            {
                return;
            }
            #endregion

            #region Get the assembly metadata;readfrom cache if possible
            string requirementsCacheFile = Path.Combine(cacheDirectoryPath, "Requirements.json");

            var assemblyList = (from r in ReferencedAssemblies
                                select r.ItemSpec).ToList();
            assemblyList.Add(Path.IsPathRooted(AssemblyFilePath)
                ? AssemblyFilePath
                : Path.Combine(ProjectDirectory, AssemblyFilePath));

            var required = AssemblyMetadata.GetNanoFrameworkAssemblies(assemblyList, requirementsCacheFile, logger);
            #endregion

            #region Verify whether the requirements are satisfied
            _ = DeploymentTargets.CanDeploy(required, implemented, logger);
            #endregion
        }
        #endregion
    }
}
