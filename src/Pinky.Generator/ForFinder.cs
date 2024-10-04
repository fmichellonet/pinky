using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pinky.Generator;

internal class ForFinder : CSharpSyntaxWalker
{
    private bool IsForConstructHasBeenFound { get; set; }

    private InvocationExpressionSyntax ForMethod { get; set; }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "For", Expression: IdentifierNameSyntax { Identifier.Text: "Ghost" } })
        {
            IsForConstructHasBeenFound = true;
            ForMethod = node;
        }
        base.VisitInvocationExpression(node);
    }
    
    public static bool IsForConstruct(SyntaxNode node)
    {
        var finder = new ForFinder();
        finder.Visit(node);
        return finder.IsForConstructHasBeenFound;
    }

    public static InvocationExpressionSyntax GetForMethodSyntax(SyntaxNode node)
    {
        var finder = new ForFinder();
        finder.Visit(node);
        return finder.ForMethod;
    }
}