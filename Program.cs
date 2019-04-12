using FS.Allocattion;
using FS.Contracts;
using FS.Directory;
using FS.Contracts.Indexes;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using bs = FS.Contracts.BlockStorage;
using FS.Api;
using System.Linq;
using System.Threading;
using System.IO;
using System.Text;

namespace FS
{
    class Program
    {
        static void PrindDirectory(IDirectoryEntry root, string path)
        {
            var entries = root.GetEntries();

            foreach (var entry in entries.Where(x => x.IsDirectory).OrderBy(x => x.Name))
            {
                Console.WriteLine($"{path}/{entry.Name}, Directory { entry.IsDirectory }, Size {entry.Size}, Created {entry.Created}, Updated {entry.Updated} ");
                using (var d = root.OpenDirectory(entry.Name, OpenMode.OpenExisting))
                {
                    PrindDirectory(d, path + "/" + entry.Name);
                }
            }

            foreach (var entry in entries.Where(x => !x.IsDirectory).OrderBy(x => x.Name))
            {
                Console.WriteLine($"{path}/{entry.Name}, Directory { entry.IsDirectory }, Size {entry.Size}, Created {entry.Created}, Updated {entry.Updated} ");
            }

            
        }
        static async Task Main(string[] args)
        {
            var shouldCreate = !System.IO.File.Exists("TestFile.dat");
            using (var fs = shouldCreate ? FileSystem.Create("TestFile.dat") : FileSystem.Open("TestFile.dat"))
            {
                var root = fs.GetRootDirectory();

                if (shouldCreate)
                {
                    for (var i = 0; i < 100; i++)
                    {
                        root.OpenDirectory("Dir " + i, OpenMode.OpenOrCreate);
                    }

                    using(var file = root.OpenFile("Program.cs", OpenMode.OpenOrCreate))
                    {
                        using (var memoryStream = new MemoryStream())
                        using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
                        using (var reader = System.IO.File.OpenText(@"Program.cs"))
                        {
                            writer.Write(reader.ReadToEnd());
                            file.SetSize((int)memoryStream.Length);
                            file.Write(0, memoryStream.ToArray());
                        }
                    }
                }

                //root.DeleteFile("Program.cs");

                PrindDirectory(root, "ROOT");
            }
            /*using (var fs = FileSystem.Open("TestFile.dat"))
            {
                var root = fs.GetRootDirectory();

                PrindDirectory(root, "ROOT");

                var file = root.OpenFile("Test File 12.04.2019", OpenMode.OpenOrCreate);
                file.SetSize(512 * 10);

                using (var memoryStream = new MemoryStream())
                using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
                using (var reader = System.IO.File.OpenText(@"C:\Users\Ilya\source\repos\FS\Program.cs"))
                {
                    writer.Write(reader.ReadToEnd());
                    file.SetSize((int)memoryStream.Length);
                    file.Write(0, memoryStream.ToArray());
                }
                file.Dispose();


                using (var d = root.OpenDirectory("Dir 8", OpenMode.OpenExisting))
                {
                    var t = new Task(() =>
                    {
                        for (int i = 100; i < 200; i++)
                        {
                            d.OpenDirectory("Dir 8." + i + "." + Thread.CurrentThread.ManagedThreadId, OpenMode.OpenOrCreate);
                        }
                    });

                    var t2 = new Task(() =>
                    {
                        for (int i = 100; i < 200; i++)
                        {
                            d.OpenDirectory("Dir 8." + i + "." + Thread.CurrentThread.ManagedThreadId, OpenMode.OpenOrCreate);
                        }
                    });

                    //t.Start(); t2.Start();

                    //Task.WaitAll(t, t2);
                }
            }*/
        }
    }
}
