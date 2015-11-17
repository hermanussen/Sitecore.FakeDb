﻿namespace Sitecore.FakeDb.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Sitecore.Data.Serialization.ObjectModel;
    using Sitecore.Diagnostics;

    public class Deserializer : IDeserializer
    {
        private DirectoryInfo serializationFolder;

        public Deserializer(DirectoryInfo serializationFolder)
        {
            this.serializationFolder = serializationFolder;
        }

        public SyncItem DeserializeFile(FileInfo file)
        {
            Assert.IsTrue(".item".Equals(file.Extension, StringComparison.InvariantCultureIgnoreCase), string.Format("File '{0}' is not a .item file", file.FullName));
            return SyncItem.ReadItem(new Tokenizer(file.OpenText()));
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
                    foreach (var childItemFile in childItemsFolder.GetFiles("*.item", SearchOption.AllDirectories))
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

            foreach (var childItemFile in truePath.GetFiles("*.item", SearchOption.AllDirectories))
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
                  "{0}.item",
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
