// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace nanoFramework.Targeting.Tooling
{
    public static class FirmwarePackage
    {
        /// <summary>
        /// Get the metadata for the native assemblies that are implemented in the firmware/target.
        /// </summary>
        /// <param name="firmwarePackageFilePath">Path to the zip file (as downloaded from the online repository)
        /// that contains the firmware.</param>
        /// <param name="logger">Logger to pass process information to the caller.</param>
        /// <returns>A list of native assembly metadata, or <c>null</c> if no information on the native assemblies is available.s</returns>
        public static List<NativeAssemblyMetadata>? GetNativeAssemblyMetadata(string firmwarePackageFilePath, LogMessenger? logger)
        {
            List<NativeAssemblyMetadata>? result = null;
            try
            {
                ZipArchive zip = ZipFile.OpenRead(firmwarePackageFilePath);
                ZipArchiveEntry? nativeAssemblies = (from e in zip.Entries
                                                     where e.Name == "native_assemblies.csv"
                                                     select e).FirstOrDefault();
                if (nativeAssemblies is not null)
                {
                    result ??= [];
                    using (Stream stream = nativeAssemblies.Open())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream)
                            {
                                string? line = reader.ReadLine();
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    continue;
                                }
                                string[] parts = line.Split(',');
                                if (parts.Length == 3)
                                {
                                    uint checksum;
                                    if (parts[2].StartsWith("0x"))
                                    {
#pragma warning disable IDE0079 // Next supression cannot be omitted
#pragma warning disable CA1846 // Prefer 'AsSpan' over 'Substring' // Overload not available
                                        if (!uint.TryParse(parts[2].Substring(2), NumberStyles.HexNumber, null, out checksum))
                                        {
                                            continue;
                                        }
#pragma warning restore CA1846 // Prefer 'AsSpan' over 'Substring'
#pragma warning restore IDE0079
                                    }
                                    else
                                    {
                                        if (!uint.TryParse(parts[2], out checksum))
                                        {
                                            continue;
                                        }
                                    }
                                    result.Add(new NativeAssemblyMetadata(parts[0], parts[1], checksum));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke(LoggingLevel.Error, $"Cannot read firmware package '{firmwarePackageFilePath}': {ex.Message}");
                result = null;
            }
            return result;
        }
    }
}
