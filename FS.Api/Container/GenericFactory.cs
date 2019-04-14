using System;
using System.Collections.Generic;
using System.Text;

namespace FS.Api.Container
{
    public sealed class GenericFactory<TResult, TArg> : IFactory<TResult, TArg>
    {
        private readonly Func<TArg, TResult> factory;

        public GenericFactory(Func<TArg, TResult> factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public TResult Create(TArg arg1)
        {
            return factory(arg1);
        }
    }
}
