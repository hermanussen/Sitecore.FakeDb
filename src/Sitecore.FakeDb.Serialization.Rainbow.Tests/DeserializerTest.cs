namespace Sitecore.FakeDb.Serialization.Rainbow.Tests
{
    using System.IO;
    using FluentAssertions;
    using Sitecore.Data;
    using Sitecore.Data.Serialization.ObjectModel;
    using Xunit;

    public class DeserializerTest
    {
        [Fact]
        public void ShouldDeserializeFile()
        {
            var deserialized = DesirializeFile(@"content\content\Home.yml");
            deserialized.Should().NotBeNull();
            deserialized.ID.ShouldBeEquivalentTo(new ID("{17ff281e-85c1-4529-ac79-24ecae17440e}").ToGuid().ToString());
        }

        [Fact]
        public void ShouldDeserializeItemName()
        {
            var deserialized = DesirializeFile(@"content\content\Home.yml");
            deserialized.Should().NotBeNull();
            deserialized.Name.ShouldBeEquivalentTo("Home");
        }

        [Fact]
        public void ShouldDeserializeTemplateId()
        {
            var deserialized = DesirializeFile(@"content\content\Home.yml");
            deserialized.Should().NotBeNull();
            deserialized.TemplateID.ShouldBeEquivalentTo(new ID("{76036f5e-cbce-46d1-af0a-4143f9b557aa}").ToGuid().ToString());
        }

        [Fact]
        public void ShouldDeserializeParentId()
        {
            var deserialized = DesirializeFile(@"content\content\Home.yml");
            deserialized.Should().NotBeNull();
            deserialized.ParentID.ShouldBeEquivalentTo(new ID("{0de95ae4-41ab-4d01-9eb0-67441b7c2450}").ToGuid().ToString());
        }

        [Fact]
        public void ShouldDeserializeItemPath()
        {
            var deserialized = DesirializeFile(@"content\content\Home.yml");
            deserialized.Should().NotBeNull();
            deserialized.ItemPath.ShouldBeEquivalentTo("/sitecore/content/Home");
        }

        [Fact]
        public void ShouldDeserializeSharedField()
        {
            var deserialized = DesirializeFile(@"content\content\Home.yml");
            deserialized.Should().NotBeNull();
            deserialized.SharedFields.Should().NotBeEmpty();
            deserialized.SharedFields[0].FieldID.ShouldBeEquivalentTo(new ID("{06d5295c-ed2f-4a54-9bf2-26228d113318}").ToGuid().ToString());
            deserialized.SharedFields[0].FieldName.ShouldBeEquivalentTo("__Icon");
            deserialized.SharedFields[0].FieldValue.ShouldBeEquivalentTo("Network/16x16/home.png");
        }

        [Fact]
        public void ShouldDeserializeVersionedField()
        {
            var deserialized = DesirializeFile(@"content\content\Home.yml");
            deserialized.Should().NotBeNull();
            deserialized.Versions.Should().NotBeEmpty();
            deserialized.Versions[0].Fields.Should().NotBeEmpty();
            deserialized.Versions[0].Fields[0].FieldID.ShouldBeEquivalentTo(new ID("{25bed78c-4957-4165-998a-ca1b52f67497}").ToGuid().ToString());
            deserialized.Versions[0].Fields[0].FieldName.ShouldBeEquivalentTo("__Created");
            deserialized.Versions[0].Fields[0].FieldValue.ShouldBeEquivalentTo("20140516T182531:635358615311709544");
        }

        private static SyncItem DesirializeFile(string filePath)
        {
            DirectoryInfo serializationFolder = DeserializerExtensions.GetSerializationFolder("master");
            SyncItem deserialized = new Deserializer(serializationFolder)
                .DeserializeFile(new FileInfo(Path.Combine(serializationFolder.FullName, filePath)));
            return deserialized;
        }
    }
}
