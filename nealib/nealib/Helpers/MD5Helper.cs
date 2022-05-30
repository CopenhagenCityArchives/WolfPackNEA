using NEA.ArchiveModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NEA.Helpers
{
    public class FilesVerifiedEventArgs : EventArgs
    {
        public int ProcessedFiles { get; set; }
        public int SkippedFiles { get; set; }
        public int ErrorsCount { get; set; }
        public int TotalFiles { get; set; }
    }
    public class MD5Helper
    {
        public int NumberOfThreads { get; set; } = 0;
        public long FileReadBufferSize { get; set; } = 0;

        private readonly IFileSystem _fileSystem;
        /// <summary>
        /// Fires when for each 1% of files verified or for every file verified if less than 100 files 
        /// </summary>
        public event EventHandler<FilesVerifiedEventArgs> FilesVerified;

        /// <summary>
        /// Fires when a checksum error is found
        /// </summary>
        public event EventHandler<string> VerifyFailed;

        public MD5Helper(IFileSystem fileSystem = null)
        {
            _fileSystem = fileSystem ?? new FileSystem();
        }
        public byte[] CalculateChecksum(string filepath)
        {
            using (var md5 = MD5.Create())
            {
                // Use File.OpenRead
                if (FileReadBufferSize == 0)
                {
                    using (var stream = _fileSystem.File.OpenRead(filepath))
                    {
                        return md5.ComputeHash(stream);
                    }
                }
                // Use FileStream with custom buffer size (the smallest of the two: Stream size or file length)
                else
                {
                    FileInfo fi = new FileInfo(filepath);
                    int bufferSize = (int)Math.Min(fi.Length, FileReadBufferSize);
                    {
                        using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize))
                        {
                            return md5.ComputeHash(fs);
                        }
                    }
                }
            }
        }

        public async Task<Dictionary<string, bool>> VerifyChecksumsAsync(ArchiveVersion av, List<string> files)
        {
            return await Task.Run(() =>
            {
                return VerifyChecksums(av, files);
            });
        }
        /// <summary>
        /// Verifies the calculated checksums of archive version files against the expected values found in its index
        /// </summary>
        /// <param name="av">The archive version to be checked</param>
        /// <param name="includeDocuments">Indicate wether documents files should also be verified</param>
        /// <returns>A dictionary of (key)filepaths and (value)verification result</returns>
        public Dictionary<string, bool> VerifyChecksums(ArchiveVersion av, List<string> files)
        {
            long checkedFiles = 0;
            int failedChecks = 0;
            int notifyFrequency = (int)Math.Ceiling((decimal)files.Count() / 100); //We want to notify at least for each 1% of files processed
            var resultDict = new ConcurrentDictionary<string, bool>();
            var expectedChecksums = av.GetChecksumDict();

            // Sort files in descending order. This will ensure that table files are validated first, giving a shorter total runtime
            files.Sort((x, y) => string.Compare(y, x));

            int threads = 0;
            if (NumberOfThreads == 0)
            {
                threads = Environment.ProcessorCount;
            }
            else
            {
                threads = NumberOfThreads;
            }

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = threads }, file =>
            {
                int currentChecked = (Int32)Interlocked.Increment(ref checkedFiles);

                bool result = false;

                try
                {
                    string relativeFilePath = av.GetRelativeFilePath(file);
                    byte[] expectedCheckSum = expectedChecksums[relativeFilePath];


                    int retries = 0;
                    while (retries < 3)
                    {
                        try
                        {
                            result = CalculateChecksum(file).SequenceEqual(expectedCheckSum);
                            break;
                        }
                        catch (IOException)
                        {
                            retries++;
                        }
                    }
                }
                catch (InvalidOperationException e)
                {
                    // Never check fileIndex.xml
                    if (!file.EndsWith("fileIndex.xml"))
                    {
                        Console.WriteLine($"The file to check was not found in the expected checksum list: {e.Message}");
                        Interlocked.Increment(ref failedChecks);
                        OnVerifyFailed(file);
                    }
                    else
                    {
                        result = true;
                    }
                }
                catch (Exception e)
                {
                    result = false;
                    Console.WriteLine($"This file could not be checked: {file} -> {e.Message}");
                    Interlocked.Increment(ref failedChecks);
                    OnVerifyFailed(file);
                }
                if (!resultDict.TryAdd(file, result))
                {
                    throw new InvalidOperationException($"Cannot process duplicate filepath! {file}");
                }

                if (!result)
                {
                    Interlocked.Increment(ref failedChecks);
                    OnVerifyFailed(file);
                }

                if (currentChecked % notifyFrequency == 0)
                {
                    OnFilesVerified(new FilesVerifiedEventArgs { ProcessedFiles = currentChecked, ErrorsCount = failedChecks });
                }
            });
            OnFilesVerified(new FilesVerifiedEventArgs { ProcessedFiles = (Int32)Interlocked.Read(ref checkedFiles), ErrorsCount = failedChecks });
            return resultDict.ToDictionary(x => x.Key, x => x.Value);
        }
        protected virtual void OnVerifyFailed(string FilePath)
        {
            VerifyFailed?.Invoke(this, FilePath);
        }
        protected virtual void OnFilesVerified(FilesVerifiedEventArgs e)
        {
            FilesVerified?.Invoke(this, e);
        }
    }
}
