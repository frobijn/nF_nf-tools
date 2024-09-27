// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Newtonsoft.Json;

namespace nanoFramework.Targeting.Tooling
{
    /// <summary>
    /// Some information about a nanoFramework assembly.
    /// </summary>
    public sealed class AssemblyMetadata
    {
        #region Construction
        /// <summary>
        /// Get the information about the nanoFramework assembly.
        /// </summary>
        /// <param name="assemblyFilePath">The path to the assembly. Can be the path to a *.pe, *.dll or *.exe file.
        /// if it is the path to a *.pe file, the *.dll/*.exe file should reside in the same directory.</param>
        public AssemblyMetadata(string assemblyFilePath)
        {
            AssemblyFilePath = assemblyFilePath;
            NanoFrameworkAssemblyFilePath = assemblyFilePath;
            if (Path.GetExtension(assemblyFilePath).ToLower() == ".pe")
            {
                string tryPath = Path.ChangeExtension(assemblyFilePath, ".exe");
                if (File.Exists(tryPath))
                {
                    AssemblyFilePath = tryPath;
                }
                else
                {
                    tryPath = Path.ChangeExtension(assemblyFilePath, ".dll");
                    if (File.Exists(tryPath))
                    {
                        AssemblyFilePath = tryPath;
                    }
                }
            }
            else
            {
                NanoFrameworkAssemblyFilePath = Path.ChangeExtension(assemblyFilePath, ".pe");
            }

            uint? nativeChecksum = null;
            if (File.Exists(NanoFrameworkAssemblyFilePath))
            {
                using (FileStream fs = File.Open(NanoFrameworkAssemblyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // read the PE checksum from the byte array at position 0x14
                    byte[] buffer = new byte[4];
                    fs.Position = 0x14;
                    fs.Read(buffer, 0, 4);
                    uint nativeMethodsChecksum = BitConverter.ToUInt32(buffer, 0);

                    if (nativeMethodsChecksum != 0)
                    {
                        // PEs with native methods checksum equal to 0 DO NOT require native support 
                        // OK to move to the next one
                        nativeChecksum = nativeMethodsChecksum;
                    }
                }
            }

            if (File.Exists(AssemblyFilePath))
            {
                var deCompiler = new CSharpDecompiler(AssemblyFilePath, new DecompilerSettings
                {
                    LoadInMemory = false,
                    ThrowOnAssemblyResolveErrors = false
                });
                string assemblyProperties = deCompiler.DecompileModuleAndAssemblyAttributesToString();

                // AssemblyVersion
                string pattern = @"(?<=AssemblyVersion\("")(.*)(?=\""\)])";
                MatchCollection match = Regex.Matches(assemblyProperties, pattern, RegexOptions.IgnoreCase);
                Version = match[0].Value;

                // AssemblyNativeVersion
                pattern = @"(?<=AssemblyNativeVersion\("")(.*)(?=\""\)])";
                match = Regex.Matches(assemblyProperties, pattern, RegexOptions.IgnoreCase);

                // only class libs have this attribute, therefore sanity check is required
                if (match.Count == 1)
                {
                    if (nativeChecksum is not null)
                    {
                        NativeAssembly = new NativeAssemblyMetadata(Path.GetFileNameWithoutExtension(AssemblyFilePath), match[0].Value, nativeChecksum.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Re-create the metadata from cache
        /// </summary>
        private AssemblyMetadata(string nanoAssemblyFilePath, string assemblyFilePath, CachedAssemblies.Metadata metadata)
        {
            AssemblyFilePath = assemblyFilePath;
            NanoFrameworkAssemblyFilePath = nanoAssemblyFilePath;
            Version = metadata.Version;
            if (metadata.NativeAssembly is not null)
            {
                NativeAssembly = new NativeAssemblyMetadata(metadata.NativeAssembly, metadata.NativeVersion!, metadata.Checksum!.Value);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the path to the EXE or DLL file.
        /// </summary>
        public string AssemblyFilePath
        {
            get;
        }

        /// <summary>
        /// Gets the path to the PE file.
        /// </summary>
        public string NanoFrameworkAssemblyFilePath
        {
            get;
        }

        /// <summary>
        /// Gets the assembly version of the EXE or DLL. Is <c>null</c> if the assembly does not exist.
        /// </summary>
        public string? Version
        {
            get;
        }

        /// <summary>
        /// Gets the native assembly/implementation that should be part of the firmware/CLR instance for
        /// this .NET assembly to function properly.
        /// Is <c>null</c> if the .NET assembly does not require a native implementation.
        /// </summary>
        public NativeAssemblyMetadata? NativeAssembly
        {
            get;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Get the metadata for the .NET nanoFramework assemblies in a directory (without recursion).
        /// </summary>
        /// <param name="directoryPath">Path to the directory where the assemblies are located.</param>
        /// <returns>An enumeration of .NET nanoFramework assemblies. The enumeration is empty if the directory does not exist.</returns>
        public static List<AssemblyMetadata> GetNanoFrameworkAssemblies(string directoryPath)
        {
            return GetNanoFrameworkAssemblies(directoryPath, null!, null);
        }

        /// <summary>
        /// Get the metadata for the .NET nanoFramework assemblies in a directory (without recursion).
        /// The metadata is cached in a file, so the next time this method is called only the assemblies
        /// that have been changed have ot be re-examined for metadata.
        /// </summary>
        /// <param name="directoryPath">Path to the directory where the assemblies are located.</param>
        /// <param name="cacheFilePath">Path to the cache file.</param>
        /// <param name="logger">Logger to pass process information to the caller.</param>
        /// <returns>An enumeration of .NET nanoFramework assemblies. The enumeration is empty if the directory does not exist.</returns>
        /// <remarks>
        /// If the cache file cannot be created, read or updated that is logged, but it will not cause an error or exception.
        /// </remarks>
        public static List<AssemblyMetadata> GetNanoFrameworkAssemblies(string directoryPath, string cacheFilePath, LogMessenger? logger)
        {
            return GetNanoFrameworkAssemblies(
                Directory.Exists(directoryPath)
                    ? Directory.EnumerateFiles(directoryPath, "*.pe")
                    : [],
                cacheFilePath,
                logger);
        }

        /// <summary>
        /// Get the metadata for the .NET nanoFramework assemblies in a directory (without recursion).
        /// The metadata is cached in a file, so the next time this method is called only the assemblies
        /// that have been changed have ot be re-examined for metadata.
        /// </summary>
        /// <param name="assemblyFilePaths">Enumeration of the paths to each assembly.</param>
        /// <param name="cacheFilePath">Path to the cache file.</param>
        /// <param name="logger">Logger to pass process information to the caller.</param>
        /// <returns>An enumeration of .NET nanoFramework assemblies. The enumeration is empty if the directory does not exist.</returns>
        /// <remarks>
        /// If the cache file cannot be created, read or updated that is logged, but it will not cause an error or exception.
        /// </remarks>
        public static List<AssemblyMetadata> GetNanoFrameworkAssemblies(IEnumerable<string> assemblyFilePaths, string cacheFilePath, LogMessenger? logger)
        {
            var cachedAssemblies = new CachedAssemblies();
            if (!string.IsNullOrWhiteSpace(cacheFilePath) && File.Exists(cacheFilePath))
            {
                try
                {
                    cachedAssemblies = JsonConvert.DeserializeObject<CachedAssemblies>(File.ReadAllText(cacheFilePath));
                }
                catch (Exception ex)
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"Cannot read the cached assembly metadata file '{cacheFilePath}': {ex.Message}");
                }
            }

            var assemblies = new CachedAssemblies();
            var result = new List<AssemblyMetadata>();
            foreach (string nanoAssemblyFilePath in assemblyFilePaths)
            {
                string assemblyFilePath;
                if (nanoAssemblyFilePath.EndsWith(".pe"))
                {
                    assemblyFilePath = Path.ChangeExtension(nanoAssemblyFilePath, ".exe");
                    if (!File.Exists(assemblyFilePath))
                    {
                        assemblyFilePath = Path.ChangeExtension(nanoAssemblyFilePath, ".dll");
                    }
                }
                else if (nanoAssemblyFilePath.EndsWith(".exe") || nanoAssemblyFilePath.EndsWith(".dll"))
                {
                    assemblyFilePath = nanoAssemblyFilePath;
                }
                else
                {
                    continue;
                }

                if (!File.Exists(assemblyFilePath))
                {
                    // Must be an orphaned file 
                    continue;
                }

                string fileName = Path.GetFileName(assemblyFilePath);
                DateTime lastModified = File.GetLastWriteTimeUtc(assemblyFilePath);
                if (cachedAssemblies!.Assemblies.TryGetValue(fileName, out CachedAssemblies.Metadata? cachedMetadata)
                    && cachedMetadata.LastModified == lastModified)
                {
                    // Keep the metadata
                    assemblies.Assemblies[fileName] = cachedMetadata;
                    result.Add(new AssemblyMetadata(nanoAssemblyFilePath, assemblyFilePath, cachedMetadata));
                }
                else
                {
                    // Get the metadata from the assembly
                    var metadata = new AssemblyMetadata(assemblyFilePath);
                    result.Add(metadata);
                    assemblies.Assemblies[fileName] = new()
                    {
                        LastModified = lastModified,
                        Version = metadata.Version,
                        NativeAssembly = metadata.NativeAssembly?.AssemblyName,
                        NativeVersion = metadata.NativeAssembly?.Version,
                        Checksum = metadata.NativeAssembly?.Checksum
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(cacheFilePath))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cacheFilePath))!);
                    File.WriteAllText(cacheFilePath, JsonConvert.SerializeObject(assemblies));
                }
                catch (Exception ex)
                {
                    logger?.Invoke(LoggingLevel.Verbose, $"Cannot write the cached assembly metadata file '{cacheFilePath}': {ex.Message}");
                }
            }

            return result;
        }
        #endregion

        #region Auxiliary class for JSON serialization
        private sealed class CachedAssemblies
        {
            public Dictionary<string, Metadata> Assemblies { get; set; } = [];

            public sealed class Metadata
            {
                public DateTime? LastModified { get; set; }
                public string? Version { get; set; }
                public string? NativeAssembly { get; set; }
                public string? NativeVersion { get; set; }
                public uint? Checksum { get; set; }
            }
        }
        #endregion
    }
}
