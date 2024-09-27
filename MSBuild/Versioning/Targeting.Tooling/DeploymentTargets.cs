// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace nanoFramework.Targeting.Tooling
{
    /// <summary>
    /// A collection of the device types a project should be deployable to.
    /// </summary>
    public sealed class DeploymentTargets
    {
        #region Fields
        private readonly Data _data = new();
        private readonly NanoDevicesConfiguration _configuration;
        private NanoCLRHelper? _nanoCLR = null;
        #endregion

        #region Construction
        /// <summary>
        /// Extract the deployment targets from the configuration and the packages available in the firmware archive.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="logger">Logger to pass process information to the caller.</param>
        public DeploymentTargets(NanoDevicesConfiguration configuration, LogMessenger? logger)
        {
            _configuration = configuration;
            if (configuration.DeviceTypes.Contains(NanoDevicesConfiguration.VirtualDeviceName))
            {
                #region nanoclr.exe / nanoFramework.nanoCLR.dll
                string filePath;
                if (configuration.PathToLocalCLRInstanceDirectory is not null)
                {
                    _data.PathToCLRInstance = filePath = Path.Combine(configuration.PathToLocalCLRInstanceDirectory, "nanoFramework.nanoCLR.dll");
                }
                else if (configuration.PathToLocalNanoCLR is not null)
                {
                    _data.PathToCLRInstance = filePath = configuration.PathToLocalNanoCLR;
                }
                else
                {
                    filePath = NanoCLRHelper.GlobalNanoCLRFilePath;
                    if (!File.Exists(filePath))
                    {
                        _nanoCLR = new(_configuration, logger);
                    }
                }
                if (File.Exists(filePath))
                {
                    _data.CLRInstanceLastModified = File.GetLastWriteTimeUtc(filePath);
                }
                #endregion
            }

            if (configuration.Platforms.Count > 0
                || (from dt in configuration.DeviceTypes
                    where dt != NanoDevicesConfiguration.VirtualDeviceName
                    select dt).Any())
            {
                #region Explicitly specified target names
                var targets = new Dictionary<string, (Version? version, string? packagePath)>();

                foreach (string deviceTypeName in configuration.DeviceTypes)
                {
                    if (deviceTypeName == NanoDevicesConfiguration.VirtualDeviceName)
                    {
                        continue;
                    }

                    IReadOnlyList<string> deviceTypeTargets = configuration.DeviceTypeTargets(deviceTypeName);
                    if (deviceTypeTargets.Count == 0)
                    {
                        logger?.Invoke(LoggingLevel.Warning, $"No target names provided for '{deviceTypeName}' in '{nameof(configuration.DeviceTypes)}' as read from the '{NanoDevicesConfiguration.ConfigurationFileName}' files.");
                    }
                    foreach (string target in deviceTypeTargets)
                    {
                        targets[target] = (null, null);
                    }
                }
                #endregion

                if (configuration.FirmwareArchivePath is null)
                {
                    logger?.Invoke(LoggingLevel.Error, $"No value provided for '{nameof(configuration.FirmwareArchivePath)}' in any of the '{NanoDevicesConfiguration.ConfigurationFileName}' files.");
                }
                else if (!Directory.Exists(configuration.FirmwareArchivePath))
                {
                    logger?.Invoke(LoggingLevel.Error, $"Directory '{configuration.FirmwareArchivePath}' not found; read as '{nameof(configuration.FirmwareArchivePath)}' from the '{NanoDevicesConfiguration.ConfigurationFileName}' files.");
                }
                else
                {
                    #region Find latest package for targets and platforms
                    foreach (string infoFilePath in Directory.EnumerateFiles(configuration.FirmwareArchivePath, "*.json"))
                    {
                        if (configuration.Platforms.Count > 0 ||
                            (from t in targets.Keys
                             where Path.GetFileName(infoFilePath).StartsWith($"{t}-")
                             select t).Any())
                        {
                            PackageInfo? info = JsonConvert.DeserializeObject<PackageInfo>(File.ReadAllText(infoFilePath));
                            if (info is null || info.Version is null || info.Name is null)
                            {
                                continue;
                            }

                            if (targets.TryGetValue(info.Name, out (Version? version, string? packagePath) targetInfo) ||
                                (info.Platform is not null && (from p in configuration.Platforms
                                                               where info.Platform.Equals(p, StringComparison.OrdinalIgnoreCase)
                                                               select p).Any()))
                            {
                                var thisVersion = new Version(info.Version);
                                if (targetInfo.version is null || thisVersion > targetInfo.version)
                                {
                                    targets[info.Name] = (thisVersion, Path.ChangeExtension(infoFilePath, null));
                                }
                            }
                        }
                    }

                    foreach (KeyValuePair<string, (Version version, string packagePath)> target in from t in targets
                                                                                                   orderby t.Key
                                                                                                   select t)
                    {
                        if (target.Value.packagePath is null || !File.Exists(target.Value.packagePath))
                        {
                            logger?.Invoke(LoggingLevel.Error, $"No firmware package found for target '{target.Key}' (read from the '{NanoDevicesConfiguration.ConfigurationFileName}' files).");
                        }
                        else
                        {
                            _data.TargetVersions.Add(target.Key, target.Value.packagePath);
                        }
                    }
                    #endregion
                }
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Indicates whether there are deployment targets specified
        /// </summary>
        public bool HasDeploymentTargets
            => _data.CLRInstanceLastModified.HasValue || _data.TargetVersions.Count > 0;
        #endregion

        #region Methods
        /// <summary>
        /// Save the deployment targets to a file
        /// </summary>
        /// <param name="filePath">Path to the file to save the data to.</param>
        public void SaveDeploymentTargets(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(_data));
        }

        /// <summary>
        /// Compare these deployment targets with the targets previously saved with <see cref="SaveDeploymentTargets(string)"/>.
        /// </summary>
        /// <param name="filePath">Path to the file the deployment data were previously saved in.</param>
        /// <returns>Returns <c>true</c> if the deployment targets are exactly the same, <c>false</c> otherwise.</returns>
        public bool AreDeploymentTargetsEqual(string filePath)
        {
            Data? data = File.Exists(filePath) ? JsonConvert.DeserializeObject<Data>(File.ReadAllText(filePath)) : null;
            if (data is null)
            {
                return _data.PathToCLRInstance is null && _data.TargetVersions.Count == 0;
            }
            if (data.PathToCLRInstance != _data.PathToCLRInstance
                || data.CLRInstanceLastModified != _data.CLRInstanceLastModified
                || data.TargetVersions.Count != _data.TargetVersions.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, string> target in _data.TargetVersions)
            {
                if (!data.TargetVersions.TryGetValue(target.Key, out string? path)
                    || path != target.Value)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get the native assembly metadata implemented by the deployment targets.
        /// </summary>
        /// <param name="saveInFilePath">File to save the native assembly metadata in.</param>
        /// <param name="logger">Logger to pass process information to the caller.</param>
        /// <returns>The required native assembly metadata. The result can contain multiple versions for the same assembly.</returns>
        public async Task<IReadOnlyList<ImplementedNativeAssemblyVersion>?> GetImplementedNativeAssemblyMetadata(string? saveInFilePath, LogMessenger? logger)
        {
            #region Collect requirements per name/version/checksum
            var mergedMetadata = new Dictionary<string, NativeAssemblyMetadataCollection.Metadata>();

            void AddMetadata(string targetName, NativeAssemblyMetadata metadata)
            {
                string key = $"{metadata.AssemblyName}\n{metadata.Version}\n{metadata.Checksum}";
                if (!mergedMetadata.TryGetValue(key, out NativeAssemblyMetadataCollection.Metadata? current))
                {
                    mergedMetadata[key] = current = new NativeAssemblyMetadataCollection.Metadata()
                    {
                        Name = metadata.AssemblyName,
                        Version = metadata.Version,
                        Checksum = metadata.Checksum,
                    };
                }
                current.TargetNames.Add(targetName);
            }
            #endregion

            #region Virtual Device
            if (_configuration.DeviceTypes.Contains(NanoDevicesConfiguration.VirtualDeviceName))
            {
                _nanoCLR ??= new(_configuration, logger);

                IReadOnlyList<NativeAssemblyMetadata>? assemblies = await _nanoCLR.GetNativeAssemblyMetadataAsync(logger);
                if (assemblies is null)
                {
                    logger?.Invoke(LoggingLevel.Warning, $"No native assembly metadata available for the {NanoDevicesConfiguration.VirtualDeviceName}");
                }
                else
                {
                    foreach (NativeAssemblyMetadata a in assemblies)
                    {
                        AddMetadata(NanoDevicesConfiguration.VirtualDeviceName, a);
                    }
                }
            }
            #endregion

            bool isConsistent = true;
            foreach (KeyValuePair<string, string> target in _data.TargetVersions)
            {
                #region Get the metadata for the target
                List<NativeAssemblyMetadata>? metadata = FirmwarePackage.GetNativeAssemblyMetadata(target.Value, logger);
                if (metadata is null)
                {
                    logger?.Invoke(LoggingLevel.Warning, $"No native assembly metadata available for devices with firmware '{target.Key}'");
                }
                else
                {
                    foreach (NativeAssemblyMetadata assembly in metadata)
                    {
                        AddMetadata(target.Key, assembly);
                    }
                }
                #endregion
            }
            if (!isConsistent)
            {
                return null;
            }

            #region Return and save results
            var result = (from t in mergedMetadata
                          orderby t.Key
                          select new ImplementedNativeAssemblyVersion(t.Value.Name, t.Value.Version, t.Value.Checksum, t.Value.TargetNames)
                          ).ToList();

            if (!string.IsNullOrWhiteSpace(saveInFilePath))
            {
                var data = new NativeAssemblyMetadataCollection()
                {
                    Collection = [.. mergedMetadata.Values]
                };
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(saveInFilePath))!);
                File.WriteAllText(saveInFilePath, JsonConvert.SerializeObject(data));
            }

            return result;
            #endregion
        }

        /// <summary>
        /// Read the implemented native assembly metadata from a file previously saved by <see cref="GetNativeAssemblyMetadata"/>.
        /// </summary>
        /// <param name="filePath">The path to the file with saved native assembly metadata.</param>
        /// <returns>The required native assembly metadata. The result can contain multiple versions for the same assembly.</returns>
        public static IReadOnlyList<ImplementedNativeAssemblyVersion> ReadImplementedNativeAssemblyMetadata(string filePath)
        {
            var result = new List<ImplementedNativeAssemblyVersion>();

            NativeAssemblyMetadataCollection? data = File.Exists(filePath)
                ? JsonConvert.DeserializeObject<NativeAssemblyMetadataCollection>(File.ReadAllText(filePath))
                : null;

            if (data is not null)
            {
                foreach (NativeAssemblyMetadataCollection.Metadata metadata in data.Collection)
                {
                    result.Add(new ImplementedNativeAssemblyVersion(metadata.Name, metadata.Version, metadata.Checksum, metadata.TargetNames));
                }
            }

            return result;
        }

        /// <summary>
        /// Check whether the set of assemblies can be deployed on all of the specified runtimes.
        /// </summary>
        /// <param name="assemblies">The set of assemblies to deploy.</param>
        /// <param name="runtimes">The native assembly implementations made available by the runtimes the assemblies should be deployed on.</param>
        /// <param name="logger">Logger that reports the mismatch between required and implemented native assemblies.</param>
        /// <returns>Returns <c>true</c> if the assemblies can be deployed (based on the native assembly versions), <c>false</c>
        /// if there is at least one runtime the assemblies cannot be deployed to.</returns>
        public static bool CanDeploy(IEnumerable<AssemblyMetadata> assemblies, IReadOnlyList<ImplementedNativeAssemblyVersion> runtimes, LogMessenger? logger)
        {
            return CanDeploy(
                from a in assemblies
                where a.NativeAssembly is not null
                select (Path.GetFileName(a.AssemblyFilePath ?? a.NanoFrameworkAssemblyFilePath), a.NativeAssembly),
                runtimes,
                logger);
        }

        /// <summary>
        /// Check whether the set of assemblies can be deployed on all of the specified runtimes.
        /// </summary>
        /// <param name="assemblies">The native assembly implementations required by the set of assemblies.</param>
        /// <param name="runtimes">The native assembly implementations made available by the runtimes the assemblies should be deployed on.</param>
        /// <param name="logger">Logger that reports the mismatch between required and implemented native assemblies.</param>
        /// <returns>Returns <c>true</c> if the assemblies can be deployed (based on the native assembly versions), <c>false</c>
        /// if there is at least one runtime the assemblies cannot be deployed to.</returns>
        public static bool CanDeploy(IEnumerable<(string assemblyName, NativeAssemblyMetadata required)> assemblies, IEnumerable<ImplementedNativeAssemblyVersion> runtimes, LogMessenger? logger)
        {
            bool canDeploy = true;

            var allTargets = new HashSet<string>();
            foreach (ImplementedNativeAssemblyVersion runtime in runtimes)
            {
                allTargets.UnionWith(runtime.TargetNames);
            }

            foreach ((string assemblyName, NativeAssemblyMetadata required) in assemblies)
            {
                var targetsWithImplementation = new HashSet<string>(allTargets);
                foreach (ImplementedNativeAssemblyVersion implemented in runtimes)
                {
                    if (implemented.AssemblyName == required.AssemblyName)
                    {
                        if (implemented.Version != required.Version || implemented.Checksum != required.Checksum)
                        {
                            canDeploy = false;
                            logger?.Invoke(LoggingLevel.Error, $"Assembly '{assemblyName}' requires native '{required.AssemblyName}' version '{required.Version}+{required.Checksum:X}' but version '{implemented.Version}+{implemented.Checksum:X}' is implemented by {string.Join(", ", from t in implemented.TargetNames
                                                                                                                                                                                                                                                                                          orderby t
                                                                                                                                                                                                                                                                                          select $"'{t}'")}.");
                        }
                        targetsWithImplementation.ExceptWith(implemented.TargetNames);
                    }
                }

                if (targetsWithImplementation.Count > 0)
                {
                    canDeploy = false;
                    logger?.Invoke(LoggingLevel.Error, $"Assembly '{assemblyName}' requires native '{required.AssemblyName}' that is not implemented by {string.Join(", ", from t in targetsWithImplementation
                                                                                                                                                                           orderby t
                                                                                                                                                                           select $"'{t}'")}.");
                }
            }

            return canDeploy;
        }
        #endregion

        #region Auxiliary classes for JSON serialization
        private sealed class Data
        {
            public string? PathToCLRInstance { get; set; }
            public DateTime? CLRInstanceLastModified { get; set; }
            public Dictionary<string, string> TargetVersions { get; set; } = [];
        }

        private sealed class PackageInfo
        {
            public string? Platform { get; set; }
            public string? Name { get; set; }
            public string? Version { get; set; }
        }

        private sealed class NativeAssemblyMetadataCollection
        {
            public List<Metadata> Collection
            {
                get; set;
            } = [];

            internal sealed class Metadata
            {
                public string Name { get; set; } = null!;
                public string Version { get; set; } = null!;
                public uint Checksum { get; set; }
                public List<string> TargetNames { get; set; } = [];
            }
        }
        #endregion
    }
}
