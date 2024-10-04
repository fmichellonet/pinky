using System;

namespace Pinky;

internal record Method<TReturn> : IMethod
{

    public Method(string name, TReturn? desiredReturnValue)
    {
        Name = name;
        TypedDesiredReturnValue = desiredReturnValue;
    }

    public string Name { get; }

    public Type ReturnType => typeof(TReturn);

    public TReturn? TypedDesiredReturnValue { get; }

    object? IMethod.DesiredReturnValue => TypedDesiredReturnValue;
}