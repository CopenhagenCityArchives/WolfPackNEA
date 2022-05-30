using System;
using System.Collections.Generic;
using System.Text;

namespace WolfPack.Lib.Models
{
    public class FileFolderContent
    {
        [CsvHelper.Configuration.Attributes.Name("relativePath")]
        public string RelativePath { set; get; }
        public string MD5Checksum { set; get; }
        [CsvHelper.Configuration.Attributes.Name("size")]
        public double Size { set; get; }
    }
}
