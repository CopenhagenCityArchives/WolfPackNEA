using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WolfPack.Lib.Services
{
    public class CSVHelperItemsPackagesMapping
    {
        public void SaveItemsPackagesMapping(string filePath, IEnumerable<Package> packages)
        {
            var destinationDir = Directory.GetParent(filePath).FullName;
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<PackageItem>();
                csv.NextRecord();

                foreach (var package in packages)
                {
                    foreach(var item in package.Items)
                    {
                        csv.WriteRecord(new PackageItem(package, item));
                        csv.NextRecord();
                    }
                }
            }
        }

        public IEnumerable<Package> LoadItemsPackagesMapping(string filePath)
        {
            var plannedPackages = new List<Package>();
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<PackageItem>();
                var groups = records.GroupBy(r => r.PackagePath);
                foreach(var group in groups)
                {
                    var package = new Package(group.Key);
                    foreach (var pi in group)
                    {
                        package.Checksum = pi.PackageChecksum != null ? MD5Validator.GetChecksumFromString(pi.PackageChecksum) : null;
                        package.RelativePath = pi.PackagePath;

                        var item = new PrioritizableValidatableItem();
                        item.RelativePath = pi.ItemPath;
                        item.Checksum = pi.ItemChecksum != null ?  MD5Validator.GetChecksumFromString(pi.ItemChecksum) : null;
                        item.Size = pi.ItemSize;
                        item.AbsolutePath = pi.ItemAbsolutePath;
                        package.Items.Add(item);
                    }

                    plannedPackages.Add(package);
                }
            }

            return plannedPackages;
        }
    }
}