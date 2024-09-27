// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Targeting.Tooling.Tests.Helpers
{
    public abstract class TestClassBase
    {
        public TestContext TestContext { get; set; } = null!;


        [TestInitialize]
        public void ReportTFM()
        {
            Debug.WriteLine($".NET common runtime version: {Environment.Version}");
        }
    }
}
