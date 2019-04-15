using System;

namespace FS
{
    /// <summary>
    /// Provides an access for the file, supporting boh read and write operations
    /// </summary>
    public interface IFileEntry : IDisposable
    {
        /// <summary>
        /// Current length of the file
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Reads a block of bytes from the file and writes the data in a given buffer
        /// </summary>
        /// <param name="position">Position in the file</param>
        /// <param name="buffer">When this method returns, contains the specified byte array
        /// replaced by the bytes read from the current source.</param>
        void Read(int position, byte[] buffer);

        /// <summary>
        /// Writes a block of bytes to the file
        /// </summary>
        /// <param name="position">Position in the file</param>
        /// <param name="buffer">The buffer containing data to write to the file</param>
        void Write(int position, byte[] buffer);

        /// <summary>
        /// Sets the length of this file to the given value
        /// </summary>
        /// <param name="size">The new length of the file</param>
        void SetSize(int size);

        /// <summary>
        /// Clears buffers for this file and causes any buffered data to be written to the file system.
        /// </summary>
        void Flush();
    }
}