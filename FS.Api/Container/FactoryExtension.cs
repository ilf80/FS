using Unity.Extension;
using Unity.Lifetime;

namespace FS.Api.Container
{
    public sealed class FactoryExtension : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterType(typeof(IFactory<>), typeof(Factory<>), null, new TransientLifetimeManager())
                .RegisterType(typeof(IFactory<,>), typeof(Factory<,>), null, new TransientLifetimeManager())
                .RegisterType(typeof(IFactory<,,>), typeof(Factory<,,>), null, new TransientLifetimeManager())
                .RegisterType(typeof(IFactory<,,,>), typeof(Factory<,,,>), null, new TransientLifetimeManager());
        }
    }
}
