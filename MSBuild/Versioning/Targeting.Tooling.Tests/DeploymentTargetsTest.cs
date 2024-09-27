// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using nanoFramework.Targeting.Tooling;
using Targeting.Tooling.Tests.Helpers;

namespace Targeting.Tooling.Tests
{
    [TestClass]
    [TestCategory("Native assemblies")]
    public sealed class DeploymentTargetsTest : TestClassBase
    {
        #region Create / Save / Compare deployment targets
        [TestMethod]
        [DataRow(null, null)]
        [DataRow("nanoclr.exe", null)]
        [DataRow(null, "clr")]
        [DataRow("nanoclr.exe", "clr")]
        public void DeploymentTargets_Create_Compare_VirtualDeviceOnly(string? toolPath, string? instanceDirectory)
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            Directory.CreateDirectory(Path.Combine(testDirectory, "clr"));
            File.WriteAllText(Path.Combine(testDirectory, "nanoclr.exe"), "tool");
            File.WriteAllText(Path.Combine(testDirectory, "clr", "nanoFramework.nanoCLR.dll"), "runtime");
            string configurationFile = Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName);
            File.WriteAllText(configurationFile, $@"
{{
    ""PathToLocalNanoCLR"": ""{toolPath}"",
    ""PathToLocalCLRInstanceDirectory"": ""{instanceDirectory}"",
    ""DeviceTypes"": [""Virtual nanoDevice""]
}}
");
            var configuration = NanoDevicesConfiguration.Read(testDirectory);
            if (configuration.DeviceTypes.Count == 0)
            {
                Assert.Inconclusive("Configuration cannot be read.");
                return;
            }
            string deploymentTargetFilePath = Path.Combine(testDirectory, "obj", ".NF", "DeploymentTargets.json");
            #endregion

            #region Create the deployment targets
            var logger = new LogMessengerMock();

            var actual = new DeploymentTargets(configuration, logger);

            logger.AssertEqual("", LoggingLevel.Verbose);
            Assert.IsTrue(actual.HasDeploymentTargets);
            #endregion

            #region Save the deployment targets
            actual.SaveDeploymentTargets(deploymentTargetFilePath);

            Assert.IsTrue(File.Exists(deploymentTargetFilePath));
            string actualJson = File.ReadAllText(deploymentTargetFilePath);
            if (toolPath is null && instanceDirectory is null)
            {
                Assert.IsTrue(actualJson.Contains("PathToCLRInstance\":null"));
            }
            else
            {
                Assert.IsTrue(actualJson.Contains("PathToCLRInstance"));
                Assert.IsTrue(actualJson.Contains((instanceDirectory ?? toolPath)!));
            }
            Assert.IsTrue(actualJson.Contains("CLRInstanceLastModified"));
            #endregion

            #region AreDeploymentTargetsEqual
            Assert.IsTrue(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));

            File.WriteAllText(deploymentTargetFilePath, actualJson.Replace(DateTime.UtcNow.Year.ToString(), "2000"));
            Assert.IsFalse(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));

            if (toolPath is null && instanceDirectory is null)
            {
                File.WriteAllText(deploymentTargetFilePath, actualJson.Replace("null", $"\"other_{instanceDirectory ?? toolPath}\""));
            }
            else
            {
                File.WriteAllText(deploymentTargetFilePath, actualJson.Replace((instanceDirectory ?? toolPath)!, $"other_{instanceDirectory ?? toolPath}"));
            }
            Assert.IsFalse(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));
            #endregion
        }

        [TestMethod]
        public void DeploymentTargets_Create_Compare_SingleDeviceType()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string firmwareDirectory = Path.Combine(testDirectory, "Firmware");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.52.zip.json", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.52.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.52.zip", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.52.zip");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip.json", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_BLE-1.12.0.118.zip.json", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_BLE-1.12.0.118.zip", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip");
            string configurationFile = Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName);
            File.WriteAllText(configurationFile, """
{
    "FirmwareArchivePath": "Firmware",
    "DeviceTypeTargets": {
        "Primary device": "ESP32_S3_ALL"
    },
    "DeviceTypes": [
        "Primary device"
    ]
}
""");
            var configuration = NanoDevicesConfiguration.Read(testDirectory);
            if (configuration.DeviceTypes.Count == 0)
            {
                Assert.Inconclusive("Configuration cannot be read.");
                return;
            }
            string deploymentTargetFilePath = Path.Combine(testDirectory, "obj", ".NF", "DeploymentTargets.json");
            #endregion

            #region Create the deployment targets
            var logger = new LogMessengerMock();

            var actual = new DeploymentTargets(configuration, logger);

            logger.AssertEqual("", LoggingLevel.Verbose);
            Assert.IsTrue(actual.HasDeploymentTargets);
            #endregion

            #region Save the deployment targets
            actual.SaveDeploymentTargets(deploymentTargetFilePath);

            Assert.IsTrue(File.Exists(deploymentTargetFilePath));
            string actualJson = File.ReadAllText(deploymentTargetFilePath);
            Assert.IsTrue(actualJson.Contains("\"ESP32_S3_ALL\""));
            Assert.IsTrue(actualJson.Replace("\\\\", "/").Contains($"Firmware/ESP32_S3_ALL-1.12.0.53.zip"));
            #endregion

            #region AreDeploymentTargetsEqual
            Assert.IsTrue(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));

            File.WriteAllText(deploymentTargetFilePath, actualJson.Replace("\"ESP32_S3_ALL\"", "\"ESP32_S3_BLE\""));
            Assert.IsFalse(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));

            File.WriteAllText(deploymentTargetFilePath, actualJson.Replace(".zip", ".999.zip"));
            Assert.IsFalse(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));
            #endregion
        }

        [TestMethod]
        public void DeploymentTargets_Create_Compare_Platform()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string firmwareDirectory = Path.Combine(testDirectory, "Firmware");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip.json", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_BLE-1.12.0.118.zip.json", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_BLE-1.12.0.118.zip", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip");
            string configurationFile = Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName);
            File.WriteAllText(configurationFile, """
{
    "FirmwareArchivePath": "Firmware",
    "Platforms": [
        "ESP32"
    ]
}
""");
            var configuration = NanoDevicesConfiguration.Read(testDirectory);
            if (configuration.Platforms.Count == 0)
            {
                Assert.Inconclusive("Configuration cannot be read.");
                return;
            }
            string deploymentTargetFilePath = Path.Combine(testDirectory, "obj", ".NF", "DeploymentTargets.json");
            #endregion

            #region Create the deployment targets
            var logger = new LogMessengerMock();

            var actual = new DeploymentTargets(configuration, logger);

            logger.AssertEqual("", LoggingLevel.Verbose);
            Assert.IsTrue(actual.HasDeploymentTargets);
            #endregion

            #region Save the deployment targets
            actual.SaveDeploymentTargets(deploymentTargetFilePath);

            Assert.IsTrue(File.Exists(deploymentTargetFilePath));
            string actualJson = File.ReadAllText(deploymentTargetFilePath);
            Assert.IsTrue(actualJson.Contains("\"ESP32_S3_ALL\""));
            Assert.IsTrue(actualJson.Replace("\\\\", "/").Contains($"Firmware/ESP32_S3_ALL-1.12.0.53.zip"));
            Assert.IsTrue(actualJson.Contains("\"ESP32_S3_BLE\""));
            Assert.IsTrue(actualJson.Replace("\\\\", "/").Contains($"Firmware/ESP32_S3_BLE-1.12.0.118.zip"));
            #endregion

            #region AreDeploymentTargetsEqual
            Assert.IsTrue(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));

            File.WriteAllText(deploymentTargetFilePath, actualJson.Replace("\"ESP32_S3_ALL\"", "\"ESP32_S3\""));
            Assert.IsFalse(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));

            File.WriteAllText(deploymentTargetFilePath, actualJson.Replace("BLE-1", "BLE-2"));
            Assert.IsFalse(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));
            #endregion
        }


        [TestMethod]
        public void DeploymentTargets_Create_Compare_All()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string firmwareDirectory = Path.Combine(testDirectory, "Firmware");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip.json", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_BLE-1.12.0.118.zip.json", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_BLE-1.12.0.118.zip", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip");
            string configurationFile = Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName);
            File.WriteAllText(configurationFile, """
{
    "FirmwareArchivePath": "Firmware",
    "DeviceTypeTargets": {
        "Primary device": ["ESP32_S3_ALL", "ESP32_S3_BLE"],
        "Additional devices": "ESP32_S3"
    },
    "DeviceTypes": [
        "Primary device",
        "Additional devices",
        "Test devices",
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
            string deploymentTargetFilePath = Path.Combine(testDirectory, "obj", ".NF", "DeploymentTargets.json");
            #endregion

            #region Create the deployment targets
            var logger = new LogMessengerMock();

            var actual = new DeploymentTargets(configuration, logger);

            logger.AssertEqual(
$@"Warning: No target names provided for 'Test devices' in 'DeviceTypes' as read from the '{NanoDevicesConfiguration.ConfigurationFileName}' files.
Error: No firmware package found for target 'ESP32_S3' (read from the '{NanoDevicesConfiguration.ConfigurationFileName}' files).", LoggingLevel.Verbose);
            Assert.IsTrue(actual.HasDeploymentTargets);
            #endregion

            #region Save the deployment targets
            actual.SaveDeploymentTargets(deploymentTargetFilePath);

            Assert.IsTrue(File.Exists(deploymentTargetFilePath));
            string actualJson = File.ReadAllText(deploymentTargetFilePath);
            Assert.IsTrue(actualJson.Contains("\"ESP32_S3_ALL\""));
            Assert.IsTrue(actualJson.Replace("\\\\", "/").Contains($"Firmware/ESP32_S3_ALL-1.12.0.53.zip"));
            Assert.IsTrue(actualJson.Contains("\"ESP32_S3_BLE\""));
            Assert.IsTrue(actualJson.Replace("\\\\", "/").Contains($"Firmware/ESP32_S3_BLE-1.12.0.118.zip"));
            Assert.IsFalse(actualJson.Contains("\"ESP32_S3\""));
            Assert.IsTrue(actualJson.Contains(DateTime.UtcNow.Year.ToString()));
            #endregion

            #region AreDeploymentTargetsEqual
            Assert.IsTrue(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));

            File.WriteAllText(deploymentTargetFilePath, actualJson.Replace("\"ESP32_S3_ALL\"", "\"ESP32_S3\""));
            Assert.IsFalse(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));

            File.WriteAllText(deploymentTargetFilePath, actualJson.Replace("BLE-1", "BLE-2"));
            Assert.IsFalse(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));

            File.WriteAllText(deploymentTargetFilePath, actualJson.Replace(DateTime.UtcNow.Year.ToString(), "2000"));
            Assert.IsFalse(actual.AreDeploymentTargetsEqual(deploymentTargetFilePath));
            #endregion
        }

        [TestMethod]
        public void DeploymentTargets_Create_NoFirmware()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string firmwareDirectory = Path.Combine(testDirectory, "Firmware");
            string configurationFile = Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName);

            File.WriteAllText(configurationFile, """
{
    "FirmwareArchivePath": "Firmware",
    "DeviceTypeTargets": {
        "Primary device": "ESP32_S3_ALL"
    },
    "DeviceTypes": [
        "Primary device"
    ]
}
""");
            var configuration = NanoDevicesConfiguration.Read(testDirectory);
            if (configuration.DeviceTypes.Count == 0)
            {
                Assert.Inconclusive("Configuration cannot be read.");
                return;
            }
            #endregion

            #region Create - Firmware directory does not exist
            var logger = new LogMessengerMock();

            var actual = new DeploymentTargets(configuration, logger);

            logger.AssertEqual($"Error: Directory '{firmwareDirectory}' not found; read as 'FirmwareArchivePath' from the '{NanoDevicesConfiguration.ConfigurationFileName}' files.", LoggingLevel.Verbose);
            Assert.IsFalse(actual.HasDeploymentTargets);
            #endregion

            #region Firmware directory not specified
            File.WriteAllText(configurationFile, """
{
    "DeviceTypeTargets": {
        "Primary device": "ESP32_S3_ALL"
    },
    "DeviceTypes": [
        "Primary device"
    ]
}
""");
            configuration = NanoDevicesConfiguration.Read(testDirectory);
            logger = new LogMessengerMock();

            actual = new DeploymentTargets(configuration, logger);

            logger.AssertEqual($"Error: No value provided for 'FirmwareArchivePath' in any of the '{NanoDevicesConfiguration.ConfigurationFileName}' files.", LoggingLevel.Verbose);
            Assert.IsFalse(actual.HasDeploymentTargets);
            #endregion

            #region No device types specified
            File.WriteAllText(configurationFile, """
{
}
""");
            configuration = NanoDevicesConfiguration.Read(testDirectory);
            logger = new LogMessengerMock();

            actual = new DeploymentTargets(configuration, logger);

            logger.AssertEqual("", LoggingLevel.Verbose);
            Assert.IsFalse(actual.HasDeploymentTargets);
            #endregion
        }
        #endregion

        #region NativeAssemblyMetadata
        [TestMethod]
        public void DeploymentTargets_NativeAssemblyMetadata_SingleDeviceType()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string firmwareDirectory = Path.Combine(testDirectory, "Firmware");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip.json", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip");
            string configurationFile = Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName);
            File.WriteAllText(configurationFile, """
{
    "FirmwareArchivePath": "Firmware",
    "DeviceTypeTargets": {
        "Primary device": "ESP32_S3_ALL"
    },
    "DeviceTypes": [
        "Primary device"
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

            var deploymentTargets = new DeploymentTargets(configuration, logger);

            logger.AssertEqual("", LoggingLevel.Verbose);

            string metadataFilePath = Path.Combine(testDirectory, "metadata.json");
            #endregion

            #region Assert
            void AssertList(IReadOnlyList<ImplementedNativeAssemblyVersion>? actual)
            {
                Assert.IsNotNull(actual);

                ImplementedNativeAssemblyVersion? mscorlib = (from m in actual
                                                              where m.AssemblyName == "mscorlib"
                                                              select m).FirstOrDefault();
                Assert.IsNotNull(mscorlib);
                Assert.AreEqual("100.5.0.19", mscorlib.Version);
                Assert.AreEqual((uint)0x445C7AF9, mscorlib.Checksum);
                Assert.AreEqual("ESP32_S3_ALL", string.Join(";", from t in mscorlib.TargetNames orderby t select t));

                Assert.AreEqual(26, actual.Count);
            }
            #endregion

            #region Test
            foreach (string? saveToFile in new string?[] { null, metadataFilePath })
            {
                Debug.WriteLine($"Save to file: {saveToFile is not null}");
                logger = new LogMessengerMock();

                // Get the assembly info
                IReadOnlyList<ImplementedNativeAssemblyVersion>? actual =
                    deploymentTargets.GetImplementedNativeAssemblyMetadata(saveToFile, logger)
                        .GetAwaiter().GetResult();

                logger.AssertEqual("");
                AssertList(actual);
            }
            Assert.IsTrue(File.Exists(metadataFilePath));

            // Read the saved results
            IReadOnlyList<ImplementedNativeAssemblyVersion>? read = DeploymentTargets.ReadImplementedNativeAssemblyMetadata(metadataFilePath);
            AssertList(read);
            #endregion
        }

        [TestMethod]
        public void DeploymentTargets_NativeAssemblyMetadata_AllDeviceTypes()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string firmwareDirectory = Path.Combine(testDirectory, "Firmware");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip.json", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_ALL-1.12.0.53.zip", testDirectory, "Firmware/ESP32_S3_ALL-1.12.0.53.zip");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_BLE-1.12.0.118.zip.json", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip.json");
            TestDirectoryHelper.CopyEmbeddedResource(GetType(), "ESP32_S3_BLE-1.12.0.118.zip", testDirectory, "Firmware/ESP32_S3_BLE-1.12.0.118.zip");
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

            var deploymentTargets = new DeploymentTargets(configuration, logger);

            logger.AssertEqual("", LoggingLevel.Verbose);

            string metadataFilePath = Path.Combine(testDirectory, "metadata.json");
            #endregion

            #region Assert
            void AssertList(IReadOnlyList<ImplementedNativeAssemblyVersion>? actual)
            {
                Assert.IsNotNull(actual);

                var allTargets = new List<string>();
                foreach (ImplementedNativeAssemblyVersion esp32 in from m in actual
                                                                   where m.AssemblyName == "nanoFramework.Hardware.Esp32"
                                                                   select m)
                {
                    allTargets.AddRange(esp32.TargetNames);
                    Assert.AreEqual("100.0.10.0", esp32.Version);
                    Assert.AreEqual((uint)0x6A20A689, esp32.Checksum);
                }
                Assert.AreEqual("ESP32_S3_ALL;ESP32_S3_BLE", string.Join(";", from t in allTargets orderby t select t));

                allTargets = new List<string>();
                foreach (ImplementedNativeAssemblyVersion mscorlib in from m in actual
                                                                      where m.AssemblyName == "mscorlib"
                                                                      select m)
                {
                    allTargets.AddRange(mscorlib.TargetNames);
                    if (mscorlib.TargetNames.Contains("ESP32_S3_ALL"))
                    {
                        Assert.AreEqual("100.5.0.19", mscorlib.Version);
                        Assert.AreEqual((uint)0x445C7AF9, mscorlib.Checksum);
                    }
                    if (mscorlib.TargetNames.Contains("ESP32_S3_BLE"))
                    {
                        Assert.AreEqual("100.5.0.18", mscorlib.Version);
                        Assert.AreEqual((uint)0x445C7AF8, mscorlib.Checksum);
                    }
                }
                Assert.AreEqual("ESP32_S3_ALL;ESP32_S3_BLE;Virtual nanoDevice", string.Join(";", from t in allTargets orderby t select t));
            }
            #endregion

            #region Test
            logger = new LogMessengerMock();

            // Get the assembly info
            IReadOnlyList<ImplementedNativeAssemblyVersion>? actual =
                deploymentTargets.GetImplementedNativeAssemblyMetadata(metadataFilePath, logger)
                    .GetAwaiter().GetResult();

            logger.AssertEqual("", LoggingLevel.Warning);
            AssertList(actual);
            Assert.IsTrue(File.Exists(metadataFilePath));

            // Read the saved results
            IReadOnlyList<ImplementedNativeAssemblyVersion>? read = DeploymentTargets.ReadImplementedNativeAssemblyMetadata(metadataFilePath);
            AssertList(read);
            #endregion
        }
        #endregion

        #region CanDeploy
        [TestMethod]
        public void DeploymentTargets_CanDeploy()
        {
            #region All good
            var logger = new LogMessengerMock();
            bool actual = DeploymentTargets.CanDeploy(
                [
                    ("Library", new ("mscorlib", "1.0.0", 0x1234)),
                    ("Application", new ("mscorlib", "1.0.0", 0x1234))
                ],
                [
                    new("mscorlib", "1.0.0", 0x1234, ["ESP32_S3_ALL", "ESP32_S3_BLE", "Virtual nanoDevice"]),
                ],
                logger);

            Assert.IsTrue(actual);
            logger.AssertEqual("");
            #endregion

            #region Some mismatches
            logger = new LogMessengerMock();
            actual = DeploymentTargets.CanDeploy(
                [
                    ("Library", new ("mscorlib", "1.0.0", 0x1234)),
                    ("Library", new ("System.Math", "1.2.3", 0x5678)),
                    ("Library", new ("nanoFramework.Hardware.Esp32", "4.5.6.7", 0x9ABC)),
                    ("Library", new ("Not.Implemented.Anywhere", "8.9.0.1", 0xDEF0)),
                    ("Application", new ("mscorlib", "1.0.0", 0x1234)),
                    ("Application", new ("System.Device.WiFi", "3.6.9", 0x369C))
                ],
                [
                    new("mscorlib", "1.0.0", 0x1234, ["ESP32_S3_ALL", "ESP32_S3_BLE", "Virtual nanoDevice"]),
                    new("System.Math", "1.2.1", 0x5678, ["ESP32_S3_ALL"]),
                    new("System.Math", "1.2.3", 0x5677, ["ESP32_S3_BLE"]),
                    new("System.Math", "1.2.3", 0x5678, ["Virtual nanoDevice"]),
                    new("System.Device.WiFi", "9.6.3", 0x369C, ["ESP32_S3_ALL", "ESP32_S3_BLE"]),
                    new("System.Device.WiFi", "3.6.9", 0x369C, ["Virtual nanoDevice"]),
                    new("nanoFramework.Hardware.Esp32", "4.5.6.7", 0x9ABC, ["ESP32_S3_ALL", "ESP32_S3_BLE"]),
                    new("System.Runtime.Serialization", "1.10.100", 0x1234, ["ESP32_S3_ALL", "ESP32_S3_BLE", "Virtual nanoDevice"]),
                ],
                logger);

            Assert.IsFalse(actual);
            logger.AssertEqual(
@"Error: Assembly 'Library' requires native 'System.Math' version '1.2.3+5678' but version '1.2.1+5678' is implemented by 'ESP32_S3_ALL'.
Error: Assembly 'Library' requires native 'System.Math' version '1.2.3+5678' but version '1.2.3+5677' is implemented by 'ESP32_S3_BLE'.
Error: Assembly 'Library' requires native 'nanoFramework.Hardware.Esp32' that is not implemented by 'Virtual nanoDevice'.
Error: Assembly 'Library' requires native 'Not.Implemented.Anywhere' that is not implemented by 'ESP32_S3_ALL', 'ESP32_S3_BLE', 'Virtual nanoDevice'.
Error: Assembly 'Application' requires native 'System.Device.WiFi' version '3.6.9+369C' but version '9.6.3+369C' is implemented by 'ESP32_S3_ALL', 'ESP32_S3_BLE'.");
            #endregion
        }
        #endregion
    }
}
