using FS.Allocattion;
using FS.Contracts;
using FS.Indexes;
using System.Threading.Tasks;
using bs = FS.BlockStorage.BlockDevice;
namespace FS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var taskFactory = new TaskFactory(TaskCreationOptions.None, TaskContinuationOptions.ExecuteSynchronously);
            using (var blockStorage = new bs("TestFile.dat", taskFactory))
            {
                blockStorage.Open();

                var buffer = new byte[Constants.BlockSize];
                //await blockStorage.WriteBlock(0, buffer);
                //buffer[0] = 2;
                //await blockStorage.WriteBlock(1, buffer);
                //return;

                var allocationManager = new AllocationManager();

                var indexManager = new IndexManager(taskFactory, allocationManager, blockStorage, 1);
                //await indexManager.Increase(10);
                //await indexManager.Shrink(5);

                var file = new SystemFile.SystemFile(blockStorage, indexManager, 2000);
                await file.SetSize(2000);

                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)(i / 2);
                }
                await file.Write(1, buffer);
                //await file.Write(512, buffer);
                //await file.Write(1023, buffer);

                //file.Dispose();
                blockStorage.Dispose();

                //buffer[0] = 1;
                //blockStorage.WriteBlock(0, buffer);
            }
        }
    }
}
