// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET472
using System.IO;
using nanoFramework.Targeting.Tooling;
using nanoFramework.Versioning.MSBuild;
using Targeting.Tooling.Tests.Helpers;

namespace Targeting.Tooling.Tests
{
    [TestClass]
    [TestCategory("MSBuild")]
    [TestCategory("NuGet packages")]
    public sealed class NF_VerifyNuGetPackageVersionTest : TestClassBase
    {
        [TestMethod]
        public void NF_VerifyNuGetPackageVersion()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            TestDirectoryHelper.CopyEmbeddedResource(typeof(NuGetPackageListTest), "Normal.txt", testDirectory, "list.txt");
            TestDirectoryHelper.CopyEmbeddedResource(typeof(NuGetPackageListTest), "packages.config", testDirectory, "packages.config");
            File.WriteAllText(Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName), """
{
    "NuGetPackageList": "list.txt"
}
""");
            var configuration = NanoDevicesConfiguration.Read(testDirectory);
            if (configuration?.NuGetPackageList is null)
            {
                Assert.Inconclusive("Configuration cannot be read.");
                return;
            }
            #endregion

            var logger = new LogMessengerMock();

            new NF_VerifyNuGetPackageVersion()
            {
                ProjectFilePath = Path.Combine(testDirectory, "project.nfproj")
            }
            .Execute(logger!);

            logger.AssertEqual(@"Error: The required version of package 'nanoFramework.Fire' is '1.1.238', but the project uses version '0.0.42'.", LoggingLevel.Warning);
        }
    }
}
#endif
