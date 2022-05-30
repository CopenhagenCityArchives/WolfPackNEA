using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace WolfPack.Lib.Services
{
    public class PackagePlanner : IPackagePlanner
    {
        static ILog log;
        public List<Package> PlannedPackages { get; set; }
        public long MaxSize { get; set; }
        private readonly IItemFeeder _itemFeeder;
        private List<PrioritizableValidatableItem> Items;

        private string PackagePrefix;
        private IFileSystem _fileSystem;
        private string _destination;

        public PackagePlanner(long maxSize, string destination, IItemFeeder itemFeeder, ILog _log, string packagePrefix = null, IFileSystem fileSystem = null)
        {
            PlannedPackages = new List<Package>();
            MaxSize = maxSize;
            PackagePrefix = packagePrefix ?? "";
            _fileSystem = fileSystem ?? new FileSystem();
            _destination = destination;
            log = _log;
            _itemFeeder = itemFeeder;
        }

        public void Plan()
        {
            var items = _itemFeeder.GetItems().ToList();
            var firstPackage = new Package(GetPackagePaddedNumber());
            foreach(var item in items.Where(i => i.Priority.Equals(PackPriority.FirstPackage)).OrderBy(item => item.RelativePath))
            {
                firstPackage.Items.Add(item);
            }

            if (firstPackage.Items.Count > 0) { PlannedPackages.Add(firstPackage);  }
            firstPackage = null;

            IPackage package = null;
            long totalSize = 0;
            foreach (PackPriority packPriority in new PackPriority[] { PackPriority.High, PackPriority.Low })
            {
                foreach (var item in items.Where(i => i.Priority.Equals(packPriority)).OrderBy(item => item.RelativePath))
                {
                    if (package == null) { package = AddPackage(); }

                    if (package.Items.Count > 0 && totalSize + item.Size > MaxSize)
                    {
                        package = AddPackage();
                        totalSize = 0;
                    }

                    totalSize += item.Size;
                    package.Items.Add(item);
                }
            }
        }

        public void SetItems()
        {
            var itemsListPath = Path.Combine(_destination, "sourceItems.csv");
            
            if (_fileSystem.File.Exists(itemsListPath))
            {
                log.Info($"Found items file at {itemsListPath}, loading items from the file");
                var csvReader = new CSVHelper();
                Items = csvReader.LoadItems<PrioritizableValidatableItem>(itemsListPath).ToList();
                csvReader = null;
            }
            else
            {
                log.Info($"Getting items from FileFeeder");
                Items = _itemFeeder.GetItems().ToList();
                log.Info($"Items retrieved, saving them at {itemsListPath}");
                var csvWriter = new CSVHelper();
                csvWriter.SaveItems(itemsListPath, Items);
                csvWriter = null;
            }

            log.Info($"Found {Items.Count} items");
        }

        public List<Package> SetPlannedPackages()
        {
            if (!LoadItemsPackagesMapCSV())
            {
                log.Info($"Planning packages");
                Plan();
                WriteItemsPackagesMapCSV();
            }
            else
            {
                log.Info($"Loaded package plan from file, continuing");
            }

            Items = null;
            return PlannedPackages;
        }

        public void WriteItemsPackagesMapCSV()
        {
            string path = Path.Combine(_destination, PackagePrefix + "-filesPackagesMapping.csv");
            log.Info($"Saving items packages map at {path}");
            var csvWriter = new CSVHelperItemsPackagesMapping();
            try
            {
                csvWriter.SaveItemsPackagesMapping(path, PlannedPackages);
            }
            catch (Exception e)
            {
                log.Warn("Could not update filesPackagesMapping.csv. Error: " + e.Message);
            }
        }

        public bool LoadItemsPackagesMapCSV()
        {
            string path = Path.Combine(_destination, "filesPackagesMapping.csv");

            if (!_fileSystem.File.Exists(path))
            {
                return false;
            }

            log.Info($"Loading items packages map at {path}");
            var csvLoader = new CSVHelperItemsPackagesMapping();
            try
            {
                PlannedPackages = csvLoader.LoadItemsPackagesMapping(path).ToList();
                if (PlannedPackages.Count == 0) { return false; }
                return true;
            }
            catch (Exception e)
            {
                log.Warn("Could not load filesPackagesMapping.csv. Error: " + e.Message);
            }

            return false;
        }

        private Package AddPackage()
        {
            var packageFileName = GetPackagePaddedNumber();
            var package = new Package(packageFileName);
            PlannedPackages.Add(package);
            return package;
        }

        private string GetPackagePaddedNumber()
        {
            var prefix = PackagePrefix != null ? $"{PackagePrefix}_" : "";
            
            var packageName = (PlannedPackages.Count + 1).ToString().PadLeft(8, '0');

            return prefix + packageName;
        }
    }
}
