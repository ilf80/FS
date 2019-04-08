using FS.BlockChain;
using System.Text;
using System.Threading.Tasks;

namespace FS.Contracts
{
    internal interface IIndex<T> : IBlockChainProvider<T>, IFlushable where T : struct
    {
    }
}
