using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WolfPack.Lib.Services
{
    public class Package : IPackage
    {
        [Ignore]
        public List<IValidatableItem> Items { get; set; }
        public string RelativePath { get; set; }
        [Ignore]
        public string AbsolutePath { get; set; }
        [Ignore]
        public byte[] Checksum { get; set; }
        public string ChecksumString { get { if (Checksum == null) { return null; } else { return MD5Validator.GetChecksumAsString(Checksum); } } }
        [Ignore]
        public long Size{ 
            get { return Items.Sum(i => i.Size); }
            set { throw new ArgumentException("Cannot set size of Package");  }
        }
        [Ignore]
        public bool IsDirectory
        {
            get
            {
                return false;
            }
        }

        public Package(string relativePath)
        {
            Items = new List<IValidatableItem>();
            RelativePath = relativePath;
        }

        public IValidatableItem GetItemByRelativePath(string relativePath)
        {
            return Items.First(i => i.RelativePath == relativePath);
        }
    }
}
