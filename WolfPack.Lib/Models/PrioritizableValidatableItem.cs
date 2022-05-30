using CsvHelper.Configuration.Attributes;

namespace WolfPack.Lib.Services
{

    public class PrioritizableValidatableItem : IPrioritizableValidatableItem
    {
        public string RelativePath { get; set; }
        public PackPriority Priority { get; set; }
        public byte[] Checksum { get; set; }
        public string ChecksumAsString
        {
            get
            {
                if (IsDirectory)
                {
                    return null;
                }
                return MD5Validator.GetChecksumAsString(Checksum);
            }
        }
        public string AbsolutePath { get; set; }
        public long Size { get; set; }
        [Ignore]
        public bool IsDirectory
        {
            get 
            {
                return Checksum == null && Size == 0 && RelativePath.EndsWith("\\");
            }
        }
    }
}
