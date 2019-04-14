using System;
using System.Collections.Generic;
using System.Text;
using FS.Core.Api.Directory;

namespace FS.Core.Directory
{
    internal interface IUnsafeDirectory : IDirectory
    {
        void UnsafeDeleteDirectory();
    }
}
