// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using Microsoft.Build.Framework;

namespace Targeting.Tooling.Tests.Helpers
{
    internal sealed class ITaskItemHelper(string itemSpec) : ITaskItem
    {
        public string ItemSpec { get; set; } = itemSpec;

        ICollection ITaskItem.MetadataNames => throw new NotImplementedException();

        int ITaskItem.MetadataCount => throw new NotImplementedException();

        IDictionary ITaskItem.CloneCustomMetadata() => throw new NotImplementedException();
        void ITaskItem.CopyMetadataTo(ITaskItem destinationItem) => throw new NotImplementedException();
        string ITaskItem.GetMetadata(string metadataName) => throw new NotImplementedException();
        void ITaskItem.RemoveMetadata(string metadataName) => throw new NotImplementedException();
        void ITaskItem.SetMetadata(string metadataName, string metadataValue) => throw new NotImplementedException();
    }
}
