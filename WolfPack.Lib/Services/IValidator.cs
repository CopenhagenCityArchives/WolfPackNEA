using System.IO;

namespace WolfPack.Lib.Services
{
    public interface IValidator
    {
        public bool Validate(IValidatableItem item, Stream itemStream);
        public bool Validate(IValidatableItem item, string itemPath);
        public byte[] GetChecksum(Stream stream);
        public byte[] GetChecksum(string itemPath);
        public bool CompareChecksums(Stream stream1, Stream stream2);
        public bool CompareChecksums(byte[] stream1, byte[] stream2);
    }
}