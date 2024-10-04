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

        return new MockInformation(
            ComputeClassNameToGenerate(symbol),
            ComputeInterfaceToImplement(forMethod, semanticModel),
            ComputeUsings(forMethod, semanticModel),
            ComputeMethods(forMethod, semanticModel)
            );
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

    private static IReadOnlyCollection<Method> ComputeMethods(InvocationExpressionSyntax forMethod, SemanticModel semanticModel)
    {
        var (_, typeSymbol) = GetInterfaceTypeInfo(forMethod, semanticModel);

        var allMethods = typeSymbol.GetMembers().OfType<IMethodSymbol>();

        return allMethods
            .Select(x =>
            {
                var returnType = GetSystemTypeFromSymbol(x.ReturnType, semanticModel.Compilation);
                return new Method(x.Name, returnType);
            })
            .ToArray();
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
                SpecialType.System_Void => typeof(void),
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
                _ => throw new ArgumentOutOfRangeException(nameof(typeSymbol.SpecialType), typeSymbol.SpecialType.ToString(),  "Unsupported type")
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

    private static string ComputeInterfaceToImplement(InvocationExpressionSyntax forMethod, SemanticModel semanticModel)
    {
        var (interfaceType, _) = GetInterfaceTypeInfo(forMethod, semanticModel);
        return interfaceType.ToString();
    }

    private static IReadOnlyCollection<string> ComputeUsings(InvocationExpressionSyntax forMethod, SemanticModel semanticModel)
    {
        var (_, typeSymbol) = GetInterfaceTypeInfo(forMethod, semanticModel);
        return [typeSymbol.ContainingNamespace.ToDisplayString()];
    }
}