namespace WolfPack.Lib.Services
{
    public interface IValidatableItem
    {
        string RelativePath { get; set;  }
        public string AbsolutePath { get; set; }
        byte[] Checksum { get; set; }
        long Size { get; set; }
        bool IsDirectory { get; }
    }
}