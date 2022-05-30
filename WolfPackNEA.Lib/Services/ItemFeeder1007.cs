using NEA.ArchiveModel;
using NEA.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using WolfPack.Lib.Services;

namespace WolfPackNEA.Lib.Services
{
    public class ItemFeeder1007 : IItemFeeder
    {
        protected readonly IFileSystem _fileSystem;
        public ArchiveVersion Archiveversion { get; }
        private BlockingCollection<IPrioritizableItem> Directories { get; }
        private BlockingCollection<PrioritizableValidatableItem> Items { get; set; }
        public ItemFeeder1007(ArchiveVersion av, IFileSystem fileSystem)
        {
            Archiveversion = av;
            _fileSystem = fileSystem ?? new FileSystem();
            Items = new BlockingCollection<PrioritizableValidatableItem>();
        }
        public ItemFeeder1007(string avPath, IFileSystem fileSystem = null)
        {
            var archiveVersionIdentifier = new ArchiveVersionIdentifier();
            var archiveVersions = archiveVersionIdentifier.GetArchiveVersions(avPath);
            if(archiveVersions.Count != 1)
            {
                throw new ArgumentException("Could not find an archiveversion in the given path");
            }

            Items = new BlockingCollection<PrioritizableValidatableItem>();

            Archiveversion = (ArchiveVersion1007) archiveVersions[0];
            _fileSystem = fileSystem ?? new FileSystem();
        }
        public bool CanFeedFromLocation(string location)
        {
            return File.Exists(Archiveversion.FileIndexPath);
        }

        public void AddItem(string absolutePath, string relativePath, byte[] checksum, PackPriority priority)
        {
            if(Items == null)
            {
                throw new NullReferenceException("Cannot add item to non instantiated Items list");
            }

            if (!_fileSystem.File.Exists(absolutePath))
            {
                throw new ArgumentException($"Cannot add item as file does not exist. Path: {absolutePath}");
            }

            var pvi = new PrioritizableValidatableItem();
            pvi.RelativePath = relativePath;
            pvi.AbsolutePath = absolutePath;
            pvi.Size = _fileSystem.FileInfo.FromFileName(pvi.AbsolutePath).Length;
            pvi.Checksum = checksum;
            pvi.Priority = priority;

            Items.Add(pvi);
        }

        public IEnumerable<PrioritizableValidatableItem> GetItems()
        {
            if(Items.Count > 0)
            {
                return Items;
            }

            var checksums = Archiveversion.GetChecksumDict();
           
            Parallel.ForEach(checksums, new ParallelOptions { MaxDegreeOfParallelism = 24 }, file =>
            {
                var pvi = new PrioritizableValidatableItem();
                pvi.RelativePath = file.Key;
                pvi.AbsolutePath = Archiveversion.GetAbsolutePath(file.Key);
                pvi.Size = _fileSystem.FileInfo.FromFileName(pvi.AbsolutePath).Length;
                pvi.Checksum = file.Value;

                pvi.Priority = PackPriority.FirstPackage;
                var lowercase = file.Key.ToLower();

                if (lowercase.IndexOf("\\documents") != -1)
                {
                    pvi.Priority = PackPriority.Low;
                }
                else if (lowercase.IndexOf("\\tables") != -1)
                {
                    pvi.Priority = PackPriority.High;
                }

                Items.Add(pvi);
            });

            // Add AVID.x.1\Schemas\localShared, which is an empty directory
            var dirPvi = new PrioritizableValidatableItem();
            dirPvi.RelativePath = Archiveversion.Info.Id + @".1\Schemas\localShared\";
            dirPvi.Size = 0;
            dirPvi.Checksum = null;
            dirPvi.Priority = PackPriority.FirstPackage;
            dirPvi.AbsolutePath = Path.Combine(Archiveversion.Info.FirstMediaPath, @"Schemas\localShared\");
            Items.Add(dirPvi);
            
            // Add fileIndex.xml, which is not part of the checksum Dictionary
            var md5Validator = new MD5Validator(_fileSystem);
            var pvi = new PrioritizableValidatableItem();
            pvi.RelativePath = Archiveversion.GetRelativeFilePath(Archiveversion.FileIndexPath);
            pvi.AbsolutePath = Archiveversion.FileIndexPath;
            pvi.Size = _fileSystem.FileInfo.FromFileName(pvi.AbsolutePath).Length;
            pvi.Checksum = md5Validator.GetChecksum(pvi.AbsolutePath);
            pvi.Priority = PackPriority.FirstPackage;

            Items.Add(pvi);

            return Items;
        }
    }
}
