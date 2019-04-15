using System;

namespace FS
{
    /// <summary>
    /// Provides methods for the creation, and opening of a single-file file system, accessing of root directory.
    /// </summary>
    public interface IFileSystem : IDisposable
    {
        /// <summary>
        /// Opens a FileSystem on the specified path having the specified mode with create,
        /// open and open or create file access strategy
        /// </summary>
        /// <param name="fileName">The file to open</param>
        /// <param name="openMode">A OpenMode value that specifies whether a file is created
        /// if one does not exist</param>
        void Open(string fileName, OpenMode openMode);

        /// <summary>
        /// Returns the root directory of single-file file system
        /// </summary>
        /// <returns>The root directory object</returns>
        IDirectoryEntry GetRootDirectory();
    }
}