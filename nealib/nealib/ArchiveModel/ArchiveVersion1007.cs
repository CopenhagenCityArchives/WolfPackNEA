using NEA.ArchiveModel.BKG1007;
using NEA.Helpers;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace NEA.ArchiveModel
{
    public class ArchiveVersion1007 : ArchiveVersion
    {
        #region public members
        private archiveIndex archiveIndex;
        private contextDocumentationIndex contextDocumentationIndex;
        private docIndexType docIndex;
        private fileIndexType fileIndex;
        private siardDiark tableIndex;

        public archiveIndex ArchiveIndex
        {
            get
            {
                if (archiveIndex == null)
                {
                    LoadArchiveIndex();
                }
                return archiveIndex;
            }
            set => archiveIndex = value;
        }
        public contextDocumentationIndex ContextDocumentationIndex
        {
            get
            {
                if (contextDocumentationIndex == null)
                {
                    LoadContextDocumentationIndex();
                }
                return contextDocumentationIndex;
            }
            set => contextDocumentationIndex = value;
        }
        public docIndexType DocIndex
        {
            get
            {
                if (docIndex == null)
                {
                    LoadDocIndex();
                }
                return docIndex;
            }
            set => docIndex = value;
        }
        public fileIndexType FileIndex
        {
            get
            {
                if (fileIndex == null)
                {
                    LoadFileIndex();
                }
                return fileIndex;
            }
            set => fileIndex = value;
        }
        public siardDiark TableIndex
        {
            get
            {
                if (tableIndex == null)
                {
                    LoadTableIndex();
                }
                return tableIndex;
            }
            set => tableIndex = value;
        }
        #endregion

        private readonly string _indexFolderPath;
        private readonly string _fileIndexPath;

        public ArchiveVersion1007(ArchiveVersionInfo info, IFileSystem fileSystem = null) : base(info, fileSystem)
        {
            _indexFolderPath = $"{Info.Medias[Info.Id + ".1"]}\\Indices";
            _fileIndexPath = _indexFolderPath + "\\fileIndex.xml";
        }
        #region Load
        public override void Load()
        {
            LoadArchiveIndex();
            LoadContextDocumentationIndex();
            LoadDocIndex();
            LoadFileIndex();
            LoadTableIndex();
        }
        public void LoadArchiveIndex()
        {
            using (var stream = _fileSystem.File.OpenRead(_indexFolderPath + "\\archiveIndex.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(archiveIndex));
                ArchiveIndex = (archiveIndex)serializer.Deserialize(stream);
            }
        }
        public void LoadContextDocumentationIndex()
        {
            using (var stream = _fileSystem.File.OpenRead(_indexFolderPath + "\\contextDocumentationIndex.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(contextDocumentationIndex));
                ContextDocumentationIndex = (contextDocumentationIndex)serializer.Deserialize(stream);
            }
        }
        public void LoadDocIndex()
        {
            using (var stream = _fileSystem.File.OpenRead(_indexFolderPath + "\\docIndex.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(docIndexType));
                DocIndex = (docIndexType)serializer.Deserialize(stream);
            }
        }
        public void LoadFileIndex()
        {
            using (var stream = _fileSystem.File.OpenRead(FileIndexPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(fileIndexType));
                FileIndex = (fileIndexType)serializer.Deserialize(stream);
            }
        }
        public void LoadTableIndex()
        {
            using (var stream = _fileSystem.File.OpenRead(_indexFolderPath + "\\tableIndex.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(siardDiark));
                TableIndex = (siardDiark)serializer.Deserialize(stream);
            }
        }
        #endregion
        public override Dictionary<string, byte[]> GetChecksumDict(string regexPattern = null)
        {
            //If the fileindex has allready been loaded into memory we get it from there
            if (fileIndex != null)
            {
                return FileIndex.f.ToDictionary(f => $"{f.foN}\\{f.fiN}", f => f.md5);
            }
            //Otherwise we stream it in from the xml to keep down memory usage
            using (var stream = _fileSystem.FileStream.Create(FileIndexPath, FileMode.Open, FileAccess.Read))
            {
                var fileindex = XDocument.Load(stream);
                var ns = fileindex.Root.Name.Namespace;

                if(regexPattern == null)
                {
                    return fileindex.Descendants(ns.GetName("f"))
                        .ToDictionary(f => $"{f.Element(ns.GetName("foN")).Value}\\{f.Element(ns.GetName("fiN")).Value}", f => ByteHelper.ParseHex(f.Element(ns.GetName("md5")).Value));
                }
                else
                {
                    return fileindex.Descendants(ns.GetName("f"))
                        .Where(f => Regex.IsMatch($"{f.Element(ns.GetName("foN")).Value}\\{f.Element(ns.GetName("fiN")).Value}", regexPattern))
                        .ToDictionary(f => $"{f.Element(ns.GetName("foN")).Value}\\{f.Element(ns.GetName("fiN")).Value}", f => ByteHelper.ParseHex(f.Element(ns.GetName("md5")).Value));
                }
            }
        }
        public override TableReader GetTableReader(string tableName, string mediaId)
        {
            return new TableReader1007(TableIndex.tables.FirstOrDefault(x => x.name == tableName), this, mediaId, _fileSystem);
        }

        public override string FileIndexPath
        {
            get
            {
                return _fileIndexPath;
            }
        }

        /// <summary>
        /// Iterates all files in ArchiveVersion media folders that match the search pattern
        /// </summary>
        /// <returns>A list of absolute file paths</returns>
        //TODO: Should this be deprecated (same as EnumerateFiles().ToList()?) 
        public override List<string> GetFiles(string regexPattern = null)
        {
            List<string> files = new List<string>();
            foreach (var mediaFolder in this.Info.Medias)
            {
                // Get files in mediaFolder root
                files.AddRange(EnumerateFilesInDirectory(mediaFolder.Value, regexPattern, SearchOption.TopDirectoryOnly).ToList());

                // Iterate all sub folders of media
                foreach (var subdir in _fileSystem.Directory.GetDirectories(mediaFolder.Value, "*.*", SearchOption.TopDirectoryOnly).ToList())
                {
                    // Get all files from subdir
                    files.AddRange(EnumerateFilesInDirectory(subdir, regexPattern, SearchOption.TopDirectoryOnly).ToList());

                    // Use threading when iterating sub folders such as Documents and ContextDocumentation folders
                    var childDirs = _fileSystem.Directory.EnumerateDirectories(subdir, "*.*", SearchOption.TopDirectoryOnly).ToList();
                    Parallel.ForEach(childDirs, new ParallelOptions { MaxDegreeOfParallelism = 24 }, dC =>
                    {
                        var fs = EnumerateFilesInDirectory(dC, regexPattern, SearchOption.AllDirectories).ToList();
                        lock (files)
                        {
                            files.AddRange(fs);
                        }
                    });
                }
            }

            return files;
        }

        ///<summary>
        /// Iterates all files in ArchiveVersion media folders that matches the regex pattern.
        /// If the regex pattern is null all files are returned
        /// </summary>
        /// <param name="path"></param>
        /// <param name="regexPattern"></param>
        /// <param name="searchOption"></param>
        /// <returns></returns>
        private IEnumerable<string> EnumerateFilesInDirectory(string path, string regexPattern, SearchOption searchOption)
        {
            if(regexPattern == null)
            {
                return _fileSystem.Directory.EnumerateFiles(path, "*.*", searchOption);
            }
            else
            {
                return _fileSystem.Directory.EnumerateFiles(path, "*.*", searchOption).Where(f => Regex.IsMatch(f, regexPattern));
            }
        }

        public override IEnumerable<string> EnumerateFiles(string regexPattern = null)
        {
            foreach (var mediaFolder in this.Info.Medias)
            {
                // Get files in mediaFolder root
                foreach (string file in EnumerateFilesInDirectory(mediaFolder.Value, regexPattern, SearchOption.TopDirectoryOnly))
                {
                    yield return file;
                }

                // Iterate all sub folders of media
                foreach (var subdir in _fileSystem.Directory.GetDirectories(mediaFolder.Value, "*", SearchOption.TopDirectoryOnly))
                {
                    // Get all files from subdir
                    foreach (string file in EnumerateFilesInDirectory(subdir, regexPattern, SearchOption.TopDirectoryOnly))
                    {
                        yield return file;
                    }

                    // Use threading when iterating sub folders such as Documents and ContextDocumentation folders
                    List<string> files = new List<string>();
                    var childDirs = _fileSystem.Directory.EnumerateDirectories(subdir, "*", SearchOption.TopDirectoryOnly).ToList();
                    Parallel.ForEach(childDirs, new ParallelOptions { MaxDegreeOfParallelism = 24 }, dC =>
                    {
                        var fs = EnumerateFilesInDirectory(dC, regexPattern, SearchOption.AllDirectories);
                        lock (files)
                        {
                            files.AddRange(fs);
                        }
                    });

                    foreach (string file in files)
                    {
                        yield return file;
                    }
                }
            }
        }
    }
}
