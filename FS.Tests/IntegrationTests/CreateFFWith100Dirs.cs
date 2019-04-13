using FS.Api;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FS.Tests.IntegrationTests
{
    [TestFixture]
    public sealed class CreateFFWith100Dirs
    {
        private string filePath;
        private Stream stream;

        [SetUp]
        public void SetUp()
        {
            this.filePath = Path.Combine(Path.GetTempPath(), "TestFile.dat");

            var assembly = Assembly.GetExecutingAssembly();
            this.stream = assembly.GetManifestResourceStream("FS.Tests.Program.cs");
        }

        [TearDown]
        public void TearDown()
        {
            this.stream.Close();
            if (System.IO.File.Exists(this.filePath))
            {
                System.IO.File.Delete(this.filePath);
            }
        }

        [Test]
        public void GenerateDirectoriesAndRead()
        {
            var nameList = new List<string>();
            using (var fs = FileSystem.Create(this.filePath))
            {
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
                using (var reader = new StreamReader(this.stream))
                {
                    writer.Write(reader.ReadToEnd());
                    file.SetSize((int)memoryStream.Length);
                    file.Write(0, memoryStream.ToArray());
                }
                nameList.Add("Program.cs");
            }

            using (var fs = FileSystem.Open(this.filePath))
            {
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEqual(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }

        [Test]
        public void GenerateDirectoriesDeleteOneAndRead()
        {
            var nameList = new List<string>();
            using (var fs = FileSystem.Create(this.filePath))
            {
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
                using (var reader = new StreamReader(this.stream))
                {
                    writer.Write(reader.ReadToEnd());
                    file.SetSize((int)memoryStream.Length);
                    file.Write(0, memoryStream.ToArray());
                }
                nameList.Add("Program.cs");

                root.DeleteDirectory("Dir 99");
                nameList.Remove("Dir 99");
            }

            using (var fs = FileSystem.Open(this.filePath))
            {
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEqual(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }

        [Test]
        public void GenerateDirectoriesDeleteFileAndRead()
        {
            var nameList = new List<string>();
            using (var fs = FileSystem.Create(this.filePath))
            {
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
                using (var reader = new StreamReader(this.stream))
                {
                    writer.Write(reader.ReadToEnd());
                    file.SetSize((int)memoryStream.Length);
                    file.Write(0, memoryStream.ToArray());
                }
                root.DeleteFile("Program.cs");
            }

            using (var fs = FileSystem.Open(this.filePath))
            {
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEqual(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }

        [Test]
        public void GenerateDirectoriesInThreadsAndRead()
        {
            using (var fs = FileSystem.Create(this.filePath))
            {
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
            for (var i = 0; i < 150; i++) nameList.Add("Dir " + i);
            using (var fs = FileSystem.Open(this.filePath))
            {
                var root = fs.GetRootDirectory();

                CollectionAssert.AreEquivalent(nameList.ToArray(), root.GetEntries().Select(x => x.Name).ToArray());
            }
        }
    }
}
