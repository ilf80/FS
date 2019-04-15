namespace FS
{
    /// <summary>
    /// Specifies how the file system should open a file
    /// </summary>
    public enum OpenMode
    {
        /// <summary>
        /// Specifies that the file system should open an existing file\directory
        /// An exception is thrown if the file\directory does not exist.
        /// </summary>
        OpenExisting,

        /// <summary>
        /// Specifies that the file system should open a file\directory if it exists; otherwise
        /// a new file\directory should be created.
        /// </summary>
        OpenOrCreate,

        /// <summary>
        /// Specifies that the file system should create a new file\directory.
        /// If the file\directory already exists, an exception is thrown.
        /// </summary>
        Create
    }
}