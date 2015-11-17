namespace Sitecore.FakeDb.Serialization.Rainbow
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Sitecore.Data.Serialization.ObjectModel;
    using Sitecore.Diagnostics;
    using global::Rainbow.Model;
    using global::Rainbow.Storage.Yaml;

    public class Deserializer : IDeserializer
    {
        private DirectoryInfo serializationFolder;

        public Deserializer(DirectoryInfo serializationFolder)
        {
            this.serializationFolder = serializationFolder;
        }

        public SyncItem DeserializeFile(FileInfo file)
        {
            Assert.IsTrue(".yml".Equals(file.Extension, StringComparison.InvariantCultureIgnoreCase), string.Format("File '{0}' is not a .yml file", file.FullName));

            var formatter = new YamlSerializationFormatter(null, null);

            using (var ms = file.OpenRead())
            {
                IItemData item = formatter.ReadSerializedItem(ms, "unittest.yml");
                SyncItem syncItem = new SyncItem()
                    {
                        BranchId = item.BranchId.ToString(),
                        DatabaseName = item.DatabaseName,
                        ID = item.Id.ToString(),
                        ItemPath = item.Path,
                        Name = item.Name,
                        ParentID = item.ParentId.ToString(),
                        TemplateID = item.TemplateId.ToString(),
                        TemplateName = "todo: templatename"
                    };
                foreach (IItemVersion itemVersion in item.Versions)
                {
                    SyncVersion syncVersion = new SyncVersion()
                        {
                            Language = itemVersion.Language.Name,
                            Revision = itemVersion.VersionNumber.ToString(),
                            Version = itemVersion.VersionNumber.ToString()
                        };
                    foreach (IItemFieldValue field in itemVersion.Fields)
                    {
                        syncVersion.Fields.Add(new SyncField()
                            {
                                FieldID = field.FieldId.ToString(),
                                FieldKey = field.FieldType,
                                FieldName = field.NameHint,
                                FieldValue = field.Value
                            });
                    }

                    syncItem.Versions.Add(syncVersion);
                }

                foreach (IItemFieldValue field in item.SharedFields)
                {
                    syncItem.SharedFields.Add(new SyncField()
                        {
                            FieldID = field.FieldId.ToString(),
                            FieldKey = field.FieldType,
                            FieldName = field.NameHint,
                            FieldValue = field.Value
                        });
                }

                return syncItem;
            }
        }

        public List<SyncItem> DeserializeAll(FileInfo itemFile, SyncItem syncItem, int maxDepth)
        {
            if (maxDepth <= 0)
            {
                return new List<SyncItem>();
            }

            Assert.ArgumentNotNull(itemFile, "itemFile");

            var result = new List<SyncItem>();

            // Find descendants in direct subfolder
            if (itemFile.Directory != null)
            {
                var childItemsFolder = new DirectoryInfo(itemFile.Directory.FullName + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(itemFile.Name));
                if (childItemsFolder.Exists)
                {
                    foreach (var childItemFile in childItemsFolder.GetFiles("*.yml", SearchOption.AllDirectories))
                    {
                        var childSyncItem = childItemFile.Deserialize(this.serializationFolder);
                        result.AddRange(childItemFile.DeserializeAll(childSyncItem, this.serializationFolder, maxDepth - 1));
                        result.Add(childSyncItem);
                    }
                }
            }

            // Find descendants in shortened paths
            var linkFiles = ShortenedPathsDictionary.GetLocationsFromLinkFiles(this.serializationFolder);
            if (!linkFiles.ContainsKey(syncItem.ItemPath))
            {
                return result;
            }

            var truePath = new DirectoryInfo(linkFiles[syncItem.ItemPath]);
            if (!truePath.Exists)
            {
                return result;
            }

            foreach (var childItemFile in truePath.GetFiles("*.yml", SearchOption.AllDirectories))
            {
                var childSyncItem = childItemFile.Deserialize(this.serializationFolder);
                result.AddRange(childItemFile.DeserializeAll(childSyncItem, this.serializationFolder, maxDepth - 1));
                result.Add(childSyncItem);
            }

            return result;
        }

        public FileInfo ResolveSerializationPath(string path)
        {
            var truePath = ShortenedPathsDictionary.FindTruePath(this.serializationFolder, path);

            var itemLocation =
              new FileInfo(
                string.Format(
                  "{0}.yml",
                  Path.Combine(
                    this.serializationFolder.FullName.Trim(new[] { Path.DirectorySeparatorChar }),
                    truePath.Replace('/', Path.DirectorySeparatorChar).Trim(new[] { Path.DirectorySeparatorChar }))));

            Assert.IsTrue(
              itemLocation.Exists,
              string.Format("Serialized item '{0}' could not be found in the path '{1}'; please check the path and if the item is serialized correctly", truePath, itemLocation.FullName));

            return itemLocation;
        }
    }
}
