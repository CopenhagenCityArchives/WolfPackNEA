using Moq;
using NEA.ArchiveModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using WolfPack.Lib.Models;
using WolfPack.Lib.Services;

namespace WolfpackTest
{
    public class PackageOrchestraTests
    {
        [Test]
        public void PlanPackage_Item_ReturnSinglePackage()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\myfile.txt", new MockFileData("Testing is meh.") },
                { @"c:\demo\image.gif", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) }
            });

            // Packages
            var package = new Package("relativePath");
            var item = new PrioritizableValidatableItem()
            {
                RelativePath = "myfile.txt",
                Checksum = null,
                Size = 100
            };
            package.Items.Add(item);
            var packages = new List<Package>() {  };
            packages.Add(package);

            var settings = GetSettings();

            Assert.IsFalse(fileSystem.Directory.Exists(settings.Destination));

            var po = new PackagingOrchestra(settings, packages, GetPackerFactory(), GetValidator(true), fileSystem);
            po.Run();

            Assert.IsTrue(fileSystem.Directory.Exists(settings.Destination));
            Assert.IsTrue(fileSystem.File.Exists(Path.Combine(settings.Destination, package.RelativePath + ".zip")));
        }

        private PackagingTaskSettings GetSettings()
        {
            var settings = new PackagingTaskSettings()
            {
                MaxRetries = 3,
                MaxPackageSize = 100,
                PackagingThreads = 1,
                Destination = "D:\\destination",
                WorkDirectory = "D:\\work-dir",
                Source = "c:\\"
            };

            return settings;
        }

        private PackerFactory GetPackerFactory()
        {
            // ZipPacker
            var zipPacker = new Mock<IPacker>();
            zipPacker.Setup(zp => zp.GetPackageFileExtension()).Returns(".zip");
            var packerFactory = new Mock<PackerFactory>();
            packerFactory.Setup(p => p.GetPacker()).Returns(zipPacker.Object);

            return packerFactory.Object;
        }

        private IValidator GetValidator(bool checksumResult)
        {
            // Validator
            var validator = new Mock<IValidator>();
            validator.Setup(v => v.CompareChecksums(It.IsAny<Stream>(), It.IsAny<Stream>())).Returns(checksumResult);
            validator.Setup(v => v.CompareChecksums(It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(checksumResult);
            return validator.Object;
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void SavePackageStreamToDestination_ReturnResultBasedOnValidationResult(bool checksumResult, bool expected)
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6 };
            Stream memoryStream = new System.IO.MemoryStream(bytes);

            // Let the validator always return false
            var validator = GetValidator(checksumResult);

            var fileSystem = new MockFileSystem();

            var po = new PackagingOrchestra(GetSettings(), null, GetPackerFactory(), validator, fileSystem);
            Assert.AreEqual(expected, po.SavePackageStreamToDestination(ref memoryStream, "C:\\tempFile", "c:\\destinationFile", null));
        }

        [Test]
        public void SavePackageStreamToDestination_FileDestinationCreationException_ReturnsFalse()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6 };
            Stream memoryStream = new System.IO.MemoryStream(bytes);

            // Let the validator always return true
            var validator = GetValidator(true);

            var fileSystemMock = new Mock<MockFileSystem>();
            var fileMock = new Mock<IFileSystem>();
            fileMock.Setup(f => f.File.Create(It.IsAny<string>())).Throws<Exception>();

            var po = new PackagingOrchestra(GetSettings(), null, GetPackerFactory(), validator, fileMock.Object);
            Assert.IsFalse(po.SavePackageStreamToDestination(ref memoryStream, "C:\\tempFile", "c:\\destinationFile", null));
        }

        [Test]
        public void SavePackageStreamToDestination_FileDestinationCreationExceptionFirstTwoTimes_ReturnsTrue()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6 };
            Stream memoryStream = new System.IO.MemoryStream(bytes);

            // Let the validator always return true
            var validator = GetValidator(true);
            
            var fileSystemMock = new Mock<IMockFileDataAccessor>();

            fileSystemMock.SetupSequence(f => f.File.Create(It.IsAny<string>()))
                .Throws<Exception>()
                .Throws<Exception>()
                .Returns(new MockFileStream(fileSystemMock.Object,  @"c:\destinationFile", FileMode.Create));
            //TODO: Add functionality that actually creates a file in third try

            var po = new PackagingOrchestra(GetSettings(), null, GetPackerFactory(), validator, fileSystemMock.Object);
            Assert.IsTrue(po.SavePackageStreamToDestination(ref memoryStream, "C:\\tempFile", "c:\\destinationFile", null));
        }

        [Test]
        public void SavePackageStreamToDestination_MemoryStream_NoValidationErrrors_CreateDestinationFile()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6 };
            Stream memoryStream = new System.IO.MemoryStream(bytes);

            var fileSystemMock = new MockFileSystem();

            // Let the validator always return true
            var validator = GetValidator(true);

            var destinationFilePath = "c:\\destinationFile";

            var po = new PackagingOrchestra(GetSettings(), null, GetPackerFactory(), validator, fileSystemMock);
            po.SavePackageStreamToDestination(ref memoryStream, "C:\\tempFile", destinationFilePath, null);
            
            Assert.IsTrue(fileSystemMock.File.Exists(destinationFilePath), "must create destination file");

            Assert.AreEqual(bytes, fileSystemMock.File.ReadAllBytes(destinationFilePath), "destination file content must match incomming stream");
        }

        [Test]
        public void SavePackageStreamToDestination_FileStream_NoValidationErrrors_CreateDestinationFile()
        {
            var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"c:\inputFile", new MockFileData(new byte[] { 1, 2, 3, 4, 5, 6 }) }
            });

            byte[] bytes = { 1, 2, 3, 4, 5, 6 };
            Stream fileStream = fileSystemMock.File.OpenRead(@"c:\inputFile");

            // Let the validator always return true
            var validator = GetValidator(true);

            var destinationFilePath = "c:\\destinationFile";

            var po = new PackagingOrchestra(GetSettings(), null, GetPackerFactory(), validator, fileSystemMock);
            po.SavePackageStreamToDestination(ref fileStream, "C:\\tempFile", destinationFilePath, null);

            Assert.IsTrue(fileSystemMock.File.Exists(destinationFilePath), "must create destination file");

            Assert.AreEqual(bytes, fileSystemMock.File.ReadAllBytes(destinationFilePath), "destination file content must match incomming stream");
        }
    }
}
