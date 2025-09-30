using System;
using System.IO;

namespace LaunchBoxMetadata.Tests;

public class DatabaseFixture : IDisposable
{
    public LaunchBoxDatabase Database { get; }

    public DatabaseFixture()
    {
        Database = new LaunchBoxDatabase(Path.GetTempPath());
        Database.CreateDatabase(new LaunchBoxXmlParser("Metadata.xml"));
    }

    public void Dispose() => Database.DeleteDatabase();
}