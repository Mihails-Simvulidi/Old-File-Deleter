using System;

namespace OldFileDeleter.ClassLibrary
{
    public class FileDeleterException : Exception
    {
        public FileDeleterException(string message) : base(message) { }
        public FileDeleterException(string message, Exception inner) : base(message, inner) { }
    }
}
