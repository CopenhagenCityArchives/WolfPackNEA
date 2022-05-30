using System;
using System.Collections.Generic;
using System.IO;

namespace WolfPack.Lib.Services
{
    public interface IPacker
    {
        public void PackItemsInStream(IPackage pack);
        public string GetPackageFileExtension();
        public Stream PackedItemsStream { get; set; }
        public int MaxRetries { get; set; }
        public IEnumerable<Tuple<string, Stream>> UnpackItemsFromStream();
        public IEnumerable<string> GetRelativePathsForEntries();
        public long MaxMemoryStreamSize { get; set; }
    }
}