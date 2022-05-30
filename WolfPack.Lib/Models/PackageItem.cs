using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WolfPack.Lib.Services
{
    public class PackageItem
    {
        [Index(0)]
        public string PackagePath { get; set; }
        [Index(1)]
        public string PackageChecksum { get; set; }
        [Index(2)]
        public long PackageSize { get; set; }
        [Index(3)]
        public string ItemPath { get; set; }
        [Index(4)]
        public string ItemChecksum {get; set;}
        [Index(5)]
        public long ItemSize { get; set; }
        [Index(6)]
        public string ItemAbsolutePath { get; set; }

        public PackageItem()
        {

        }

        public PackageItem(IPackage p, IValidatableItem i)
        {
            PackagePath = p.RelativePath;
            PackageChecksum = p.Checksum != null ? MD5Validator.GetChecksumAsString(p.Checksum) : null;
            PackageSize = p.Size;
            ItemPath = i.RelativePath;
            ItemChecksum = i.Checksum != null ? MD5Validator.GetChecksumAsString(i.Checksum) : null;
            ItemSize = i.Size;
            ItemAbsolutePath = i.AbsolutePath;
        }
    }
}
