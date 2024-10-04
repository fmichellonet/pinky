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
            ComputeInterfaceToImplement(forMethod),
            ComputeUsings(forMethod, semanticModel),
            ComputeMethods(forMethod, semanticModel)
            );
    }

    private static IReadOnlyCollection<Method> ComputeMethods(InvocationExpressionSyntax forMethod, SemanticModel semanticModel)
    {
        var memberAccess = forMethod.Expression as MemberAccessExpressionSyntax;

        if (memberAccess?.Name is GenericNameSyntax genericName)
        {

            var interfaceType = genericName.TypeArgumentList.Arguments.Single();

            var typeSymbol = semanticModel.GetSymbolInfo(interfaceType).Symbol as INamedTypeSymbol;
            var allMethods = typeSymbol.GetMembers().OfType<IMethodSymbol>();

            return allMethods
                .Select(x => new Method(x.Name, x.ReturnType.ToDisplayString()))
                .ToArray();
        }

        throw new ArgumentException("ggrr3");
    }


    private static string ComputeClassNameToGenerate(IMethodSymbol symbol)
    {
        return $"{symbol.ContainingType.ToDisplayString().Replace(".", "_")}_{symbol.Name}";
    }

    private static string ComputeInterfaceToImplement(InvocationExpressionSyntax forMethod)
    {
        var memberAccess = forMethod.Expression as MemberAccessExpressionSyntax;

        if (memberAccess?.Name is GenericNameSyntax genericName)
        {

            var interfaceType = genericName.TypeArgumentList.Arguments.Single();
            return interfaceType.ToString();
        }

        throw new ArgumentException("ggrr");
    }


    private static IReadOnlyCollection<string> ComputeUsings(InvocationExpressionSyntax forMethod, SemanticModel semanticModel)
    {
        var memberAccess = forMethod.Expression as MemberAccessExpressionSyntax;

        if (memberAccess?.Name is GenericNameSyntax genericName)
        {

            var interfaceType = genericName.TypeArgumentList.Arguments.Single();
            var typeSymbol = semanticModel.GetSymbolInfo(interfaceType).Symbol as INamedTypeSymbol;
            return new[] { typeSymbol.ContainingNamespace.ToDisplayString() };
            
        }
        throw new ArgumentException("ggrr2");
        
    }
}