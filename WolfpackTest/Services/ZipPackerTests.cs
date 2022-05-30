using NUnit.Framework;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using WolfPack.Lib.Services;

namespace WolfpackTest
{
    public class ZipPackerTests
    {
        [Test]
        public void PackAndUnPackItem()
        {
            // Mock file system and file
            var _fileSystem = new MockFileSystem();
            var fileData = new MockFileData("test");
            _fileSystem.AddFile(@"c:\testfile", fileData);

            var packer = new ZipPacker("test", _fileSystem);

            using (var memoryStream = new MemoryStream())
            {
                var package = new Package("package.zip");
                var item = new PrioritizableValidatableItem();
                item.RelativePath = "testfile";
                item.AbsolutePath = @"c:\testfile";
                package.Items.Add(item);
                packer.PackedItemsStream = memoryStream;
                packer.PackItemsInStream(package);
                
                var items = packer.UnpackItemsFromStream().ToList();
                Assert.AreEqual(items[0].Item1, item.RelativePath);

                StreamReader sr = new StreamReader(items[0].Item2);
                Assert.AreEqual(fileData.Contents, new MockFileData(sr.ReadToEnd()).Contents);
            }
        }

        [Test]
        public void PackAndUnPackDirectory()
        {
            // Mock file system and directory
            var _fileSystem = new MockFileSystem();
            _fileSystem.AddDirectory(@"c:\testDir");

            var packer = new ZipPacker("test", _fileSystem);

            using (var memoryStream = new MemoryStream())
            {
                var package = new Package("package.zip");
                var item = new PrioritizableValidatableItem();
                item.RelativePath = @".\testDir\";
                item.AbsolutePath = @"c:\testDir\";
                package.Items.Add(item);
                packer.PackedItemsStream = memoryStream;
                packer.PackItemsInStream(package);

                var items = packer.UnpackItemsFromStream().ToList();
                Assert.AreEqual(item.RelativePath, items[0].Item1);
            }
        }

        [Test]
        public void GetEntriesReturnAllFilesAndDirectories()
        {
            // Mock file system and file
            var _fileSystem = new MockFileSystem();
            var fileData = new MockFileData("test");
            _fileSystem.AddFile(@"c:\testfile", fileData);
            _fileSystem.AddDirectory(@"c:\testDir");

            var packer = new ZipPacker("test", _fileSystem);

            using (var memoryStream = new MemoryStream())
            {
                var package = new Package("package.zip");
                var fileItem = new PrioritizableValidatableItem();
                fileItem.RelativePath = "testfile";
                fileItem.AbsolutePath = @"c:\testfile";
                package.Items.Add(fileItem);

                var dirItem = new PrioritizableValidatableItem();
                dirItem.RelativePath = @"testDir\";
                dirItem.AbsolutePath = @"c:\testDir\";
                package.Items.Add(dirItem);


                packer.PackedItemsStream = memoryStream;
                packer.PackItemsInStream(package);

                var items = packer.UnpackItemsFromStream().ToList();
                Assert.AreEqual(items[0].Item1, fileItem.RelativePath);
                Assert.AreEqual(items[1].Item1, dirItem.RelativePath);

                var allEntries = packer.GetRelativePathsForEntries();

                Assert.AreEqual(2, allEntries.Count());
            }
        }
    }
}
