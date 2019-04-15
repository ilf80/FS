using System;

namespace FS
{
    /// <summary>
    /// Contains detailed information about directory entry
    /// </summary>
    public interface IFileSystemEntry
    {
        /// <summary>
        /// Gets a value that indicates that this entry should be treated as a directory
        /// </summary>
        bool IsDirectory { get; }

        /// <summary>
        /// Size of entry. Size in bytes for file and zero for directory.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// The time that the current file or directory was created
        /// </summary>
        DateTime Created { get; }

        /// <summary>
        /// The time that the current file or directory was last accessed
        /// </summary>
        DateTime Updated { get; }

        /// <summary>
        /// The name of the current file or directory
        /// </summary>
        string Name { get; }
    }
}