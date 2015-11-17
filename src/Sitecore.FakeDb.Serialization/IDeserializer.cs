namespace Sitecore.FakeDb.Serialization
{
    using System.IO;
    using Sitecore.Data.Serialization.ObjectModel;

    public interface IDeserializer
    {
        SyncItem DeserializeFile(FileInfo file);

        System.Collections.Generic.List<SyncItem> DeserializeAll(FileInfo itemFile, SyncItem syncItem, int maxDepth);

        FileInfo ResolveSerializationPath(string path);
    }
}
