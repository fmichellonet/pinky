using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pinky.Generator;

internal class GhostParser
{
    internal static MockInformation? Parse(IMethodSymbol symbol, SemanticModel semanticModel)
    {
        var callingMethodSyntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax;
        if (callingMethodSyntax == null)
        {
            return null;
        }

        var forMethod = ForFinder.GetForMethodSyntax(callingMethodSyntax);

        var (interfaceType, typeSymbol) = GetInterfaceTypeInfo(forMethod, semanticModel);

        var methodReturnValues = FindMethodReturnValues(callingMethodSyntax, semanticModel);

        return new MockInformation(
            ComputeClassNameToGenerate(symbol),
            interfaceType.ToString(),
            ComputeUsings(forMethod, semanticModel),
            ComputeMethods(typeSymbol, semanticModel, methodReturnValues)
        );
    }

    private static Dictionary<string, object?> FindMethodReturnValues(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel)
    {
        var methodReturnValues = new Dictionary<string, object?>();

        var invocations = methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "Returns")
            {
                var methodName = ExtractMethodName(memberAccess.Expression);
                if (methodName != null)
                {
                    var returnValue = ExtractDesiredReturnValue(invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression, semanticModel);
                    methodReturnValues[methodName] = returnValue;
                }
            }
        }

        return methodReturnValues;
    }

    private static string? ExtractMethodName(ExpressionSyntax expression)
    {
        return expression switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
                _ => null
            },
            _ => null
        };
    }

    private static IReadOnlyCollection<IMethod> ComputeMethods(INamedTypeSymbol typeSymbol, SemanticModel semanticModel, Dictionary<string, object?> methodReturnValues)
    {
        var allMethods = typeSymbol.GetMembers().OfType<IMethodSymbol>();

        return allMethods
            .Select(x =>
            {
                var returnType = x.ReturnType;
                var desiredReturnValue = methodReturnValues.TryGetValue(x.Name, out var value) ? value : null;
                return CreateTypedMethodNew(x.Name, returnType, desiredReturnValue, semanticModel);
            })
            .ToArray();
    }

    private static IMethod CreateTypedMethodNew(string name, ITypeSymbol returnType, object? desiredReturnValue, SemanticModel semanticModel)
    {
        var methodType = typeof(Method<>).MakeGenericType(GetSystemTypeFromSymbol(returnType, semanticModel.Compilation));
        return (IMethod)Activator.CreateInstance(methodType, new object?[] { name, desiredReturnValue })!;
    }

    private static object? ExtractDesiredReturnValue(ExpressionSyntax? expression, SemanticModel semanticModel)
    {
        if (expression == null) return null;

        var constantValue = semanticModel.GetConstantValue(expression);
        if (constantValue.HasValue)
        {
            return constantValue.Value;
        }

        // Si ce n'est pas une constante, on pourrait avoir besoin d'une logique plus complexe ici
        // pour gérer d'autres types d'expressions
        return null;
    }

    private static (TypeSyntax interfaceType, INamedTypeSymbol typeSymbol) GetInterfaceTypeInfo(InvocationExpressionSyntax forMethod, SemanticModel semanticModel)
    {
        var memberAccess = forMethod.Expression as MemberAccessExpressionSyntax;

        if (memberAccess?.Name is not GenericNameSyntax genericName)
        {
            throw new ArgumentException("Unable to find the interface type");
        }

        var interfaceType = genericName.TypeArgumentList.Arguments.Single();
        if (semanticModel.GetSymbolInfo(interfaceType).Symbol is not INamedTypeSymbol typeSymbol)
        {
            throw new ArgumentException("Unable to find the interface type");
        }
        return (interfaceType, typeSymbol);

    }

    private static Type GetSystemTypeFromSymbol(ITypeSymbol typeSymbol, Compilation compilation)
    {
        if (typeSymbol.SpecialType != SpecialType.None)
        {
            return typeSymbol.SpecialType switch
            {
                SpecialType.System_Int32 => typeof(int),
                SpecialType.System_String => typeof(string),
                SpecialType.System_Boolean => typeof(bool),
                SpecialType.System_Void => typeof(SpecialTypes.Void),
                SpecialType.System_Object => typeof(object),
                SpecialType.System_Char => typeof(char),
                SpecialType.System_SByte => typeof(sbyte),
                SpecialType.System_Byte => typeof(byte),
                SpecialType.System_Int16 => typeof(short),
                SpecialType.System_UInt16 => typeof(ushort),
                SpecialType.System_UInt32 => typeof(uint),
                SpecialType.System_Int64 => typeof(long),
                SpecialType.System_UInt64 => typeof(ulong),
                SpecialType.System_Decimal => typeof(decimal),
                SpecialType.System_Single => typeof(float),
                SpecialType.System_Double => typeof(double),
                SpecialType.System_IntPtr => typeof(IntPtr),
                SpecialType.System_UIntPtr => typeof(UIntPtr),
                SpecialType.System_DateTime => typeof(DateTime),
                SpecialType.System_IDisposable => typeof(IDisposable),
                //SpecialType.System_Enum => expr,
                //SpecialType.System_ValueType => expr,
                //SpecialType.System_Array => expr,
                //SpecialType.System_Collections_IEnumerable => expr,
                //SpecialType.System_Collections_Generic_IEnumerable_T => expr,
                //SpecialType.System_Collections_Generic_IList_T => expr,
                //SpecialType.System_Collections_Generic_ICollection_T => expr,
                //SpecialType.System_Collections_IEnumerator => expr,
                //SpecialType.System_Collections_Generic_IEnumerator_T => expr,
                //SpecialType.System_Collections_Generic_IReadOnlyList_T => expr,
                //SpecialType.System_Collections_Generic_IReadOnlyCollection_T => expr,
                //SpecialType.System_Nullable_T => expr,
                //SpecialType.System_Runtime_CompilerServices_IsVolatile => expr,
                _ => throw new ArgumentOutOfRangeException(nameof(typeSymbol.SpecialType), typeSymbol.SpecialType.ToString(), "Unsupported type")
            };
        }

        var metadataName = GetFullyQualifiedMetadataName(typeSymbol);
        var type = Type.GetType(metadataName);

        if (type != null)
        {
            return type;
        }

        // Fallback : try to infer type based from referenced assemblies
        foreach (var reference in compilation.References)
        {
            if (reference is not PortableExecutableReference peReference)
            {
                continue;
            }
            var assembly = Assembly.LoadFrom(peReference.FilePath);
            type = assembly.GetType(metadataName);
            if (type != null)
            {
                return type;
            }
        }

        return typeof(object);
    }

    private static string GetFullyQualifiedMetadataName(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
    }

    private static string ComputeClassNameToGenerate(IMethodSymbol symbol)
    {
        return $"{symbol.ContainingType.ToDisplayString().Replace(".", "_")}_{symbol.Name}";
    }

    private static IReadOnlyCollection<string> ComputeUsings(InvocationExpressionSyntax forMethod, SemanticModel semanticModel)
    {
        var (_, typeSymbol) = GetInterfaceTypeInfo(forMethod, semanticModel);
        return [typeSymbol.ContainingNamespace.ToDisplayString()];
    }
}