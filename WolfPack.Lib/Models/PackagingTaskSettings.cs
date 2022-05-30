using System;
using System.Collections.Generic;
using System.Text;

namespace WolfPack.Lib.Services
{
    public class PackagingTaskSettings
    {
        public string Destination { get; set; }
        public string Source { get; set; }
        public string WorkDirectory { get; set; }
        public string EncryptionType { get; set; }
        public string EncryptionPassPhrase { get; set; }
        public string EncryptionPassPhraseFile { get; set; }
        public long MaxPackageSize { get; set; }
        public int PackagingThreads { get; set; }
        public long MemoryLimit { get; set; }
        public int MaxRetries { get; set; }
        public string FilePrefix { get; set; } = "";
        public string ChecksumAlgorithm { get; set; } = "MD5";

        public IEnumerable<string> ToStrings()
        {
            yield return $"Source: {Source}";
            yield return $"WorkDirectory: {WorkDirectory}";
            yield return $"Destination: {Destination}";
            yield return $"ChecksumAlgorithm: {ChecksumAlgorithm}";
            yield return $"EncryptionType: {EncryptionType}";
            yield return $"EncryptionPassPhraseFile: {EncryptionPassPhraseFile}";
            yield return $"EncryptionPassPhrase: ****";
            yield return $"MaxPackageSize: {MaxPackageSize}";
            yield return $"PackagingThreads: {PackagingThreads}";
            yield return $"MemoryLimit: {MemoryLimit}";
            yield return $"MaxRetries: {MaxRetries}";
        }
    }
}
