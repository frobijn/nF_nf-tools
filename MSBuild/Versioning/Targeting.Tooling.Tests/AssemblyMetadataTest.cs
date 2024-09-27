// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nanoFramework.Targeting.Tooling;
using Targeting.Tooling.Tests.Helpers;

namespace Targeting.Tooling.Tests
{
    [TestClass]
    [TestCategory("Native assemblies")]
    public sealed class AssemblyMetadataTest : TestClassBase
    {
        [TestMethod]
        public void AssemblyMetadata_NanoFrameworkAssembly()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string dllFile = TestDirectoryHelper.CopyEmbeddedResource(GetType(), "mscorlib.dll", testDirectory, "mscorlib.dll");
            string peFile = TestDirectoryHelper.CopyEmbeddedResource(GetType(), "mscorlib.pe", testDirectory, "mscorlib.pe");
            string expectedAssemblyName = "mscorlib";
            string expectedVersion = "1.15.6.0";
            string expectedNativeVersion = "100.5.0.19";
            uint expectedChecksum = 0x445C7AF9;
            #endregion

            #region From .pe file
            var actual = new AssemblyMetadata(peFile);

            Assert.AreEqual(peFile, actual.NanoFrameworkAssemblyFilePath);
            Assert.AreEqual(dllFile, actual.AssemblyFilePath);
            Assert.AreEqual(expectedVersion, actual.Version);
            Assert.AreEqual(expectedAssemblyName, actual.NativeAssembly?.AssemblyName);
            Assert.AreEqual(expectedChecksum, actual.NativeAssembly?.Checksum);
            Assert.AreEqual(expectedNativeVersion, actual.NativeAssembly?.Version);
            #endregion

            #region From .dll file
            actual = new AssemblyMetadata(dllFile);

            Assert.AreEqual(peFile, actual.NanoFrameworkAssemblyFilePath);
            Assert.AreEqual(dllFile, actual.AssemblyFilePath);
            Assert.AreEqual(expectedAssemblyName, actual.NativeAssembly?.AssemblyName);
            Assert.AreEqual(expectedChecksum, actual.NativeAssembly?.Checksum);
            Assert.AreEqual(expectedNativeVersion, actual.NativeAssembly?.Version);
            #endregion
        }

        [TestMethod]
        public void AssemblyMetadata_DotNetAssembly()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string dllFile = TestDirectoryHelper.CopyFile(GetType().Assembly.Location, testDirectory, "library.dll");
            string peFile = TestDirectoryHelper.CopyFile(GetType().Assembly.Location, testDirectory, "library.pe");
            #endregion

            #region From pe file
            var actual = new AssemblyMetadata(peFile);

            Assert.AreEqual(peFile, actual.NanoFrameworkAssemblyFilePath);
            Assert.AreEqual(dllFile, actual.AssemblyFilePath);
            Assert.IsNotNull(actual.Version);
            Assert.IsNull(actual.NativeAssembly);
            #endregion

            #region From .dll file
            actual = new AssemblyMetadata(dllFile);

            Assert.AreEqual(peFile, actual.NanoFrameworkAssemblyFilePath);
            Assert.AreEqual(dllFile, actual.AssemblyFilePath);
            Assert.IsNotNull(actual.Version);
            Assert.IsNull(actual.NativeAssembly);
            #endregion
        }



        [TestMethod]
        public void AssemblyMetadata_GetNanoFrameworkAssemblies()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string dllFile1 = TestDirectoryHelper.CopyEmbeddedResource(GetType(), "mscorlib.dll", testDirectory, "mscorlib.dll");
            string peFile1 = TestDirectoryHelper.CopyEmbeddedResource(GetType(), "mscorlib.pe", testDirectory, "mscorlib.pe");
            string expectedAssemblyName1 = "mscorlib";
            string expectedNativeVersion1 = "100.5.0.19";
            uint expectedChecksum1 = 0x445C7AF9;
            string dllFile2 = TestDirectoryHelper.CopyFile(GetType().Assembly.Location, testDirectory, "application.exe");
            string peFile2 = TestDirectoryHelper.CopyFile(GetType().Assembly.Location, testDirectory, "application.pe");
            string cacheFilePath = Path.Combine(testDirectory, "obj", ".nF", "AssemblyMetadata.json");
            #endregion

            #region Assert
            void AssertMetadata(List<AssemblyMetadata> actual, string nonNativeAssembly)
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual
                (
                    $"{nonNativeAssembly};mscorlib.dll",
                    string.Join(";", from a in actual orderby a.AssemblyFilePath select Path.GetFileName(a.AssemblyFilePath))
                );
                Assert.AreEqual
                (
                    $";{expectedAssemblyName1}",
                    string.Join(";", from a in actual orderby a.AssemblyFilePath select a.NativeAssembly?.AssemblyName)
                );
                Assert.AreEqual
                (
                    $";{expectedNativeVersion1}",
                    string.Join(";", from a in actual orderby a.AssemblyFilePath select a.NativeAssembly?.Version)
                );
                Assert.AreEqual
                (
                    $";{expectedChecksum1}",
                    string.Join(";", from a in actual orderby a.AssemblyFilePath select a.NativeAssembly?.Checksum)
                );
            }
            #endregion

            #region Without cache
            List<AssemblyMetadata> actual = AssemblyMetadata.GetNanoFrameworkAssemblies(testDirectory);

            AssertMetadata(actual, "application.exe");
            #endregion

            #region Save to cache
            var logger = new LogMessengerMock();

            actual = AssemblyMetadata.GetNanoFrameworkAssemblies(testDirectory, cacheFilePath, logger);

            logger.AssertEqual("");
            AssertMetadata(actual, "application.exe");
            Assert.IsTrue(File.Exists(cacheFilePath));
            string actualJson = File.ReadAllText(cacheFilePath);
            Assert.IsTrue(actualJson.Contains("mscorlib.dll"));
            Assert.IsTrue(actualJson.Contains(expectedNativeVersion1));
            Assert.IsTrue(actualJson.Contains("application.exe"));
            #endregion

            #region Read from cache
            // Add a new assembly
            TestDirectoryHelper.CopyFile(GetType().Assembly.Location, testDirectory, "library.dll");
            TestDirectoryHelper.CopyFile(GetType().Assembly.Location, testDirectory, "library.pe");
            // Disguise application.* as mscorlib. The code should use the cached data, and that can
            // be tested because if it wouldn't the metadata would be wrong
            File.SetLastWriteTimeUtc(dllFile2, File.GetLastWriteTimeUtc(dllFile1));
            File.Delete(dllFile1);
            File.Delete(peFile1);
            File.Move(dllFile2, dllFile1);
            File.Move(peFile2, peFile1);

            logger = new LogMessengerMock();

            actual = AssemblyMetadata.GetNanoFrameworkAssemblies(testDirectory, cacheFilePath, logger);

            logger.AssertEqual("");
            AssertMetadata(actual, "library.dll");
            Assert.IsTrue(File.Exists(cacheFilePath));
            actualJson = File.ReadAllText(cacheFilePath);
            Assert.IsTrue(actualJson.Contains("mscorlib.dll"));
            Assert.IsTrue(actualJson.Contains(expectedNativeVersion1));
            Assert.IsTrue(actualJson.Contains("library.dll"));
            Assert.IsFalse(actualJson.Contains("application.exe"));
            #endregion

            #region Use cache, assembly updated
            // Touch the so-called mscorlib
            File.SetLastWriteTimeUtc(dllFile1, DateTime.UtcNow);
            logger = new LogMessengerMock();

            actual = AssemblyMetadata.GetNanoFrameworkAssemblies(testDirectory, cacheFilePath, logger);

            logger.AssertEqual("");
            Assert.AreEqual
            (
                "library.dll;mscorlib.dll",
                string.Join(";", from a in actual orderby a.AssemblyFilePath select Path.GetFileName(a.AssemblyFilePath))
            );
            Assert.AreEqual
            (
                ";",
                string.Join(";", from a in actual orderby a.AssemblyFilePath select a.NativeAssembly?.AssemblyName)
            );
            #endregion
        }
    }
}
