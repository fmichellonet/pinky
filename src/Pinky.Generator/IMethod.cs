using System;

namespace Pinky;

internal interface IMethod
{
    string Name { get; }

    Type ReturnType { get; }

    object? DesiredReturnValue { get; }
}