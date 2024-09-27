// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;

namespace Targeting.Tooling.Tests.Helpers
{
    internal static class TestDirectoryHelper
    {
        public static string GetTestDirectory(TestContext context)
        {
            lock (typeof(TestDirectoryHelper))
            {
                s_lastIndex++;
                string path = Path.Combine(context.ResultsDirectory, "T.T.T", s_lastIndex.ToString());
                Debug.WriteLine($"Test directory: {path}");
                Directory.CreateDirectory(path);
                return path;
            }
        }
        private static int s_lastIndex;

        /// <summary>
        /// Copy an embedded resource to the test directory
        /// </summary>
        /// <param name="testClass"></param>
        /// <param name="embeddedResourceName"></param>
        /// <param name="testDirectory"></param>
        /// <param name="relativeFilePath"></param>
        /// <returns>The absolute path to the file</returns>
        public static string CopyEmbeddedResource(Type testClass, string embeddedResourceName, string testDirectory, string relativeFilePath)
        {
            using Stream? stream = testClass.Assembly.GetManifestResourceStream($"{testClass.FullName}.{embeddedResourceName}");
            if (stream is null)
            {
                Assert.Inconclusive($"Embedded resource not found: '{testClass.FullName}.{embeddedResourceName}'");
                return null!;
            }
            string fullPath = Path.Combine(testDirectory, relativeFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            using var file = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write);
            stream.CopyTo(file);
            return fullPath;
        }

        /// <summary>
        /// Copy a file to the test directory
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="testDirectory"></param>
        /// <param name="relativeFilePath"></param>
        /// <returns>The absolute path to the file</returns>
        public static string CopyFile(string sourceFilePath, string testDirectory, string relativeFilePath)
        {
            if (!File.Exists(sourceFilePath))
            {
                Assert.Inconclusive($"File not found: '{sourceFilePath}'");
                return null!;
            }

            string fullPath = Path.Combine(testDirectory, relativeFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.Copy(sourceFilePath, fullPath);

            return fullPath;
        }
    }
}
