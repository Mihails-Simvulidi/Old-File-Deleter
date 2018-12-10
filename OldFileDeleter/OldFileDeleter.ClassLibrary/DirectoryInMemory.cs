using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace OldFileDeleter.ClassLibrary
{
    internal class DirectoryInMemory
    {
        private readonly DirectoryInfo DirectoryInfo;
        private readonly List<FileInMemory> Files = new List<FileInMemory>();
        private readonly ILogger Logger;
        private readonly DirectoryInMemory ParentDirectory;
        private readonly List<DirectoryInMemory> SubDirectories = new List<DirectoryInMemory>();

        public DirectoryInMemory(DirectoryInfo directoryInfo, ILogger logger)
        {
            DirectoryInfo = directoryInfo;
            Logger = logger;
            
            try
            {
                foreach (FileSystemInfo fileSystemInfo in DirectoryInfo.EnumerateFileSystemInfos())
                {
                    if (fileSystemInfo is DirectoryInfo subDirectoryInfo)
                    {
                        DirectoryInMemory directoryInMemory = new DirectoryInMemory(subDirectoryInfo, logger, this);
                        SubDirectories.Add(directoryInMemory);
                    }
                    else if (fileSystemInfo is FileInfo fileInfo)
                    {
                        FileInMemory fileInMemory = new FileInMemory(fileInfo, this);
                        Files.Add(fileInMemory);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Cannot read contents of \"{directoryInfo.FullName}\"", e);
            }
        }

        private DirectoryInMemory(DirectoryInfo directoryInfo, ILogger logger, DirectoryInMemory parentDirectory) : this(directoryInfo, logger)
        {
            ParentDirectory = parentDirectory;
        }

        public void DeleteEmptySubDirectories(DateTime olderThanUtc)
        {
            foreach (DirectoryInMemory subDirectory in SubDirectories)
            {
                subDirectory.DeleteEmptySubDirectories(olderThanUtc);
                subDirectory.DeleteIfEmpty();
            }
        }

        private void DeleteIfEmpty()
        {
            if (SubDirectories.Count == 0 && Files.Count == 0)
            {
                try
                {
                    Logger.LogInformation($"Deleting empty directory \"{DirectoryInfo.FullName}\"...");
                    DirectoryInfo.Delete();
                    ParentDirectory?.RemoveDirectory(this);
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Could not delete empty directory \"{DirectoryInfo.FullName}\"", e);
                }
            }
        }

        public IEnumerable<FileInMemory> GetAllFiles()
        {
            foreach (DirectoryInMemory subDirectory in SubDirectories)
            {
                foreach (FileInMemory file in subDirectory.GetAllFiles())
                {
                    yield return file;
                }
            }

            foreach (FileInMemory file in Files)
            {
                yield return file;
            }
        }

        private void RemoveDirectory(DirectoryInMemory directoryInMemory)
        {
            SubDirectories.Remove(directoryInMemory);
        }

        internal void RemoveFile(FileInMemory fileInMemory)
        {
            Files.Remove(fileInMemory);
        }
    }
}
