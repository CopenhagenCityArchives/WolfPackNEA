using Moq;
using NEA.ArchiveModel;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using WolfPack.Lib.Services;
using WolfPackNEA;
using WolfPackNEA.Lib.Services;

namespace WolfpackTest
{
    public class ItemFeeder1007Tests
    {
        [Test]
        [TestCase(@"avid.ksa.1.1\fileIndex.xml", PackPriority.FirstPackage)]
        [TestCase(@"avid.ksa.1.1\contextDocumentationIndex.xml", PackPriority.FirstPackage)]
        [TestCase(@"avid.ksa.1.1\archiveIndex.xml", PackPriority.FirstPackage)]
        [TestCase(@"avid.ksa.1.1\contextDocumentationIndex.xml", PackPriority.FirstPackage)]
        [TestCase(@"avid.ksa.1.1\ContextDocumentation\docCollection\1\1.tif", PackPriority.FirstPackage)]
        [TestCase(@"avid.ksa.1.1\Schemas\standard\archiveIndex.xsd", PackPriority.FirstPackage)]
        [TestCase(@"avid.ksa.1.1\Tables\table1\table1.xml", PackPriority.High)]
        [TestCase(@"avid.ksa.1.1\Documents\docCollection\1\1.tif", PackPriority.Low)]
        public void GetFiles_FromArchiveversion_ReturnPrioritySizeAndChecksum(string fileName, PackPriority priority)
        {
            // Mock file system and file
            var _fileSystem = new MockFileSystem();
            var fileData = new MockFileData("test");
            _fileSystem.AddFile(@"\C\AVID.KSA.1\AVID.KSA.1.1\fileIndex.xml", fileData);
            _fileSystem.AddFile(fileName, fileData);

            // Create media
            var medias = new Dictionary<string, string>();
            medias.Add("AVID.KSA.1.1", @"\C\AVID.KSA.1\AVID.KSA.1.1");

            //Create mock archiveversion
            var avInfo = new ArchiveVersionInfo("AVID.KSA.1", medias, AVRuleSet.BKG1007);
            var av = new Mock<ArchiveVersion>(avInfo, _fileSystem);

            // Create mock checksum dict containing the file and a checksum
            var checksumDict = new Dictionary<string, byte[]>();
            var checksum = new byte[] { 34, 34, 34 };
            checksumDict.Add(fileName, checksum);
            av.Setup(av => av.GetChecksumDict(null)).Returns(checksumDict);
            av.Setup(av => av.GetAbsolutePath(fileName)).Returns(fileName);
            av.Setup(av => av.FileIndexPath).Returns(@"\C\AVID.KSA.1\AVID.KSA.1.1\fileIndex.xml");

            var itemFeeder = new WolfPackNEA.Lib.Services.ItemFeeder1007(av.Object, _fileSystem);
            var items = itemFeeder.GetItems().ToList();

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(priority, items[0].Priority);
            Assert.AreEqual(checksum, items[0].Checksum);
            Assert.IsTrue(items[0].Size > 0);
        }

        [Test]
        public void AlwaysReturnFileIndexAndSchemasLocalSharedFolder()
        {
            // Mock file system and file
            var _fileSystem = new MockFileSystem();
            var fileData = new MockFileData("test");
            _fileSystem.AddFile(@"\C\AVID.KSA.1\AVID.KSA.1.1\fileIndex.xml", fileData);

            // Create media
            var medias = new Dictionary<string, string>();
            medias.Add("AVID.KSA.1.1", @"\C\AVID.KSA.1\AVID.KSA.1.1");

            //Create mock archiveversion
            var avInfo = new ArchiveVersionInfo("AVID.KSA.1", medias, AVRuleSet.BKG1007);
            var av = new Mock<ArchiveVersion>(avInfo, _fileSystem);

            // Create mock checksum dict containing the file and a checksum
            var checksumDict = new Dictionary<string, byte[]>();
            av.Setup(av => av.GetChecksumDict(null)).Returns(checksumDict);
            av.Setup(av => av.FileIndexPath).Returns(@"\C\AVID.KSA.1\AVID.KSA.1.1\fileIndex.xml");

            var itemFeeder = new ItemFeeder1007(av.Object, _fileSystem);
            var items = itemFeeder.GetItems().ToList();

            Assert.IsTrue(items.Count == 2);
            Assert.AreEqual(@"AVID.KSA.1.1\Schemas\localShared\", items[0].RelativePath);
            Assert.AreEqual(null, items[0].Checksum);

            Assert.AreEqual(@"AVID.KSA.1.1\fileIndex.xml", items[1].RelativePath);
            Assert.AreNotEqual(null, items[1].Checksum);
        }
    }
}
