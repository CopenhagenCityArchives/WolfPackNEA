using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WolfPack.Lib.Services;

namespace WolfPack.Lib.Helpers
{
    public class PassPhrasePeriod
    {
        [Ignore]
        public DateTime StartDateTime { get; set; }
        [Ignore]
        public DateTime EndDateTime { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string PassPhrase { get; set; }

        public static string LoadLatestPassPhraseFileFromCSVFile(string passPhraseFile)
        {
            var csvHelper = new CSVHelper();
            var passPhrasePeriods = csvHelper.LoadItems<PassPhrasePeriod>(passPhraseFile);
            foreach(var passPhrasePeriod in passPhrasePeriods)
            {
                passPhrasePeriod.EndDateTime = DateTime.Parse(passPhrasePeriod.EndDate, CultureInfo.InvariantCulture);
                passPhrasePeriod.StartDateTime = DateTime.Parse(passPhrasePeriod.StartDate, CultureInfo.InvariantCulture);
            }

            var currentPassPhrases = passPhrasePeriods.Where(p => p.StartDateTime < DateTime.Now && p.EndDateTime > DateTime.Now);

            if (currentPassPhrases.ToList().Count != 1)
            {
                throw new Exception("Passphrase file does not contain passphrases matching the current date");
            }

            return currentPassPhrases.First().PassPhrase;
        }
    }
}
