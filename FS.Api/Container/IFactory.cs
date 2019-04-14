using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FS.Api.Container
{
    public interface IFactory<out TResult>
    {
        TResult Create();
    }

    public interface IFactory<out TResult, in TArg1>
    {
        TResult Create(TArg1 arg1);
    }

    public interface IFactory<out TResult, in TArg1, in TArg2>
    {
        TResult Create(TArg1 arg1, TArg2 arg2);
    }

    public interface IFactory<out TResult, in TArg1, in TArg2, in TArg3>
    {
        TResult Create(TArg1 arg1, TArg2 arg2, TArg3 arg3);
    }
}
