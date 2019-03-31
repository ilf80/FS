using FS.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS.Indexes
{
    internal interface IIndexManager
    {
        Task Increase(int blockCount);

        Task Shrink(int totalBlockCount);

        Task<uint> GetBlockForOffset(int offset);
    }
}
