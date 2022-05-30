using Microsoft.VisualStudio.TestTools.UnitTesting;
using NEA.ArchiveModel;
using System.Collections.Generic;

namespace NEA.Testing.ArchiveModel
{
    [TestClass]
    public class ArchiveVersion1007Test
    {
        [TestMethod]
        public void GetRelativePathFromAbsolutePath()
        {
            Dictionary<string, string> AbsolutePaths = new Dictionary<string, string>();
            AbsolutePaths.Add(@"C:\AVID.KSA.1.1\Indices\tableIndex.xml", @"AVID.KSA.1.1\Indices\tableIndex.xml");
            AbsolutePaths.Add(@"C:\AVID.KSA.1\AVID.KSA.1.1\Indices\tableIndex.xml", @"AVID.KSA.1.1\Indices\tableIndex.xml");
            AbsolutePaths.Add(@"C:\AVID.KSA.1\AVID.KSA.1\AVID.KSA.1.1\Indices\tableIndex.xml", @"AVID.KSA.1.1\Indices\tableIndex.xml");


            foreach (var path in AbsolutePaths)
            {
                ArchiveVersionInfo avInfo = new ArchiveVersionInfo("AVID.KSA.1", new Dictionary<string, string>(), AVRuleSet.BKG1007);
                avInfo.Medias.Add("AVID.KSA.1.1", @"C:\");
                ArchiveVersion av = new ArchiveVersion1007(avInfo);

                Assert.AreEqual(path.Value, av.GetRelativeFilePath(path.Key));
            }
        }
    }
}
