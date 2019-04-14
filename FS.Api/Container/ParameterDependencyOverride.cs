using System;
using System.Collections.Generic;
using System.Text;
using Unity.Resolution;

namespace FS.Api.Container
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
