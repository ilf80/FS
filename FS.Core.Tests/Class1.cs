using System;
using FS.Api.Container;
using FS.Core.Api.BlockAccess;
using FS.Core.Container;
using NUnit.Framework;
using Unity;
using Unity.Resolution;

namespace FS.Core.Tests
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public void Test()
        {
            var container = new UnityContainer();
            var result = container
                .AddExtension(new CoreRegistration())
                .AddExtension(new FactoryExtension())
                .Resolve<IFactory<IBlockStorage, string>>();
            result.Create("1");
        }
    }
}
