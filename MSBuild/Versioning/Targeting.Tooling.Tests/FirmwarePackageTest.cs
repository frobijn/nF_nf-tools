// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using nanoFramework.Targeting.Tooling;
using Targeting.Tooling.Tests.Helpers;

namespace Targeting.Tooling.Tests
{
    [TestClass]
    [TestCategory("Native assemblies")]
    public sealed class FirmwarePackageTest : TestClassBase
    {
        [TestMethod]
        public void FirmwarePackage_GetNativeAssemblyMetadata()
        {
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string zipNoMetadataFilePath = TestDirectoryHelper.CopyEmbeddedResource(GetType(), "NoMetadata.zip", testDirectory, "NoMetadata.zip");
            string zipWithMetadataFilePath = TestDirectoryHelper.CopyEmbeddedResource(GetType(), "WithMetadata.zip", testDirectory, "WithMetadata.zip");
            var logger = new LogMessengerMock();

            #region Package with list
            System.Collections.Generic.List<NativeAssemblyMetadata>? actual = FirmwarePackage.GetNativeAssemblyMetadata(zipWithMetadataFilePath, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            NativeAssemblyMetadata? mscorlib = (from m in actual
                                                where m.AssemblyName == "mscorlib"
                                                select m).FirstOrDefault();
            Assert.IsNotNull(mscorlib);
            Assert.AreEqual("100.5.0.19", mscorlib.Version);
            Assert.AreEqual((uint)0x445C7AF9, mscorlib.Checksum);
            Assert.AreEqual(26, actual.Count);
            #endregion

            #region Package without list
            logger = new LogMessengerMock();

            actual = FirmwarePackage.GetNativeAssemblyMetadata(zipNoMetadataFilePath, logger);

            logger.AssertEqual("");
            Assert.IsNull(actual);
            #endregion
        }
    }
}
