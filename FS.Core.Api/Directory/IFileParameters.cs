using System.Collections.Generic;
using System.Text;

namespace FS.Core.Api.Directory
{
    public interface IFileParameters
    {
        int BlockId { get; }

        int ParentDirectoryBlockId { get; }

        int Size { get; }
    }
}
