// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace nanoFramework.Targeting.Tooling
{
    /// <summary>
    /// The version information of the native assembly version implemented by one or more firmware/targets.
    /// </summary>
    /// <param name="assemblyName">Name of the .NET assembly that corresponds to the native assembly.</param>
    /// <param name="version">Version of the native assembly.</param>
    /// <param name="checkSum">Checksum of the interface to the native assembly.</param>
    /// <param name="targetNames">The names of the firmware/targets that contain this version of the native assembly implementation.
    /// The firmware for the Virtual Device has name <see cref="NanoDevicesConfiguration.VirtualDeviceName"/>.</param>
    public sealed class ImplementedNativeAssemblyVersion(string assemblyName, string version, uint checkSum, IReadOnlyList<string> targetNames)
        : NativeAssemblyMetadata(assemblyName, version, checkSum)
    {
        #region Properties
        /// <summary>
        /// Gets the names of the firmware/targets that require this version of the native assembly implementation.
        /// The firmware for the Virtual Device has name <see cref="NanoDevicesConfiguration.VirtualDeviceName"/>.
        /// </summary>
        public IReadOnlyList<string> TargetNames
        {
            get;
        } = targetNames;
        #endregion
    }
}
