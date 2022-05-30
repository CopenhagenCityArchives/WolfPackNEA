using Microsoft.VisualStudio.TestTools.UnitTesting;
using NEA.ArchiveModel;
using NEA.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace NEA.Testing.HelperTests
{
    [TestClass]
    public class ArchiveversionIdentifierTests
    {
        private MockFileSystem _fileSystem;

        private void CreateFilesInFileSystem(List<string> files)
        {
            _fileSystem = new MockFileSystem();

            foreach (var file in files)
            {
                _fileSystem.AddDirectory(Directory.GetParent(file).FullName);
                _fileSystem.AddFile(file, new MockFileData("", Encoding.UTF8));
            }
        }

        [TestMethod]
        public void GetArchiveVersions()
        {
            _fileSystem = new MockFileSystem();
            _fileSystem.AddDirectory(@"D:\AVID.SA.18000.1");
            _fileSystem.AddDirectory(@"D:\AVID.SA.18000.1\Indices");
            _fileSystem.AddDirectory(@"D:\AVID.KSA.1.1");
            _fileSystem.AddDirectory(@"D:\AVID.KSA.1.1\Indices");

            ArchiveVersionIdentifier avid = new ArchiveVersionIdentifier(_fileSystem);
            var avs = avid.GetArchiveVersions(@"D:\");

            Assert.AreEqual(2, avs.Count);
        }

        [TestMethod]
        public void GetMediasForArchiveversions()
        {
            _fileSystem = new MockFileSystem();
            
            // This should be skipped by the ArchiveVersionIdentifier
            _fileSystem.AddDirectory(@"D:\AVID.SA.18000");
            
            // These should be returned
            _fileSystem.AddDirectory(@"D:\AVID.SA.18000.1");
            _fileSystem.AddDirectory(@"D:\AVID.SA.18000.1\Indices");
            _fileSystem.AddDirectory(@"D:\AVID.SA.18000.2");

            ArchiveVersionIdentifier avid = new ArchiveVersionIdentifier(_fileSystem);
            var avs = avid.GetArchiveVersions(@"D:\");

            Assert.AreEqual(1, avs.Count, "expect 1 ArchiveVersion");
            Assert.AreEqual(2, avs[0].Info.Medias.Count, "expect 2 Media folders");
        }
    }
}