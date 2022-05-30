using Microsoft.VisualStudio.TestTools.UnitTesting;
using NEA.ArchiveModel;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace NEA.Testing.ArchiveModel
{
    [TestClass]
    public class ArchiveVersion1007GetFilesTest
    {
        private MockFileSystem _fileSystem;

        private ArchiveVersion1007 GetArchiveVersion()
        {
            ArchiveVersionInfo avInfo = new ArchiveVersionInfo("AVID.KSA.1", new Dictionary<string, string>(), AVRuleSet.BKG1007);
            avInfo.Medias.Add("AVID.KSA.1.1", @"C:\AVID.KSA.1.1");
            var av = new ArchiveVersion1007(avInfo, _fileSystem);

            return av;
        }

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
        public void GetFilesInOrdinaryFolders()
        {
            List<string> files = new List<string> {
                 @"C:\AVID.KSA.1.1\Documents\docCollection1\1\1.tif",
                 @"C:\AVID.KSA.1.1\ContextDocumentation\docCollection1\1\1.tif",
                 @"C:\AVID.KSA.1.1\Tables\table1.xml",
                 @"C:\AVID.KSA.1.1\Tables\table1.xsd",
                 @"C:\AVID.KSA.1.1\Schemas\shared\tableIndex.xsd",
                 @"C:\AVID.KSA.1.1\Indices\tableIndex.xml"
            };

            CreateFilesInFileSystem(files);

            var av = GetArchiveVersion();
            var avFiles = av.GetFiles();

            Assert.AreEqual(files.Count, avFiles.Count);
        }

        [TestMethod]
        public void GetFilesInSpecialFolders()
        {
            List<string> files = new List<string> {
                 @"C:\AVID.KSA.1.1\tmp.log",
                 @"C:\AVID.KSA.1.1\Documents\tmp2.txt",
                 @"C:\AVID.KSA.1.1\Documents\docCollection1\test.tif",
                 @"C:\AVID.KSA.1.1\Documents\docCollection1\1\tmp\test.tif"
            };

            CreateFilesInFileSystem(files);

            var av = GetArchiveVersion();
            var avFiles = av.GetFiles();

            Assert.AreEqual(files.Count, avFiles.Count);
        }
    }
}
