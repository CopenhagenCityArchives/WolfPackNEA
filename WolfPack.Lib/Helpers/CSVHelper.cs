using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WolfPack.Lib.Services
{
    public class CSVHelper
    {
        public List<T> LoadItems<T>(string itemsFilePath) 
        {
            //var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            //config.Delimiter = ",";
            using (var reader = new StreamReader(itemsFilePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<T>();
                    return records.ToList();
                }
            }
        }
        public void SaveItems<T>(string itemsFilePath, IEnumerable<T> items)
        {
            var destinationDir = Directory.GetParent(itemsFilePath).FullName;
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            using (var writer = new StreamWriter(itemsFilePath, false, System.Text.Encoding.UTF8))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(items);
                }
            }
        }
    }
}