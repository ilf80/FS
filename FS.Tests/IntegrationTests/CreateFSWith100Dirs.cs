using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FS.Container;
using NUnit.Framework;
using Unity;

namespace FS.Tests.IntegrationTests
{
    [TestFixture]
    public sealed class CreateFsWith100Dirs
    {
        [SetUp]
        public void SetUp()
        {
            container = new UnityContainer()
                .AddExtension(new UnityExtension())
                .AddExtension(new Diagnostic());

            filePath = Path.Combine(Path.GetTempPath(), "TestFile.dat");

            var assembly = Assembly.GetExecutingAssembly();
            stream = assembly.GetManifestResourceStream("FS.Tests.Program.cs");
        }

        [TearDown]
        public void TearDown()
        {
            stream.Close();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private string filePath;
        private Stream stream;
        private IUnityContainer container;

        [Test]
        public void GenerateDirectoriesAndRead()
        {
            var nameList = new List<string>();
            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.Create);
                var root = fs.GetRootDirectory();


                for (var i = 0; i < 100; i++)
                {
                    var name = "Dir " + i;
                    root.OpenDirectory(name, OpenMode.OpenOrCreate);
                    nameList.Add(name);
                }

                using (var file = root.OpenFile("Program.cs", OpenMode.OpenOrCreate))
                using (var memoryStream = new MemoryStream())
                using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
                using (var reader = new StreamReader(stream))
                {
                    writer.Write(reader.ReadToEnd());
                    file.SetSize((int) memoryStream.Length);
                    file.Write(0, memoryStream.ToArray());
                }

                nameList.Add("Program.cs");
            }

            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.OpenExisting);
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEqual(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }

        [Test]
        public void GenerateDirectoriesDeleteFileAndRead()
        {
            var nameList = new List<string>();
            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.Create);

                var root = fs.GetRootDirectory();


                for (var i = 0; i < 100; i++)
                {
                    var name = "Dir " + i;
                    root.OpenDirectory(name, OpenMode.OpenOrCreate).Dispose();
                    nameList.Add(name);
                }

                using (var file = root.OpenFile("Program.cs", OpenMode.OpenOrCreate))
                using (var memoryStream = new MemoryStream())
                using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
                using (var reader = new StreamReader(stream))
                {
                    writer.Write(reader.ReadToEnd());
                    file.SetSize((int) memoryStream.Length);
                    file.Write(0, memoryStream.ToArray());
                }

                root.DeleteFile("Program.cs");
            }

            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.OpenExisting);
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEqual(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }

        [Test]
        public void GenerateDirectoriesDeleteOneAndRead()
        {
            var nameList = new List<string>();
            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.Create);

                var root = fs.GetRootDirectory();


                for (var i = 0; i < 100; i++)
                {
                    var name = "Dir " + i;
                    root.OpenDirectory(name, OpenMode.OpenOrCreate).Dispose();
                    nameList.Add(name);
                }

                using (var file = root.OpenFile("Program.cs", OpenMode.OpenOrCreate))
                using (var memoryStream = new MemoryStream())
                using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
                using (var reader = new StreamReader(stream))
                {
                    writer.Write(reader.ReadToEnd());
                    file.SetSize((int) memoryStream.Length);
                    file.Write(0, memoryStream.ToArray());
                }

                nameList.Add("Program.cs");

                root.DeleteDirectory("Dir 99");
                nameList.Remove("Dir 99");
            }

            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.OpenExisting);
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEqual(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }

        [Test]
        public void GenerateDirectoriesInThreadsAndRead()
        {
            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.Create);

                var root = fs.GetRootDirectory();

                var task1 = new Task(() =>
                {
                    for (var i = 0; i < 100; i++)
                    {
                        var name = "Dir " + i;
                        root.OpenDirectory(name, OpenMode.OpenOrCreate);
                    }
                });

                var task2 = new Task(() =>
                {
                    for (var i = 0; i < 150; i++)
                    {
                        var name = "Dir " + i;
                        root.OpenDirectory(name, OpenMode.OpenOrCreate);
                    }
                });

                task1.Start();
                task2.Start();

                Task.WaitAll(task1, task2);
            }

            var nameList = new List<string>();
            for (var i = 0; i < 150; i++)
            {
                nameList.Add("Dir " + i);
            }

            using (var fs = container.Resolve<IFileSystem>())
            {
                fs.Open(filePath, OpenMode.OpenExisting);
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEquivalent(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }
    }
}