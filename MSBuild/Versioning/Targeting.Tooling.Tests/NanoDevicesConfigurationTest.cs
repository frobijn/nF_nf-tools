// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using nanoFramework.Targeting.Tooling;
using Targeting.Tooling.Tests.Helpers;

namespace Targeting.Tooling.Tests
{
    [TestClass]
    [TestCategory("NuGet packages")]
    [TestCategory("Native assemblies")]
    [TestCategory("NanoCLR")]
    [TestCategory("nanoDevices")]
    public sealed class NanoDevicesConfigurationTest : TestClassBase
    {
        [TestMethod]
        public void NanoDevicesConfiguration_Single()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            NanoDevicesConfiguration.UserProfileDirectoryPath = Path.Combine(testDirectory, "UserProfile");
            #endregion

            #region Configuration does not exist
            var actual = NanoDevicesConfiguration.Read(testDirectory);

            Assert.IsNotNull(actual);
            Assert.AreEqual("", string.Join(";", from d in actual.DeviceTypeNames orderby d select d));
            Assert.AreEqual("", string.Join(";", from d in actual.DeviceTypes orderby d select d));
            Assert.AreEqual("", string.Join(";", from t in actual.DeviceTypeTargets("invalid") orderby t select t));
            Assert.IsNull(actual.FirmwareArchivePath);
            Assert.IsNull(actual.PathToLocalCLRInstanceDirectory);
            Assert.IsNull(actual.PathToLocalNanoCLR);
            Assert.AreEqual("", string.Join(";", from p in actual.Platforms orderby p select p));
            Assert.AreEqual("", string.Join(";", from p in actual.ReservedSerialPorts orderby p select p));
            Assert.AreEqual("COM30", actual.VirtualDeviceSerialPort);
            #endregion

            #region Fully specified
            File.WriteAllText(Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName),
"""
{
    "NuGetPackageList": "NuGetPackageList.txt",
    "PathToLocalNanoCLR": "../Tools/nanoclr.exe",
    "PathToLocalCLRInstanceDirectory": "Firmware/WIN_DLL_nanoCLR-1.12.0.53",
    "VirtualDeviceSerialPort": "COM42",
    "ReservedSerialPorts": ["COM30", "COM31", "COM42", "COM43"],
    "FirmwareArchivePath": "Firmware",
    "DeviceTypeTargets": {
        "Primary device": "ESP32_S3_ALL",
        "Alternative": "ESP32_S3_BLE",
        "Test devices": ["ESP32_S3", "ESP32_S3_ALL", "ESP32_S3_BLE"],
        "Empty": " ",
        "None": [],
        "NoneEmpty": [" "]
    },
    "DeviceTypes": [
        "Primary device",
        "Virtual nanoDevice"
    ],
    "Platforms": [
        "ESP32"
    ]
}
""");

            actual = NanoDevicesConfiguration.Read(testDirectory);

            Assert.AreEqual("Alternative;Primary device;Test devices", string.Join(";", from d in actual.DeviceTypeNames orderby d select d));
            Assert.AreEqual("Primary device;Virtual nanoDevice", string.Join(";", from d in actual.DeviceTypes orderby d select d));
            Assert.AreEqual("", string.Join(";", from t in actual.DeviceTypeTargets("invalid") orderby t select t));
            Assert.AreEqual("ESP32_S3_BLE", string.Join(";", from d in actual.DeviceTypeTargets("Alternative") orderby d select d));
            Assert.AreEqual("ESP32_S3_ALL", string.Join(";", from d in actual.DeviceTypeTargets("Primary device") orderby d select d));
            Assert.AreEqual("ESP32_S3;ESP32_S3_ALL;ESP32_S3_BLE", string.Join(";", from d in actual.DeviceTypeTargets("Test devices") orderby d select d));
            Assert.AreEqual(Path.Combine(testDirectory, "Firmware"), actual.FirmwareArchivePath);
            Assert.AreEqual(Path.Combine(testDirectory, "NuGetPackageList.txt"), actual.NuGetPackageList);
            Assert.AreEqual(Path.Combine(testDirectory, "Firmware", "WIN_DLL_nanoCLR-1.12.0.53"), actual.PathToLocalCLRInstanceDirectory);
            Assert.AreEqual(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testDirectory)!, "Tools", "nanoclr.exe")), actual.PathToLocalNanoCLR);
            Assert.AreEqual("ESP32", string.Join(";", from p in actual.Platforms orderby p select p));
            Assert.AreEqual("COM30;COM31;COM43", string.Join(";", from p in actual.ReservedSerialPorts orderby p select p));
            Assert.AreEqual("COM42", actual.VirtualDeviceSerialPort);
            #endregion
        }

        [TestMethod]
        public void NanoDevicesConfiguration_Hierarchy()
        {
            #region Setup
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            NanoDevicesConfiguration.UserProfileDirectoryPath = Path.Combine(testDirectory, "UserProfile");
            Directory.CreateDirectory(NanoDevicesConfiguration.UserProfileDirectoryPath);
            string globalSettingsDirectory = Path.Combine(testDirectory, "Global");
            Directory.CreateDirectory(globalSettingsDirectory);
            File.WriteAllText(Path.Combine(globalSettingsDirectory, NanoDevicesConfiguration.ConfigurationFileName),
"""
{
    "NuGetPackageList": "NuGetPackageList.txt",
    "PathToLocalNanoCLR": "../Tools/nanoclr.exe",
    "PathToLocalCLRInstanceDirectory": "Firmware/WIN_DLL_nanoCLR-1.12.0.53",
    "VirtualDeviceSerialPort": "COM42",
    "ReservedSerialPorts": ["COM10", "COM11", "COM42", "COM43"],
    "FirmwareArchivePath": "Firmware",
    "DeviceTypeTargets": {
        "Primary device": "ESP32_S3_ALL",
        "Alternative": "ESP32_S3_BLE",
        "Test devices": ["ESP32_S3", "ESP32_S3_ALL", "ESP32_S3_BLE"]
    },
    "DeviceTypes": [
        "Primary device",
        "Virtual nanoDevice"
    ],
    "Platforms": [
        "ESP32"
    ]
}
""");
            #endregion

            #region All settings from global settings
            File.WriteAllText(Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName),
"""
{
    "GlobalSettingsDirectoryPath": "Global"
}
""");
            var actual = NanoDevicesConfiguration.Read(testDirectory);

            Assert.AreEqual("Alternative;Primary device;Test devices", string.Join(";", from d in actual.DeviceTypeNames orderby d select d));
            Assert.AreEqual("Primary device;Virtual nanoDevice", string.Join(";", from d in actual.DeviceTypes orderby d select d));
            Assert.AreEqual("ESP32_S3_BLE", string.Join(";", from d in actual.DeviceTypeTargets("Alternative") orderby d select d));
            Assert.AreEqual("ESP32_S3_ALL", string.Join(";", from d in actual.DeviceTypeTargets("Primary device") orderby d select d));
            Assert.AreEqual("ESP32_S3;ESP32_S3_ALL;ESP32_S3_BLE", string.Join(";", from d in actual.DeviceTypeTargets("Test devices") orderby d select d));
            Assert.AreEqual(Path.Combine(globalSettingsDirectory, "Firmware"), actual.FirmwareArchivePath);
            Assert.AreEqual(Path.Combine(globalSettingsDirectory, "NuGetPackageList.txt"), actual.NuGetPackageList);
            Assert.AreEqual(Path.Combine(globalSettingsDirectory, "Firmware", "WIN_DLL_nanoCLR-1.12.0.53"), actual.PathToLocalCLRInstanceDirectory);
            Assert.AreEqual(Path.Combine(testDirectory, "Tools", "nanoclr.exe"), actual.PathToLocalNanoCLR);
            Assert.AreEqual("ESP32", string.Join(";", from p in actual.Platforms orderby p select p));
            Assert.AreEqual("COM10;COM11;COM43", string.Join(";", from p in actual.ReservedSerialPorts orderby p select p));
            Assert.AreEqual("COM42", actual.VirtualDeviceSerialPort);
            #endregion

            #region Overwrite global settings
            File.WriteAllText(Path.Combine(testDirectory, NanoDevicesConfiguration.ConfigurationFileName),
"""
{
    "GlobalSettingsDirectoryPath": "Global",
    "NuGetPackageList": "NuGetPackageList.txt",
    "PathToLocalNanoCLR": "nanoclr.exe",
    "PathToLocalCLRInstanceDirectory": "Firmware/WIN_DLL_nanoCLR-1.12.0.53",
    "VirtualDeviceSerialPort": "",
    "ReservedSerialPorts": ["COM30", "COM31", "COM42"],
    "FirmwareArchivePath": "Firmware",
    "DeviceTypeTargets": {
        "Product": "ESP32_S3_ALL",
        "Primary device": null,
        "Alternative": "",
        "Test devices": ["ESP32_S3_ALL", "ESP32_S3_BLE"]
    },
    "DeviceTypes": [
        "Product"
    ],
    "Platforms": [
        "WIN32"
    ]
}
""");
            actual = NanoDevicesConfiguration.Read(testDirectory);

            Assert.AreEqual("Product;Test devices", string.Join(";", from d in actual.DeviceTypeNames orderby d select d));
            Assert.AreEqual("Product", string.Join(";", from d in actual.DeviceTypes orderby d select d));
            Assert.AreEqual("ESP32_S3_ALL", string.Join(";", from d in actual.DeviceTypeTargets("Product") orderby d select d));
            Assert.AreEqual("ESP32_S3_ALL;ESP32_S3_BLE", string.Join(";", from d in actual.DeviceTypeTargets("Test devices") orderby d select d));
            Assert.AreEqual(Path.Combine(testDirectory, "Firmware"), actual.FirmwareArchivePath);
            Assert.AreEqual(Path.Combine(testDirectory, "NuGetPackageList.txt"), actual.NuGetPackageList);
            Assert.AreEqual(Path.Combine(testDirectory, "Firmware", "WIN_DLL_nanoCLR-1.12.0.53"), actual.PathToLocalCLRInstanceDirectory);
            Assert.AreEqual(Path.Combine(testDirectory, "nanoclr.exe"), actual.PathToLocalNanoCLR);
            Assert.AreEqual("WIN32", string.Join(";", from p in actual.Platforms orderby p select p));
            Assert.AreEqual("COM31;COM42", string.Join(";", from p in actual.ReservedSerialPorts orderby p select p));
            Assert.AreEqual("COM30", actual.VirtualDeviceSerialPort);
            #endregion

            #region Add userprofile settings
            File.WriteAllText(Path.Combine(NanoDevicesConfiguration.UserProfileDirectoryPath, NanoDevicesConfiguration.ConfigurationFileName),
"""
{
    "GlobalSettingsDirectoryPath": "../Global",
    "NuGetPackageList": "ShouldBeIgnored",
    "PathToLocalNanoCLR": "ShouldBeIgnored",
    "PathToLocalCLRInstanceDirectory": "ShouldBeIgnored",
    "VirtualDeviceSerialPort": "ShouldBeIgnored",
    "ReservedSerialPorts": ["COM30", "COM42", "COM99"],
    "FirmwareArchivePath": "ShouldBeIgnored",
    "DeviceTypeTargets": {
        "ShouldBeIgnored": "ShouldBeIgnored"
    },
    "DeviceTypes": [
        "ShouldBeIgnored"
    ],
    "Platforms": [
        "ShouldBeIgnored"
    ]
}
""");
            actual = NanoDevicesConfiguration.Read(testDirectory);

            Assert.AreEqual("Product;Test devices", string.Join(";", from d in actual.DeviceTypeNames orderby d select d));
            Assert.AreEqual("Product", string.Join(";", from d in actual.DeviceTypes orderby d select d));
            Assert.AreEqual("ESP32_S3_ALL", string.Join(";", from d in actual.DeviceTypeTargets("Product") orderby d select d));
            Assert.AreEqual("ESP32_S3_ALL;ESP32_S3_BLE", string.Join(";", from d in actual.DeviceTypeTargets("Test devices") orderby d select d));
            Assert.AreEqual(Path.Combine(testDirectory, "Firmware"), actual.FirmwareArchivePath);
            Assert.AreEqual(Path.Combine(testDirectory, "NuGetPackageList.txt"), actual.NuGetPackageList);
            Assert.AreEqual(Path.Combine(testDirectory, "Firmware", "WIN_DLL_nanoCLR-1.12.0.53"), actual.PathToLocalCLRInstanceDirectory);
            Assert.AreEqual(Path.Combine(testDirectory, "nanoclr.exe"), actual.PathToLocalNanoCLR);
            Assert.AreEqual("WIN32", string.Join(";", from p in actual.Platforms orderby p select p));
            Assert.AreEqual("COM31;COM42;COM99", string.Join(";", from p in actual.ReservedSerialPorts orderby p select p));
            Assert.AreEqual("COM30", actual.VirtualDeviceSerialPort);
            #endregion

            #region Only userprofile settings
            actual = NanoDevicesConfiguration.Read(Path.Combine(testDirectory, "DoesNotExist"));

            Assert.IsNotNull(actual);
            Assert.AreEqual("", string.Join(";", from d in actual.DeviceTypeNames orderby d select d));
            Assert.AreEqual("", string.Join(";", from d in actual.DeviceTypes orderby d select d));
            Assert.IsNull(actual.FirmwareArchivePath);
            Assert.IsNull(actual.NuGetPackageList);
            Assert.IsNull(actual.PathToLocalCLRInstanceDirectory);
            Assert.IsNull(actual.PathToLocalNanoCLR);
            Assert.AreEqual("", string.Join(";", from p in actual.Platforms orderby p select p));
            Assert.AreEqual("COM42;COM99", string.Join(";", from p in actual.ReservedSerialPorts orderby p select p));
            Assert.AreEqual("COM30", actual.VirtualDeviceSerialPort);
            #endregion
        }
    }
}
