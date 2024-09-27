// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.Targeting.Tooling
{
    /// <summary>
    /// Metadata of a native assembly, i.e., the native implementation that is required
    /// to be part of the firmware/CLR instance for a .NET nanoFramework assembly to function
    /// properly.
    /// </summary>
    /// <param name="assemblyName">Name of the .NET assembly that corresponds to the native assembly.</param>
    /// <param name="version">Version of the native assembly.</param>
    /// <param name="checkSum">Checksum of the interface to the native assembly.</param>
    public class NativeAssemblyMetadata(string assemblyName, string version, uint checkSum)
    {
        #region Properties
        /// <summary>
        /// Name of the .NET assembly that corresponds to the native assembly.
        /// </summary>
        public string AssemblyName
        {
            get;
        } = assemblyName;

        /// <summary>
        /// Version of the native assembly.
        /// </summary>
        public string Version
        {
            get;
        } = version;

        /// <summary>
        /// Checksum of the interface to the native assembly.
        /// </summary>
        public uint Checksum
        {
            get;
        } = checkSum;
        #endregion
    }
}
