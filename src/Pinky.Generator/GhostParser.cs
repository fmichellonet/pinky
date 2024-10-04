using System;
using System.Collections.Generic;
using System.Linq;
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
            .Select(x => new Method(x.Name, x.ReturnType.ToDisplayString()))
            .ToArray();
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