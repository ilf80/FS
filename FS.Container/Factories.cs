using Unity;
using Unity.Resolution;

namespace FS.Container
{
    internal abstract class FactoryBase<TResult>
    {
        private readonly IUnityContainer container;

        protected FactoryBase(IUnityContainer container)
        {
            this.container = container;
        }

        protected TResult GetService(params ResolverOverride[] overrides)
        {
            return container.Resolve<TResult>(overrides);
        }
    }

    internal sealed class Factory<TResult> : FactoryBase<TResult>, IFactory<TResult>
    {
        public Factory(IUnityContainer container) : base(container)
        {
        }

        public TResult Create()
        {
            return GetService();
        }
    }

    internal sealed class Factory<TResult, TArg1> : FactoryBase<TResult>, IFactory<TResult, TArg1>
    {
        public Factory(IUnityContainer container) : base(container)
        {
        }

        public TResult Create(TArg1 arg1)
        {
            return GetService(new ParameterDependencyOverride<TResult, TArg1>(arg1));
        }
    }

    internal sealed class Factory<TResult, TArg1, TArg2> : FactoryBase<TResult>, IFactory<TResult, TArg1, TArg2>
    {
        public Factory(IUnityContainer container) : base(container)
        {
        }

        public TResult Create(TArg1 arg1, TArg2 arg2)
        {
            return GetService(
                new ParameterDependencyOverride<TResult, TArg1>(arg1),
                new ParameterDependencyOverride<TResult, TArg2>(arg2));
        }
    }

    internal sealed class Factory<TResult, TArg1, TArg2, TArg3> : FactoryBase<TResult>, IFactory<TResult, TArg1, TArg2, TArg3>
    {
        public Factory(IUnityContainer container) : base(container)
        {
        }

        public TResult Create(TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return GetService(
                new ParameterDependencyOverride<TResult, TArg1>(arg1),
                new ParameterDependencyOverride<TResult, TArg2>(arg2),
                new ParameterDependencyOverride<TResult, TArg3>(arg3));
        }
    }
}