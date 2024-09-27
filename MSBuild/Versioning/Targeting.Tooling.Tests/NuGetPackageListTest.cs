// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using nanoFramework.Targeting.Tooling;
using Targeting.Tooling.Tests.Helpers;

namespace Targeting.Tooling.Tests
{
    [TestClass]
    [TestCategory("NuGet packages")]
    public sealed class NuGetPackageListTest : TestClassBase
    {
        [TestMethod]
        [DataRow("Normal")]
        [DataRow("Detailed")]
        public void NuGetPackageList_Read(string verbosity)
        {
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), $"{verbosity}.txt", testDirectory, "list.txt");
            var logger = new LogMessengerMock();

            var actual = NuGetPackageList.Read(Path.Combine(testDirectory, "list.txt"), logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.IsNotNull(actual.PackageVersions);
            Assert.IsTrue(actual.PackageVersions.ContainsKey("nanoFramework.Logging"));
            Assert.AreEqual("1.1.108", actual.PackageVersions["nanoFramework.Logging"]);
            Assert.AreEqual(374, actual.PackageVersions.Count);
        }

        [TestMethod]
        public void NuGetPackageList_FileDoesNotExist()
        {
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            var logger = new LogMessengerMock();

            var actual = NuGetPackageList.Read(Path.Combine(testDirectory, "list.txt"), logger);

            logger.AssertEqual($"Error: NuGet package list file '{Path.Combine(testDirectory, "list.txt")}' does not exist");
            Assert.IsNull(actual);
        }



        [TestMethod]
        public void NuGetPackageList_Validate()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "Normal.txt", testDirectory, "list.txt");
            string projectFilePath = Path.Combine(testDirectory, "Project.nfproj");
            var actual = NuGetPackageList.Read(Path.Combine(testDirectory, "list.txt"), null);
            if (actual is null)
            {
                Assert.Inconclusive("NuGetPackageList could not be read");
                return;
            }
            #endregion

            #region No packages.config present
            var logger = new LogMessengerMock();

            actual.Validate(projectFilePath, logger!);

            logger.AssertEqual("", LoggingLevel.Error);
            #endregion

            #region packages.config present
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "packages.config", testDirectory, "packages.config");
            logger = new LogMessengerMock();

            actual.Validate(projectFilePath, logger!);

            logger.AssertEqual(
@"Error: The required version of package 'nanoFramework.Fire' is '1.1.238', but the project uses version '0.0.42'.", LoggingLevel.Error);
            #endregion
        }
    }
}
