using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Void = Pinky.Generator.SpecialTypes.Void;

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

    private static Dictionary<string, string> FindMethodReturnValues(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel)
    {
        var methodReturnValues = new Dictionary<string, string>();

        var invocations = methodSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Returns" } memberAccess)
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

    private static IReadOnlyCollection<Method> ComputeMethods(INamedTypeSymbol typeSymbol, SemanticModel semanticModel, Dictionary<string, string> methodReturnValues)
    {
        var allMethods = typeSymbol.GetMembers().OfType<IMethodSymbol>();

        return allMethods
            .Select(x =>
            {
                var desiredReturnValue = methodReturnValues.TryGetValue(x.Name, out var value) ? value : $"default({x.ReturnType.ToDisplayString()})";
                return new Method(x.Name,
                    new ReturnValue(
                        BuildType(x.ReturnType.ToDisplayString(), x.ReturnType.ContainingNamespace.Name),
                        desiredReturnValue)
                );
            })
            .ToArray();
    }

    private static string ExtractDesiredReturnValue(ExpressionSyntax? expression, SemanticModel semanticModel)
    {
        if (expression == null)
        {
            return "null";
        }

        var constantValue = semanticModel.GetConstantValue(expression);
        if (constantValue.HasValue)
        {
            return FormatDesiredValue(constantValue.Value!);
        }

        // Si ce n'est pas une constante, on pourrait avoir besoin d'une logique plus complexe ici
        // pour gérer d'autres types d'expressions
        return "null";
    }

    private static string FormatDesiredValue(object desired)
    {
        var formattedValue = desired switch
        {
            string s => SymbolDisplay.FormatLiteral(s, true),
            char c => SymbolDisplay.FormatLiteral(c, true),
            _ => desired!.ToString()
        };
        return formattedValue;
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

    private static Type BuildType(string name, string nameSpace)
    {
        return name switch
        {
            "void" => new Void(),
            _ => new Type(name, nameSpace)
        };
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