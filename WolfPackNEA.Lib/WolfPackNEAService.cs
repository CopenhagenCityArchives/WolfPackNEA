using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WolfPack.Lib.Helpers;
using WolfPack.Lib.Models;
using WolfPack.Lib.Services;
using WolfPackNEA.Lib.Services;

namespace WolfPackNEA.Lib
{
    public class WolfPackNEAService
    {
        private ILog log;
        private PackagingTaskSettings Settings { get; set; }
        private long PackagesCreated = 0;
        private PackagingOrchestra packagingOrchestrator;
        
        public WolfPackNEAService(PackagingTaskSettings settings)
        {
            Settings = settings;
        }

        public void Pack()
        {
            var logName = $"WolfPack-{ DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
            var logPath = Path.Combine(Settings.Destination, logName);
            Initconfig(logPath);

            LogAllInfo(Settings);

            log = LogManager.GetLogger("PackageOrchestra");

            log.Info("Load passphrase from filepath: " + Settings.EncryptionPassPhraseFile);
            Settings.EncryptionPassPhraseFile = PassPhrasePeriod.LoadLatestPassPhraseFileFromCSVFile(Settings.EncryptionPassPhraseFile);

            var itemFeeder = new ItemFeeder1007(Settings.Source);
            Settings.Destination = Path.Combine(Settings.Destination, itemFeeder.Archiveversion.Info.Id);

            var validator = new MD5Validator();

            // Save items to fileFolderContent file
            // TODO If this file exists, all files and folders should be loaded from it using an ItemFeeder, right?
            var fileName = $"fileFolderContent_v1_{itemFeeder.Archiveversion.Info.Id}.csv";
            var filePath = Path.Combine(Settings.Destination, fileName);

            log.Info($"Creating fileFolderContent file: {filePath}");

            var csvHelper = new CSVHelper();
            csvHelper.SaveItems<FileFolderContent>(filePath, itemFeeder.GetItems().Select(i => new FileFolderContent() { RelativePath = i.RelativePath, MD5Checksum = i.ChecksumAsString, Size = i.Size }).AsEnumerable());

            // Add saved file to items
            log.Info($"Adding fileFolderContent file to items");
            itemFeeder.AddItem(filePath, fileName, validator.GetChecksum(filePath), PackPriority.FirstPackage);

            var packagePlanner = new PackagePlanner(Settings.MaxPackageSize, Settings.Destination, itemFeeder, log, itemFeeder.Archiveversion.Info.Id);
            packagePlanner.SetItems();
            var plannedPackages = packagePlanner.SetPlannedPackages();

            var packerFactory = new ZipPackerFactory(Settings.EncryptionPassPhraseFile, null);

            Settings.FilePrefix = itemFeeder.Archiveversion.Info.Id + "-";

            packagingOrchestrator = new PackagingOrchestra(Settings, plannedPackages, packerFactory, validator);
            packagingOrchestrator.Settings = Settings;

            packagingOrchestrator.PackageCreated += OnPackageCreated;

            packagingOrchestrator.Run();
            packagePlanner.WriteItemsPackagesMapCSV();
            log.Info("All done");

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            logRepository.Shutdown();

            // Moving log file to a file containing the archiveversion id
            File.Move(logPath, Path.Combine(Settings.Destination, itemFeeder.Archiveversion.Info.Id + "-" + logName));
        }

        public async Task PackAsync()
        {
            await Task.Run(() => Pack());
        }

        private void OnPackageCreated(object sender, IPackage package)
        {
            Interlocked.Increment(ref PackagesCreated);

            if (Interlocked.Read(ref PackagesCreated) % Settings.PackagingThreads == 0)
            {
                CreateUpdatePackagesCSV();
            }
        }

        private void CreateUpdatePackagesCSV()
        {
            string path = Path.Combine(Settings.Destination, Settings.FilePrefix + "packages.csv");
            var csvWriter = new CSVHelper();
            log.Info($"Saving packages list at {path}");
            try
            {
                csvWriter.SaveItems<Package>(path, packagingOrchestrator.Packages);
            }
            catch (Exception e)
            {
                log.Warn("Could not update packages.csv: " + e.Message);
            }
        }

        private void Initconfig(string logPath)
        {
            // Load configuration
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.GlobalContext.Properties["LogFileName"] = logPath;
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }

        private void LogAllInfo(PackagingTaskSettings settings)
        {
            var log = LogManager.GetLogger("PackageOrchestra");

            log.Info("Running WolfPack with the following settings");
            foreach (var info in settings.ToStrings())
            {
                log.Info(info);
            }

            log.Info(" ---- Software, packages and environment info ----");

            log.Info("Software version details");
            foreach (var info in VersionControlInfoHelper.GetInfo())
            {
                log.Info(info);
            }

            log.Info("Software packages and dependencies");
            foreach (var info in AssemblyInfoHelper.GetInfo())
            {
                log.Info(info);
            }

            log.Info("Environmental info");
            foreach (var info in EnvironmentInfoHelper.GetInfo())
            {
                log.Info(info);
            }

            log.Info("---- End of software, packages and environment info ----");
        }
    }
}
