namespace Sitecore.FakeDb.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Serialization.ObjectModel;
    using Sitecore.Diagnostics;

    /// <summary>
    /// Utility methods that help with deserializing data for use within unit tests.
    /// </summary>
    public static class DeserializerExtensions
    {
        private static Dictionary<string, IDeserializer> deserializers = new Dictionary<string, IDeserializer>();

        private static IDeserializer GetDeserializer(DirectoryInfo serializationFolder)
        {
            lock (deserializers)
            {
                if (!deserializers.ContainsKey(serializationFolder.FullName))
                {
                    IDeserializer deserializer = null;
                    
                    string typeName = GetDeserializerType(serializationFolder);
                    if (! string.IsNullOrWhiteSpace(typeName))
                    {
                        Type type = Type.GetType(typeName);
                        Assert.IsNotNull(type, "Type '{0}' could not be found", typeName);

                        deserializer = Activator.CreateInstance(type, new [] { serializationFolder }) as IDeserializer;
                        Assert.IsNotNull(deserializer, "Type '{0}' could not be instantiated");
                    }

                    deserializer = deserializer ?? new Deserializer(serializationFolder);
                    deserializers.Add(serializationFolder.FullName, deserializer);
                }

                return deserializers[serializationFolder.FullName];
            }
        }

        /// <summary>
        /// Deserializes an item file that was serialized with normal Sitecore serialization or TDS.
        /// </summary>
        /// <param name="file">.item file</param>
        /// <param name="serializationFolder"></param>
        /// <returns></returns>
        public static SyncItem Deserialize(this FileInfo file, DirectoryInfo serializationFolder)
        {
            Assert.ArgumentNotNull(file, "file");
            Assert.IsTrue(file.Exists, string.Format("File '{0}' can not be found or cannot be accessed", file.FullName));

            var item = GetDeserializer(serializationFolder).DeserializeFile(file);

            Assert.IsTrue(ID.IsID(item.ID), string.Format("Item id '{0}' is not a valid guid", item.ID));
            Assert.IsTrue(ID.IsID(item.TemplateID), string.Format("Item template id '{0}' is not a valid guid", item.TemplateID));

            return item;
        }

        /// <summary>
        /// Deserializes all .item files below the item's children folder and all deeper directories.
        /// Also traverses shortened paths.
        /// </summary>
        /// <param name="itemFile"></param>
        /// <param name="syncItem"></param>
        /// <param name="serializationFolder"></param>
        /// <param name="maxDepth"></param>
        /// <returns></returns>
        public static List<SyncItem> DeserializeAll(this FileInfo itemFile, SyncItem syncItem, DirectoryInfo serializationFolder, int maxDepth)
        {
            return GetDeserializer(serializationFolder).DeserializeAll(itemFile, syncItem, maxDepth);
        }

        /// <summary>
        /// Maps shared field values from a deserialized item to the FakeDb item.
        /// </summary>
        /// <param name="item">Deserialized item</param>
        /// <param name="dsDbItem">FakeDb item to copy values to</param>
        internal static void CopySharedFieldsTo(this SyncItem item, IDsDbItem dsDbItem)
        {
            foreach (var sharedField in item.SharedFields)
            {
                Assert.IsTrue(ID.IsID(sharedField.FieldID), string.Format("Shared field id '{0}' is not a valid guid", sharedField.FieldID));

                var field = dsDbItem.Fields.FirstOrDefault(f => f.ID.ToString() == sharedField.FieldID);
                if (field != null)
                {
                    field.Value = sharedField.FieldValue;
                }
                else
                {
                    dsDbItem.Add(new DbField(sharedField.FieldName, ID.Parse(sharedField.FieldID)) { Value = sharedField.FieldValue, Shared = true });
                }
            }
        }

        /// <summary>
        /// Maps versioned field values from a deserialized item to the FakeDb item.
        /// </summary>
        /// <param name="item">Deserialized item</param>
        /// <param name="dsDbItem">FakeDb item to copy values to</param>
        internal static void CopyVersionedFieldsTo(this SyncItem item, IDsDbItem dsDbItem)
        {
            var fields = new List<DbField>();
            foreach (var version in item.Versions)
            {
                foreach (var field in version.Fields)
                {
                    Assert.IsTrue(ID.IsID(field.FieldID), string.Format("Field id '{0}' is not a valid guid", field.FieldID));
                    var fieldId = ID.Parse(field.FieldID);
                    var dbField = fields.FirstOrDefault(f => f.ID == fieldId);

                    if (dbField == null)
                    {
                        dbField = new DbField(field.FieldName, fieldId) { Shared = false };
                        dsDbItem.Add(dbField);
                        fields.Add(dbField);
                    }

                    int versionNumber;
                    if (int.TryParse(version.Version, out versionNumber))
                    {
                        dbField.Add(version.Language, versionNumber, field.FieldValue);
                    }
                    else
                    {
                        dbField.Add(version.Language, field.FieldValue);
                    }
                }
            }
        }

        /// <summary>
        /// Resolves a sitecore path to the filesystem where it is serialized.
        /// </summary>
        /// <param name="path">A valid sitecore item path</param>
        /// <param name="serializationFolderName">Name of serialization folder as configured in app.config</param>
        /// <returns></returns>
        internal static FileInfo ResolveSerializationPath(string path, string serializationFolderName)
        {
            var serializationFolder = GetSerializationFolder(serializationFolderName);
            return ResolveSerializationPath(path, serializationFolder);
        }

        /// <summary>
        /// Resolves a sitecore path to the filesystem where it is serialized.
        /// For example, path=/sitecore/content/item1 and serializationFolderName=master 
        /// Will be resolved to c:\site\data\serialization\master\sitecore\content\item1.item
        /// 
        /// You need to define the serialization folders in the app.config. For example:
        /// <szfolders>
        ///   <folder name="core" value="c:\site\data\serialization\core\" />
        ///   <folder name="master" value="c:\site\data\serialization\master\" />
        ///   <folder name="web" value="c:\site\data\serialization\web\" />
        /// </szfolders>
        /// </summary>
        /// <param name="path">A valid sitecore item path</param>
        /// <param name="serializationFolder">Folder in which serialized items are available</param>
        /// <returns></returns>
        internal static FileInfo ResolveSerializationPath(string path, DirectoryInfo serializationFolder)
        {
            return GetDeserializer(serializationFolder).ResolveSerializationPath(path);
        }

        public static DirectoryInfo GetSerializationFolder(string serializationFolderName)
        {
            Assert.IsNotNullOrEmpty(serializationFolderName, "Please specify a serialization folder when you instantiate a FakeDb or individual DsDbItem/DsDbTemplate");

            var folderNode = Factory.GetConfigNode(string.Format("szfolders/folder[@name='{0}']", serializationFolderName));

            Assert.IsNotNull(
              folderNode,
              string.Format(
                "Configuration for serialization folder name '{0}' could not be found; please check the <szfolders /> configuration in the app.config",
                serializationFolderName));

            var serializationFolder = new DirectoryInfo(folderNode.Attributes["value"].Value);
            Assert.IsTrue(
              serializationFolder.Exists,
              string.Format(
                "Path '{0}', as configured in the app.config could not be found; please check the <szfolders /> configuration in the app.config",
                serializationFolder.FullName));
            return serializationFolder;
        }

        private static string GetDeserializerType(DirectoryInfo serializationFolder)
        {
            foreach (var folderNode in Factory.GetConfigNodes(string.Format("szfolders/folder")).OfType<XmlNode>())
            {
                var folder = new DirectoryInfo(folderNode.Attributes["value"].Value);
                if (folder.FullName.Equals(serializationFolder.FullName))
                {
                    if (folderNode.Attributes["deserializer"] != null)
                    {
                        return folderNode.Attributes["deserializer"].Value;
                    }
                    return null;
                }
            }
            return null;
        }
    }
}