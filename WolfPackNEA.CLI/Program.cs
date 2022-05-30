using System.CommandLine;
using WolfPack.Lib.Services;
using WolfPackNEA.Lib;

namespace WolfPack.CLI
{
    class Program
    {

        static int Main(string[] args)
        {
            var destination = new Argument<string>("--destination", "The destination folder");
            var source = new Argument<string>("--source", "The source location");
            var workDir = new Argument<string>("--work-dir", "The temp directory where packaging and validation is done");
            var encryptionPassPhraseFile = new Argument<string>("--pass", "Passphrase file used for packages");

            var encryptionType = new Option<string>(new[] { "--encryption", "-e" }, getDefaultValue: () => "aes256", "The encryption method. Only AES256 is supported at the moment");
            var maxPackageSize = new Option<long>(new[] { "--size", "-s" }, getDefaultValue: () => 1, "Maximum size of zip files in gigabytes. Prioritized files and files larger than this value will result in larger zip files than this value");
            var maxPackagingThreads = new Option<int>(new[] { "--threads", "-t" }, getDefaultValue: () => 8, "Maximum number of threads used when packing");
            var maxMemory = new Option<long>(new[] { "--memory", "-m" }, getDefaultValue: () => 8, "Maximum memory in gigabytes used for packing and validating");

            var rootCommand = new RootCommand()
            {

            };

            var packCommand = new Command("pack")
            {
                source,
                workDir,
                destination,
                encryptionPassPhraseFile,
                encryptionType,
                maxPackageSize,
                maxPackagingThreads,
                maxMemory
            };

            packCommand.SetHandler(
                (string source, string workDir, string destination, string encryptionPassPhraseFile, string encryptionType, long maxPackageSize, int maxPackagingThreads, long maxMemory) =>
                {

                    var settings = new PackagingTaskSettings
                    {
                        Source = source,
                        Destination = destination,
                        WorkDirectory = workDir,
                        EncryptionType = encryptionType,
                        EncryptionPassPhraseFile = encryptionPassPhraseFile,
                        MaxPackageSize = maxPackageSize * 1024 * 1024 * 1024,
                        PackagingThreads = maxPackagingThreads,
                        MemoryLimit = maxMemory * 1024 * 1024 * 1024,
                        MaxRetries = 3
                    };

                    PlanPackAndValidate(settings);
                },
                source, workDir, destination, encryptionPassPhraseFile, encryptionType,  maxPackageSize, maxPackagingThreads, maxMemory
            );

            rootCommand.AddCommand(packCommand);

            // Parse the incoming args and invoke the handler
            return rootCommand.Invoke(args);

        }

        private static void PlanPackAndValidate(PackagingTaskSettings settings)
        {
            var wolfPackService = new WolfPackNEAService(settings);
            wolfPackService.Pack();
        }
    }
}
