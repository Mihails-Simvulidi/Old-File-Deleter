using System;
using System.IO;

namespace OldFileDeleter.ClassLibrary
{
    internal class FileInMemory
    {
        private readonly FileInfo FileInfo;
        private readonly DirectoryInMemory ParentDirectory;

        public string FullName => FileInfo.FullName;
        public DateTime LastWriteTime => FileInfo.LastWriteTime;
        public DateTime LastWriteTimeUtc => FileInfo.LastWriteTimeUtc;
        public long Length => FileInfo.Length;

        internal FileInMemory(FileInfo fileInfo, DirectoryInMemory parentDirectory)
        {
            FileInfo = fileInfo;
            ParentDirectory = parentDirectory;
        }

        public void Delete()
        {
            FileInfo.Delete();
            ParentDirectory?.RemoveFile(this);
        }

        public override string ToString()
        {
            return FileInfo.FullName;
        }
    }
}
