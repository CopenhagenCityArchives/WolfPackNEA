using NUnit.Framework;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using WolfPack.Lib.Services;

namespace WolfpackTest
{
    public class MD5ValidatorTests
    {
        [Test]
        public void ValidateItemWithFileAndCorrectChecksum_ReturnTrue()
        {
            // Mock file system and file
            var _fileSystem = new MockFileSystem();
            var fileData = new MockFileData("test");
            _fileSystem.AddFile("testfile", fileData);

            var validator = new MD5Validator(_fileSystem);


            var item = new PrioritizableValidatableItem();
            item.Checksum = validator.GetChecksum("testfile");

            Assert.IsTrue(validator.Validate(item, "testfile"));
        }

        [Test]
        public void ValidateItemWithWithStreamAndCorrectChecksum_ReturnTrue()
        {
            // Mock file system and file
            var _fileSystem = new MockFileSystem();
            var fileData = new MockFileData("test");
            _fileSystem.AddFile("testfile", fileData);

            var validator = new MD5Validator(_fileSystem);


            var item = new PrioritizableValidatableItem();
            item.Checksum = validator.GetChecksum("testfile");

            var testFileAsStream = _fileSystem.FileStream.Create("testfile", FileMode.Open);

            Assert.IsTrue(validator.Validate(item, testFileAsStream));
        }

        [Test]
        public void ValidateStream_WithSameStreamsAtDifferentPositions_ReturnCorrectChecksum()
        {
            var testValue = "test stream";

            var streamAtBeginning = GenerateStreamFromString(testValue);
            streamAtBeginning.Seek(0, SeekOrigin.Begin);

            var streamAtEnd = GenerateStreamFromString(testValue);
            streamAtEnd.Seek(0, SeekOrigin.End);

            var validator = new MD5Validator();

            Assert.AreEqual(validator.GetChecksum(streamAtBeginning), validator.GetChecksum(streamAtEnd));
        }

        private Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
