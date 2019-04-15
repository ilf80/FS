﻿using System;

namespace FS
{
    public interface IFileSystemEntry
    {
        bool IsDirectory { get; }

        int Size { get; }

        DateTime Created { get; }

        DateTime Updated { get; }

        string Name { get; }
    }
}