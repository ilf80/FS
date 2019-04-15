using System;
using System.Collections.Generic;

namespace FS
{
    /// <summary>
    ///  Provides methods for creating, moving, and enumerating through directories and subdirectories. 
    /// </summary>
    public interface IDirectoryEntry : IDisposable
    {
        /// <summary>
        /// Enumerates all subdirectories and files with detailed information
        /// </summary>
        /// <returns>Enumeration of directories and files</returns>
        IEnumerable<IFileSystemEntry> GetEntries();

        /// <summary>
        /// Tries to find file system entry by its name, and returns a value that indicates whether
        /// the search succeeded.
        /// </summary>
        /// <param name="name">File system entry name to search</param>
        /// <param name="entry">The result of the search operation.</param>
        /// <returns></returns>
        bool TryGetEntry(string name, out IFileSystemEntry entry);

        /// <summary>
        /// Creates or opens a file with specified name
        /// </summary>
        /// <param name="name">File name</param>
        /// <param name="mode">Opening mode for file</param>
        /// <returns>File object</returns>
        IFileEntry OpenFile(string name, OpenMode mode);

        /// <summary>
        /// Removes specified file name. If file does not exist an exception is thrown
        /// </summary>
        /// <param name="name">Name of file to remove</param>
        void DeleteFile(string name);

        /// <summary>
        /// Creates or opens a directory with specified name
        /// </summary>
        /// <param name="name">Directory name</param>
        /// <param name="mode">Opening mode for directory</param>
        /// <returns>Directory object</returns>
        IDirectoryEntry OpenDirectory(string name, OpenMode mode);

        /// <summary>
        /// Deletes an empty directory with specified name.
        /// </summary>
        /// <param name="name">The name of the empty directory to remove.</param>
        void DeleteDirectory(string name);

        /// <summary>
        /// Clears buffers for this directory and causes any buffered data to be written to the file.
        /// </summary>
        void Flush();
    }
}