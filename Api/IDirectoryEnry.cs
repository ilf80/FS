﻿using System;

namespace FS.Api
{
    public interface IDirectoryEntry : IDisposable
    {
        IFileSystemEntry[] GetEntries();

        IFileSystemEntry FindOrDefault(string name);

        IFileEntry OpenFile(string name, OpenMode mode);

        IDirectoryEntry OpenDirectory(string name, OpenMode mode);

        void Flush();
    }
}
