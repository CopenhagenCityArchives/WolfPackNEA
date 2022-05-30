using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.MemoryMappedFiles;

namespace WolfPack.Lib.Services
{
    public class ZipPacker : IPacker
    {
        static ILog log;
        private ZipEncryptionMethod EncryptionType { get; set; }
        private string Password { get; set; }
        private readonly IFileSystem fileSystem;
        public Stream PackedItemsStream { get; set; }
        public int MaxRetries { get; set; }

        public string GetPackageFileExtension()
        {
            return ".zip";
        }

        public long MaxMemoryStreamSize { get; set; }

        public ZipPacker(string password,  IFileSystem _fileSystem = null)
        {
            Password = password;
            EncryptionType = ZipEncryptionMethod.AES256;
            fileSystem = _fileSystem ?? new FileSystem();
            MaxMemoryStreamSize = (long)(1024 * 1024 * 1024 * 1.8);
            MaxRetries = 1;

            log = LogManager.GetLogger("PackageOrchestra");
        }

        public void PackItemsInStream(IPackage pack)
        {
            var zipStream = new ZipOutputStream(PackedItemsStream);
  
            zipStream.Password = Password;
            zipStream.UseZip64 = UseZip64.On;
            foreach (var item in pack.Items)
            {
                var entry = new ZipEntry(item.RelativePath);
                entry.AESKeySize = 256;
                entry.CompressionMethod = CompressionMethod.Stored;



                zipStream.PutNextEntry(entry);
                if (entry.IsDirectory)
                {
                    zipStream.CloseEntry();
                    continue;
                }

                int retries = 0;
                while(retries < MaxRetries)
                {
                    try
                    {
                        using (var sourceStream = fileSystem.File.OpenRead(item.AbsolutePath))
                        {
                            sourceStream.CopyTo(zipStream);
                        }
                        break;
                    }
                    catch (Exception e)
                    {
                        log.Info($"Could not open file {item.AbsolutePath} for read while packing. Retrying. Error: {e.Message}");
                        retries++;
                        if (retries >= MaxRetries) { throw new Exception("Could not read file while packing. MaxEntries exceeded."); }
                    }
                }

                zipStream.CloseEntry();
            }
            zipStream.Finish();
        }

        public IEnumerable<Tuple<string, Stream>> UnpackItemsFromStream()
        {
            var zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(PackedItemsStream);

            if (!string.IsNullOrEmpty(Password))
            {
                // AES encrypted entries are handled automatically
                zf.Password = Password;
            }

            foreach (ZipEntry zipEntry in zf)
            {
                
                string entryFileName = zipEntry.Name.Replace('/', '\\');
                /*
                // to remove the folder from the entry:
                //entryFileName = Path.GetFileName(entryFileName);
                // Optionally match entrynames against a selection list here
                // to skip as desired.
                // The unpacked length is available in the zipEntry.Size property.

                // Manipulate the output filename here as desired.
                var fullZipToPath = $"{destinationPath}\\{entryFileName}";
                var directoryName = _fileSystem.Path.GetDirectoryName(fullZipToPath);
                if (directoryName.Length > 0)
                {
                    _fileSystem.Directory.CreateDirectory(directoryName);
                }
                */

                // 4K is optimum
                var buffer = new byte[4096];

                // Unzip file in buffered chunks. This is just as fast as unpacking
                // to a buffer the full size of the file, but does not waste memory.
                // The "using" will close the stream even if an exception occurs.
                var zipStream = zf.GetInputStream(zipEntry);
                //{
                //Stream fsOutput = new MemoryStream();
                //StreamUtils.Copy(zipStream, fsOutput, buffer);
                yield return new Tuple<string, Stream>(entryFileName, zipStream);
                //}

            }
        }

        public IEnumerable<string> GetRelativePathsForEntries()
        {
            var zf = new ICSharpCode.SharpZipLib.Zip.ZipFile(PackedItemsStream);

            if (!string.IsNullOrEmpty(Password))
            {
                // AES encrypted entries are handled automatically
                zf.Password = Password;
            }

            foreach (ZipEntry zipEntry in zf)
            {
                yield return zipEntry.Name.Replace('/', '\\');
            }
        }
    }
}
