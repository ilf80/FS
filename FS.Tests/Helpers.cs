using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace FS.Tests
{
    internal static class Helpers
    {
        public static bool CollectionsAreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            CollectionAssert.AreEqual(expected, actual);
            return true;
        }
    }
}
