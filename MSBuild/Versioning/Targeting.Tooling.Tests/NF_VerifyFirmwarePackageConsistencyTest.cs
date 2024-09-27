// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET472
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using nanoFramework.Targeting.Tooling;
using nanoFramework.Versioning.MSBuild;
using Targeting.Tooling.Tests.Helpers;

namespace Targeting.Tooling.Tests
{
    [TestClass]
    [TestCategory("MSBuild")]
    [TestCategory("Native assemblies")]
    public sealed class NF_VerifyFirmwarePackageConsistencyTest : TestClassBase
    {
        [TestMethod]
        public void NF_VerifyFirmwarePackageConsistency()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string dllFile1 = TestDirectoryHelper.CopyEmbeddedResource(typeof(AssemblyMetadataTest), "mscorlib.dll", testDirectory, "bin/mscorlib.dll");
            string peFile1 = TestDirectoryHelper.CopyEmbeddedResource(typeof(AssemblyMetadataTest), "mscorlib.pe", testDirectory, "bin/mscorlib.pe");
            string dllFile2 = TestDirectoryHelper.CopyFile(GetType().Assembly.Location, testDirectory, "bin/library.dll");
            string peFile2 = TestDirectoryHelper.CopyFile(GetType().Assembly.Location, testDirectory, "bin/library.pe");
            string firmwareDirectory = Path.Combine(testDirectory, "Firmware");
            TestDirectoryHelper.CopyEmbeddedResource(typeof(DeploymentTargetsTest), "ESP32_S3_ALL-1.12.0.53.zip.json", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(typeof(DeploymentTargetsTest), "ESP32_S3_ALL-1.12.0.53.zip", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip");
            TestDirectoryHelper.CopyEmbeddedResource(typeof(DeploymentTargetsTest), "ESP32_S3_BLE-1.12.0.118.zip.json", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(typeof(DeploymentTargetsTest), "ESP32_S3_BLE-1.12.0.118.zip", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip");
            string configurationFile = Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName);
            File.WriteAllText(configurationFile, """
{
    "FirmwareArchivePath": "Firmware",
    "DeviceTypeTargets": {
        "Primary device": ["ESP32_S3_ALL", "ESP32_S3_BLE"]
    },
    "DeviceTypes": [
        "Primary device",
        "Virtual nanoDevice"
    ]
}
""");
            var configuration = NanoDevicesConfiguration.Read(testDirectory);
            if (configuration.DeviceTypes.Count == 0)
            {
                Assert.Inconclusive("Configuration cannot be read.");
                return;
            }
            var logger = new LogMessengerMock();
            var cacheDirectory = Path.Combine(testDirectory, "obj");
            #endregion

            new NF_VerifyFirmwarePackageConsistency()
            {
                AssemblyFilePath = peFile2,
                CacheFilesDirectory = cacheDirectory,
                ProjectDirectory = testDirectory,
                ReferencedAssemblies = [new ITaskItemHelper(dllFile1)]
            }
            .Execute(logger!);

            Assert.IsTrue((from m in logger.Messages
                           where m.level == LoggingLevel.Error &&
                                 m.message == "Assembly 'mscorlib.dll' requires native 'mscorlib' version '100.5.0.19+445C7AF9' but version '100.5.0.18+445C7AF8' is implemented by 'ESP32_S3_BLE'."
                           select m).Any());

            Assert.IsTrue(Directory.Exists(cacheDirectory));
        }
    }
}
#endif
