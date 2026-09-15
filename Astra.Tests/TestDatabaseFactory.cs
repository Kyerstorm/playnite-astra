using System;
using System.IO;
using Astra.Data;

namespace Astra.Tests
{
    internal static class TestDatabaseFactory
    {
        public static AstraDatabase CreateTemp()
        {
            var dir = Path.Combine(Path.GetTempPath(), "AstraTests_" + Guid.NewGuid());
            return new AstraDatabase(dir);
        }
    }
}
