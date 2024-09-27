// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace nanoFramework.Targeting.Tooling
{
    /// <summary>
    /// In-memory representation of a file that lists the allowed versions of the NuGet packages to use
    /// in .NET naoFramework projects. The name of the package should be at the start of the line, followed by whitespace
    /// (optionally including an end of line) and the version of the package. The file format is consistent with the
    /// output of <c>nuget list</c>.
    /// </summary>
    public sealed class NuGetPackageList
    {
        #region Fields
        private readonly Dictionary<string, string> _packageVersions = [];
        #endregion

        #region Construction
        /// <summary>
        /// Read the package list.
        /// </summary>
        /// <param name="filePath">File containing the package list.</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        /// <returns>The package list, or <c>null</c> if the package list is not available.</returns>
        public static NuGetPackageList? Read(string filePath, LogMessenger? logger)
        {
            if (!File.Exists(filePath))
            {
                logger?.Invoke(LoggingLevel.Error, $"NuGet package list file '{filePath}' does not exist");
                return null;
            }

            var result = new NuGetPackageList();

            string content = File.ReadAllText(filePath);
            foreach (Match match in _parseListFile.Matches(content))
            {
                result._packageVersions[match.Groups["id"].Value] = match.Groups["version"].Value;
            }
            return result;
        }
        private static readonly Regex _parseListFile = new(@"^(?<id>[A-Z0-9_\-.]+)[\s\r\n]+(?<version>[0-9]+(\.[0-9]+){1,3})[\s\r\n]+", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private NuGetPackageList()
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// A dictionary with for each of the known packages (key) its required version (value)
        /// </summary>
        public IReadOnlyDictionary<string, string> PackageVersions
            => _packageVersions;
        #endregion

        #region Methods
        /// <summary>
        /// Validate that the project uses the correct version of the packages. If not, log an error.
        /// </summary>
        /// <param name="projectFilePath">Path to the project file.</param>
        /// <param name="logger">Method to pass errors to the caller.</param>
        public void Validate(string projectFilePath, LogMessenger logger)
        {
            string packagesFilePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(projectFilePath))!, "packages.config");

            if (!File.Exists(packagesFilePath))
            {
                logger.Invoke(LoggingLevel.Detailed, $"Cannot find project file '{packagesFilePath}'.");
                return;
            }
            try
            {
                var document = XDocument.Load(packagesFilePath);
                if (document?.Root is null)
                {
                    logger.Invoke(LoggingLevel.Detailed, $"Packages file '{packagesFilePath}' has no content.");
                    return;
                }
                foreach (XElement package in document!.Root!.Descendants(document.Root.Name.Namespace + "package"))
                {
                    if (package.Attribute("targetFramework")?.Value == "netnano1.0")
                    {
                        string packageId = package.Attribute("id")!.Value;
                        string? packageVersion = package.Attribute("version")?.Value;
                        if (_packageVersions.TryGetValue(packageId, out string? version))
                        {
                            if (packageVersion != version)
                            {
                                logger(LoggingLevel.Error, $"The required version of package '{packageId}' is '{version}', but the project uses version '{packageVersion}'.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Invoke(LoggingLevel.Error, $"Cannot parse packages file '{packagesFilePath}': {ex.Message}.");
            }
        }
        #endregion
    }
}
