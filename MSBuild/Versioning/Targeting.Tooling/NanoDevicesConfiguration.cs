// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

namespace nanoFramework.Targeting.Tooling
{
    /// <summary>
    /// In-memory representation of the configuration in the <c>nano.devices.json</c>
    /// files that contain information about the devices projects should be deployable to.
    /// </summary>
    public sealed class NanoDevicesConfiguration
    {
        #region Fields and constants
        /// <summary>
        /// File name to store the nanoFramework.TestFramework configuration in
        /// that contains the settings for all computers/servers that host the
        /// tests runners. This file is typically added to version control / git.
        /// </summary>
        public const string ConfigurationFileName = "nano.devices.json";
        /// <summary>
        /// The name to identify a Virtual Device with.
        /// </summary>
        public const string VirtualDeviceName = "Virtual nanoDevice";

        private string? _virtualDeviceSerialPort;
        private List<string> _reservedSerialPorts = [];
        private readonly Dictionary<string, List<string>> _deviceTypeTargets = [];
        private List<string> _deviceTypes = [];
        private List<string> _platforms = [];
        private static readonly AsyncLocal<string> s_userProfileDirectoryPath = new();
        private static readonly string s_defaultUserProfileDirectoryPath
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nanoFramework");
        #endregion

        #region Properties
        /// <summary>
        /// The path to a file that lists the allowed versions of the NuGet packages to use (<see cref="NuGetPackageList"/>).
        /// The value can be <c>null</c> if no check of NuGet packages is required.
        /// </summary>
        public string? NuGetPackageList
        {
            get; private set;
        }

        /// <summary>
        /// The path to a local version of the <c>nano.exe</c> tool that should be used to
        /// run a Virtual nanoDevice. If the value is <c>null</c>, the global tool should be used.
        /// </summary>
        public string? PathToLocalNanoCLR
        {
            get; private set;
        }

        /// <summary>
        /// The path to a directory that contains a file <c>nanoFramework.nanoCLR.dll</c>. The latter
        /// implements an alternative Virtual nanoDevice runtime. If the value is <c>null</c>, the runtime
        /// embedded in the <c>nanoclr.exe</c> file (<see cref="PathToLocalNanoCLR"/>) is used.
        /// </summary>
        public string? PathToLocalCLRInstanceDirectory
        {
            get; private set;
        }

        /// <summary>
        /// The serial port to use for a Virtual nanoDevice where applications can be deployed to.
        /// </summary>
        public string VirtualDeviceSerialPort
        {
            get => _virtualDeviceSerialPort ?? "COM30";
            private set => _virtualDeviceSerialPort = value;
        }

        /// <summary>
        /// The serial ports reserved for use by, e.g., a Virtual nanoDevice. These ports should be
        /// excluded in the discovery of real hardware nanoDevices. The <see cref="VirtualDeviceSerialPort"/>
        /// is not a member of this list.
        /// </summary>
        public IReadOnlyList<string> ReservedSerialPorts
            => _reservedSerialPorts;

        /// <summary>
        /// The path to the firmware archive directory.
        /// </summary>
        public string? FirmwareArchivePath
        {
            get; private set;
        }

        /// <summary>
        /// A collection of named device types a project can be deployed to.
        /// </summary>
        public ICollection<string> DeviceTypeNames
            => _deviceTypeTargets.Keys;

        /// <summary>
        /// The list of firmware/target names that are used for a named device type.
        /// </summary>
        /// <param name="name">Name of the device type; must be one of the <see cref="DeviceTypeNames"/>.</param>
        /// <returns></returns>
        public IReadOnlyList<string> DeviceTypeTargets(string name)
        {
            _deviceTypeTargets.TryGetValue(name, out List<string>? result);
            return result ?? [];
        }

        /// <summary>
        /// A list of device types the project is designed to be deployed to. The names are a subset
        /// of the <see cref="DeviceTypeNames"/>.
        /// </summary>
        public IReadOnlyList<string> DeviceTypes
            => _deviceTypes;

        /// <summary>
        /// A list of platforms the project is designed to be deployed to. This is shorthand to select all
        /// named devices in <see cref="DeviceTypeNames"/> that match the specified platform.
        /// </summary>
        public IReadOnlyList<string> Platforms
            => _platforms;

        /// <summary>
        /// Path with the base location for firmware packages.
        /// </summary>
        public static string UserProfileDirectoryPath
        {
            get => s_userProfileDirectoryPath.Value is null ? s_defaultUserProfileDirectoryPath : s_userProfileDirectoryPath.Value;

            // The path must be assignable for testability
            internal set => s_userProfileDirectoryPath.Value = value;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Read the hierarchical configuration, starting with the <see cref="ConfigurationFileName"/>
        /// in the specified directory.
        /// </summary>
        /// <param name="configurationDirectoryPath">Directory the configuration file resides in.</param>
        /// <returns>The configuration.</returns>
        public static NanoDevicesConfiguration Read(string configurationDirectoryPath)
        {
            // Read the configuration
            NanoDevicesConfiguration result = Read(configurationDirectoryPath, false);

            // Add reserved serial ports from the user's global settings
            NanoDevicesConfiguration globalForUser = Read(UserProfileDirectoryPath, true);
            foreach (string port in globalForUser._reservedSerialPorts)
            {
                if (!result._reservedSerialPorts.Contains(port))
                {
                    result._reservedSerialPorts.Add(port);
                }
            }

            // Corrections
            result._reservedSerialPorts.Remove(result.VirtualDeviceSerialPort);

            return result;
        }
        #endregion

        #region Implementation
        private static NanoDevicesConfiguration Read(string configurationDirectoryPath, bool reservedSerialPortsOnly)
        {
            string configurationFilePath = Path.Combine(configurationDirectoryPath, ConfigurationFileName);
            if (!File.Exists(configurationFilePath))
            {
                return new NanoDevicesConfiguration();
            }

            ConfigurationInJson? specification = JsonConvert.DeserializeObject<ConfigurationInJson>(File.ReadAllText(configurationFilePath));
            if (specification is null)
            {
                return new NanoDevicesConfiguration();
            }

            string? CompletePath(string? path)
                => string.IsNullOrWhiteSpace(path)
                        ? null
                        : Path.GetFullPath(Path.Combine(configurationDirectoryPath, path));
            specification.GlobalSettingsDirectoryPath = CompletePath(specification.GlobalSettingsDirectoryPath);

            NanoDevicesConfiguration result;
            if (!reservedSerialPortsOnly && specification.GlobalSettingsDirectoryPath is not null)
            {
                result = Read(specification.GlobalSettingsDirectoryPath, reservedSerialPortsOnly);
            }
            else
            {
                result = new NanoDevicesConfiguration();
            }

            if (specification.ReservedSerialPorts is not null)
            {
                result._reservedSerialPorts = specification.ReservedSerialPorts;
            }

            if (!reservedSerialPortsOnly)
            {
                if (specification.NuGetPackageList is not null)
                {
                    result.NuGetPackageList = CompletePath(specification.NuGetPackageList);
                }
                if (specification.PathToLocalNanoCLR is not null)
                {
                    result.PathToLocalNanoCLR = CompletePath(specification.PathToLocalNanoCLR);
                }
                if (specification.PathToLocalCLRInstanceDirectory is not null)
                {
                    result.PathToLocalCLRInstanceDirectory = CompletePath(specification.PathToLocalCLRInstanceDirectory);
                }
                if (specification.VirtualDeviceSerialPort is not null)
                {
                    result._virtualDeviceSerialPort = string.IsNullOrWhiteSpace(specification.VirtualDeviceSerialPort) ? null : specification.VirtualDeviceSerialPort;
                }
                if (specification.FirmwareArchivePath is not null)
                {
                    result.FirmwareArchivePath = CompletePath(specification.FirmwareArchivePath);
                }

                if (specification.DeviceTypes is not null)
                {
                    result._deviceTypes = specification.DeviceTypes;
                }
                if (specification.Platforms is not null)
                {
                    result._platforms = specification.Platforms;
                }

                if (specification.DeviceTypeTargets is not null)
                {
                    foreach (KeyValuePair<string, List<string>?> deviceType in specification.DeviceTypeTargets)
                    {
                        if (deviceType.Value is not null && (from t in deviceType.Value
                                                             where !string.IsNullOrWhiteSpace(t)
                                                             select t).Any())
                        {
                            result._deviceTypeTargets[deviceType.Key] = (from t in deviceType.Value
                                                                         where !string.IsNullOrWhiteSpace(t)
                                                                         select t).ToList();
                        }
                        else
                        {
                            result._deviceTypeTargets.Remove(deviceType.Key);
                        }
                    }
                }
            }
            return result;
        }

        private sealed class ConfigurationInJson
        {
            public string? GlobalSettingsDirectoryPath { get; set; }
            public string? NuGetPackageList { get; set; }
            public string? PathToLocalNanoCLR { get; set; }
            public string? PathToLocalCLRInstanceDirectory { get; set; }
            public string? VirtualDeviceSerialPort { get; set; }
            public List<string>? ReservedSerialPorts { get; set; }
            public string? FirmwareArchivePath { get; set; }
            [JsonConverter(typeof(DeviceTypeTargetsConverter))]
            public Dictionary<string, List<string>?>? DeviceTypeTargets { get; set; }
            public List<string>? DeviceTypes { get; set; }
            public List<string>? Platforms { get; set; }
        }

        /// <summary>
        /// Converter for the <see cref="Values"/> dictionary
        /// </summary>
        private class DeviceTypeTargetsConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Dictionary<string, List<string>>);
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                var result = new Dictionary<string, List<string>?>();

                if (reader.TokenType == JsonToken.StartObject)
                {
                    reader.Read();
                    while (reader.TokenType != JsonToken.EndObject)
                    {
                        if (reader.TokenType != JsonToken.PropertyName)
                        {
                            throw new JsonException();
                        }
                        string key = reader.Value!.ToString()!;

                        reader.Read();
                        if (reader.TokenType == JsonToken.String)
                        {
                            result[key] = [reader.Value.ToString()];
                        }
                        else
                        {
                            result[key] = serializer.Deserialize<List<string>?>(reader)!;
                        }

                        reader.Read();
                    }
                }
                else
                {
                    throw new JsonException();
                }
                return result;
            }
        }
        #endregion
    }
}
