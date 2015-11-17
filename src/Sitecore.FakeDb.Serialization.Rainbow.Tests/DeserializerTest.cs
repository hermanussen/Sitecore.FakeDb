namespace Sitecore.FakeDb.Serialization.Rainbow.Tests
{
    using System.IO;
    using FluentAssertions;
    using Sitecore.Data.Serialization.ObjectModel;
    using Xunit;

    public class DeserializerTest
    {
        [Fact]
        public void ShouldDeserializeFile()
        {
            DirectoryInfo serializationFolder = DeserializerExtensions.GetSerializationFolder("master");
            SyncItem deserialized = new Deserializer(serializationFolder).DeserializeFile(new FileInfo(Path.Combine(serializationFolder.FullName, @"content\content\Home.yml")));
            deserialized.Should().NotBeNull();
        }
    }
}
