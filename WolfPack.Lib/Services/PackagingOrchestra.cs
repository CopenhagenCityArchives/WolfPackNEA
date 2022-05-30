using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WolfPack.Lib.Models;

namespace WolfPack.Lib.Services
{
    public class PackagingOrchestra : IPackagingTask
    {
        static ILog log;
        
        public PackagingTaskSettings Settings { get; set; }
        
        public PackerFactory PackerFactory { get; }

        public IValidator Validator { get; }

        public IEnumerable<Package> Packages { get; set; }

        private long currentSizeInMemory;
        private long counter;

        private readonly IFileSystem fileSystem;

        public event EventHandler<IPackage> PackageCreated;
        public event EventHandler<IEnumerable<IPackage>> AllPackagesCreated;

        public PackagingOrchestra(PackagingTaskSettings settings, IEnumerable<Package> plannedPackages, PackerFactory packerFactory, IValidator validator, IFileSystem _fileSystem = null)
        {
            Settings = settings;
            Packages = plannedPackages;
            Validator = validator;
            PackerFactory = packerFactory;

            currentSizeInMemory = 0;
            counter = 0;

            fileSystem = _fileSystem ?? new FileSystem();

            log = LogManager.GetLogger("PackageOrchestra");
        }

        public bool IsResumable()
        {
            throw new NotImplementedException();
        }

        public void Run()
        {
            log.Info($"Packing and validating using {Settings.PackagingThreads} threads");

            Parallel.ForEach(Packages.OrderBy(p => p.RelativePath), new ParallelOptions { MaxDegreeOfParallelism = Settings.PackagingThreads }, plannedPackage =>
            {
                Interlocked.Increment(ref counter);

                var _packer = PackerFactory.GetPacker();
                _packer.MaxRetries = Settings.MaxRetries;

                if (!fileSystem.Directory.Exists(Settings.Destination))
                {
                    fileSystem.Directory.CreateDirectory(Settings.Destination);
                }

                // Remove destination file if it exists
                var destinationFilePath = Path.Combine(Settings.Destination, plannedPackage.RelativePath + _packer.GetPackageFileExtension());
                if (fileSystem.File.Exists(destinationFilePath))
                {
                    //TODO Overwrite optional?
                    if(ValidatePackageFilesByExistence(plannedPackage, destinationFilePath)) { return; }
                }

                var tempFilePath = Path.Combine(Settings.WorkDirectory, plannedPackage.RelativePath, "_package.tmp");
                Stream PackedStream = CreatePackageStream(plannedPackage, _packer.MaxMemoryStreamSize, tempFilePath);

                // Pack the items
                _packer.PackedItemsStream = PackedStream;
                _packer.PackItemsInStream(plannedPackage);

                if(!ValidatePackageFilesByChecksum(plannedPackage, ref PackedStream))
                {
                    Interlocked.Add(ref currentSizeInMemory, plannedPackage.Size * -2);
                    return;
                }

                if(!SavePackageStreamToDestination(ref PackedStream, tempFilePath, destinationFilePath, plannedPackage.Checksum))
                {
                    throw new Exception("Could not write data to destination file. MaxEntries exceeded.");
                }

                Interlocked.Add(ref currentSizeInMemory, plannedPackage.Size * -2);

                PackageCreated?.Invoke(this, plannedPackage);
            });

            AllPackagesCreated?.Invoke(this, Packages);
        }

        public Stream CreatePackageStream(IPackage plannedPackage, long maxMemorySize, string tempFilePath)
        {
            var tempDirPath = fileSystem.Directory.GetParent(tempFilePath).FullName;

            // Files are used as temp if MemoryLimit is reached, or the files exceeds the maximum memory stream size in the packer
            if (Interlocked.Read(ref currentSizeInMemory) + plannedPackage.Size * 2 > Settings.MemoryLimit || plannedPackage.Size > maxMemorySize)
            {
                log.Info($"Packing {plannedPackage.RelativePath} using temp file");
                if (fileSystem.Directory.Exists(tempDirPath))
                {
                    fileSystem.Directory.Delete(tempDirPath, true);
                }
                fileSystem.Directory.CreateDirectory(tempDirPath);

                //Filestream pointing to a temp file name
                return fileSystem.FileStream.Create(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.SequentialScan);
            }
            else
            {
                log.Info($"Packing {plannedPackage.RelativePath} using memory");
                Interlocked.Add(ref currentSizeInMemory, plannedPackage.Size * 2);

                return new MemoryStream();
            }
        }

        public bool SavePackageStreamToDestination(ref Stream PackedStream, string tempFilePath, string destinationFilePath, byte[] expectedChecksum)
        {
            int retries = 0;

            while (retries < Settings.MaxRetries)
            {
                try
                {
                    // If PackedStream is a File, move it and validate its checksum according to the temp file
                    if (PackedStream.GetType().Equals(typeof(FileStream)))
                    {
                        PackedStream.Close();
                        
                        // Try to move temp file to destination path if it exists
                        if(fileSystem.File.Exists(tempFilePath)) fileSystem.File.Move(tempFilePath, destinationFilePath);
                        
                        using var destinationFileStream = fileSystem.File.Open(destinationFilePath, FileMode.Open, FileAccess.Read);
                        var actualChecksum = Validator.GetChecksum(destinationFileStream);
                        //log.Info($"{string.Join("", expectedChecksum)},{string.Join("", actualChecksum)}");
                        
                        // Compare checksums of the temp file stream and the destination file stream
                        if (Validator.CompareChecksums(expectedChecksum, actualChecksum))
                        {
                            fileSystem.Directory.Delete(fileSystem.Directory.GetParent(tempFilePath).FullName, true);
                            return true;
                        }

                        retries++;
                    }
                    // If it is a memory stream, write it to a file, and validate its checksum
                    else
                    {
                        using var fileStream = fileSystem.File.Create(destinationFilePath);
                        PackedStream.Seek(0, SeekOrigin.Begin);
                        PackedStream.CopyTo(fileStream);
                        
                        var actualChecksum = Validator.GetChecksum(fileStream);
                        
                        //Compare checksums of the memory stream and the destination file
                        if (Validator.CompareChecksums(expectedChecksum, actualChecksum))
                        {
                            PackedStream.Dispose();
                            return true;
                        }

                        retries++;
                    }
                }
                catch (Exception e)
                {
                    log.Warn($"Could not save package at destination, retrying. Error: {e.Message}");
                    retries++;
                }
            }

            return false;
        }

        public bool ValidatePackageFilesByChecksum(IPackage plannedPackage, ref Stream PackedStream)
        {
            var _unpacker = PackerFactory.GetPacker();

            _unpacker.PackedItemsStream = PackedStream;

            bool packageHasInvalidItems = false;

            // Unpack the items and validate them
            foreach (var pathAndStream in _unpacker.UnpackItemsFromStream())
            {
                try
                {
                    var plannedItem = plannedPackage.GetItemByRelativePath(pathAndStream.Item1);
                    
                    //Skip the validation if the item is a directory
                    if(plannedItem.IsDirectory)
                    {
                        continue;
                    }

                    if(!Validator.Validate(plannedPackage.GetItemByRelativePath(pathAndStream.Item1), pathAndStream.Item2))
                    {
                        log.Error($"Item {pathAndStream.Item1} in package {plannedPackage.RelativePath} is invalid!");
                        packageHasInvalidItems = true;
                    }
                }
                catch (Exception e)
                {
                    packageHasInvalidItems = true;
                    log.Warn($"Could not validate item: " + e.Message);
                }
            }

            if (!packageHasInvalidItems)
            {
                log.Info($"Items in package {plannedPackage.RelativePath} are valid");
                plannedPackage.Checksum = Validator.GetChecksum(PackedStream);
                return true;
            }
            else
            {
                log.Warn($"Package {plannedPackage.RelativePath} has invalid items and validation is skipped");
                return false;
            }
        }

        public bool ValidatePackageFilesByExistence(IPackage plannedPackage, string destinationFilePath)
        {
            log.Info($"Package {plannedPackage.RelativePath} already exists, validating its contents with the package plan");

            var _packageEntriesGetter = PackerFactory.GetPacker();
            _packageEntriesGetter.PackedItemsStream = new FileStream(destinationFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

            var filesInPackage = _packageEntriesGetter.GetRelativePathsForEntries().ToList();
            _packageEntriesGetter.PackedItemsStream.Dispose();
            _packageEntriesGetter = null;

            List<string> filesInPlannedPackage = new List<string>();

            foreach (var item in plannedPackage.Items)
            {
                filesInPlannedPackage.Add(item.RelativePath);
            }

            var plannedNotInZip = filesInPlannedPackage.Except(filesInPackage);
            var zipFilesNotInPackage = filesInPackage.Except(filesInPlannedPackage);


            if (!plannedNotInZip.Any() && !zipFilesNotInPackage.Any())
            {
                log.Info($"Package {plannedPackage.RelativePath} already exists, content equals planned content, skipping the package");
                plannedPackage.Checksum = Validator.GetChecksum(destinationFilePath);
                return true;
            }
            else
            {
                log.Warn($"Package {plannedPackage.RelativePath} already exists, content does not equal planned content, deleting the package");
                fileSystem.File.Delete(destinationFilePath);
                return false;
            }
        }
    }
}
