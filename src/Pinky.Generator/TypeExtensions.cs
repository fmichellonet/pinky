using System;
using System.Collections.Generic;

namespace Pinky.Generator;

public static class TypeExtensions
{
    private static readonly Dictionary<Type, string> SpecialTypeNames = new()
    {
        { typeof(SpecialTypes.Void), "void" },
        //{ typeof(int), "int" },
        //{ typeof(long), "long" },
        //{ typeof(float), "float" },
        //{ typeof(double), "double" },
        //{ typeof(decimal), "decimal" },
        //{ typeof(string), "string" },
        //{ typeof(bool), "bool" },
        //{ typeof(char), "char" },
        //{ typeof(object), "object" }
    };

    public static string ToFriendlyString(this Type type)
    {
        var value = SpecialTypeNames.TryGetValue(type, out var specialName);
        return value ? specialName : type.Name;
    }
}