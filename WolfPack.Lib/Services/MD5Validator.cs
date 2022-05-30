using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WolfPack.Lib.Services
{
    public class MD5Validator : IValidator
    {
        public readonly IFileSystem _fileSystem;
        public MD5Validator(IFileSystem fileSystem = null)
        {
            _fileSystem = fileSystem ?? new FileSystem();
        }

        public byte[] GetChecksum(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
                return md5.ComputeHash(stream);
            }
        }

        public byte[] GetChecksum(string filePath)
        {
            using (var stream = _fileSystem.FileStream.Create(filePath, FileMode.Open))
            {
                return GetChecksum(stream);
            }
        }

        public static string GetChecksumAsString(byte[] checksum)
        {
            return BitConverter.ToString(checksum).Replace("-", "");
        }

        public static byte[] GetChecksumFromString(string checksumStr)
        {
            return Encoding.UTF8.GetBytes(checksumStr);
        }

        public bool Validate(IValidatableItem item, Stream itemStream)
        {
            if (item.Checksum == null)
            {
                throw new ArgumentException("Cannot validate item: Expected checksum is null");
            }

            using (var md5 = MD5.Create())
            {
                using (var stream = itemStream)
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    return CompareChecksums(item.Checksum, md5.ComputeHash(stream));
                }
            }
        }

        public bool Validate(IValidatableItem item, string filePath)
        {
            using (var stream = _fileSystem.FileStream.Create(filePath, FileMode.Open))
            {
                return Validate(item, stream);
            }
        }

        public bool CompareChecksums(Stream stream1, Stream stream2)
        {
            var checksum1 = GetChecksum(stream1);
            var checksum2 = GetChecksum(stream2);

            return CompareChecksums(checksum1, checksum2);
        }

        public bool CompareChecksums(byte[] checksum1, byte[] checksum2)
        {
            return checksum1.SequenceEqual(checksum2);
        }
    }
}
