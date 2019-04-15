using System;
using Unity.Resolution;

namespace FS.Container
{
    internal sealed class ParameterDependencyOverride<TType, TDependencyType> : DependencyOverride<TDependencyType>
    {
        public ParameterDependencyOverride(object dependencyValue) : base(dependencyValue)
        {
        }

        public override ResolveDelegate<TContext> GetResolver<TContext>(Type type)
        {
            return ((ResolverOverride) this).GetResolver<TContext>(type);
        }
    }
}