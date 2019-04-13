using System;
using System.Threading.Tasks;
using FS.Api;
using System.Linq;
using System.IO;
using System.Text;

namespace FS
{
    class Program
    {
        static void PrintDirectory(IDirectoryEntry root, string path)
        {
            var entries = root.GetEntries();

            foreach (var entry in entries.Where(x => x.IsDirectory).OrderBy(x => x.Name))
            {
                Console.WriteLine($"{path}/{entry.Name}, Directory { entry.IsDirectory }, Size {entry.Size}, Created {entry.Created}, Updated {entry.Updated} ");
                using (var d = root.OpenDirectory(entry.Name, OpenMode.OpenExisting))
                {
                    PrintDirectory(d, path + "/" + entry.Name);
                }
            }

            foreach (var entry in entries.Where(x => !x.IsDirectory).OrderBy(x => x.Name))
            {
                Console.WriteLine($"{path}/{entry.Name}, Directory { entry.IsDirectory }, Size {entry.Size}, Created {entry.Created}, Updated {entry.Updated} ");
            }

            
        }
        static void Main(string[] args)
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

                    using (var file = root.OpenFile("Program.cs", OpenMode.OpenOrCreate))
                    using (var memoryStream = new MemoryStream())
                    using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
                    using (var reader = System.IO.File.OpenText(@"Program.cs"))
                    {
                        writer.Write(reader.ReadToEnd());
                        file.SetSize((int)memoryStream.Length);
                        file.Write(0, memoryStream.ToArray());
                    }
                }

                //root.DeleteFile("Program.cs");
                //root.OpenDirectory("Dir 98", OpenMode.OpenExisting);
                //root.DeleteDirectory("Dir 98");

                ////using (var d = root.OpenDirectory("Dir 8", OpenMode.OpenExisting))
                ////{
                ////    var t = new Task(() =>
                ////    {
                ////        for (int i = 100; i < 200; i++)
                ////        {
                ////            d.OpenDirectory("Dir 8." + i + "." + Thread.CurrentThread.ManagedThreadId, OpenMode.OpenOrCreate);
                ////        }
                ////    });

                ////    var t2 = new Task(() =>
                ////    {
                ////        for (int i = 100; i < 200; i++)
                ////        {
                ////            d.OpenDirectory("Dir 8." + i + "." + Thread.CurrentThread.ManagedThreadId, OpenMode.OpenOrCreate);
                ////        }
                ////    });

                ////    //t.Start(); t2.Start();

                ////    //Task.WaitAll(t, t2);
                ////}

                PrintDirectory(root, "ROOT");
            }
        }
    }
}
