using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Pinky.Generator;

[Generator(LanguageNames.CSharp)]
public class MockGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //if (!Debugger.IsAttached)
        //{
        //    Debugger.Launch();
        //}

        var provider = context
            .SyntaxProvider
            .ForAttributeWithMetadataName("NUnit.Framework.TestAttribute",
                static (syntaxNode, _) => ForFinder.IsForConstruct(syntaxNode),
                static (syntaxContext, _) => GhostParser.Parse((IMethodSymbol)syntaxContext.TargetSymbol, syntaxContext.SemanticModel))
            .Where(static m => m is not null);

        var allContexts = provider.Collect();

        context.RegisterSourceOutput(allContexts, (sourceProductionContext, allMetadata) =>
        {
            for (var index = 0; index < allMetadata.Length; index++)
            {
                var metadata = allMetadata[index];
                var mockCode = SourceText.From(GenerateMockClass(metadata), Encoding.UTF8);
                sourceProductionContext.AddSource($"Pinky.Mock{index}.g.cs", mockCode);
            }

            var distinctUsing = allMetadata
                .SelectMany(x => x.Usings)
                .Distinct()
                .ToArray();

            var extensionCode = SourceText.From(GenerateGhostClass(new GhostExtensionInformation(allMetadata, distinctUsing)), Encoding.UTF8);
            sourceProductionContext.AddSource("Pinky.g.cs", extensionCode);
        });
        
    }

    private static string GenerateGhostClass(GhostExtensionInformation metadata)
    {
        return $$"""
                 using System;
                 using Pinky.Mocks;
                 {{GenerateUsings(metadata.Usings)}}
                 
                 namespace Pinky;
                 
                 public static class Ghost
                 {
                    {{GenerateExtensionMethod(metadata.MockInterfaces)}}
                 }
                 
                 """;
    }

    private static string GenerateExtensionMethod(IReadOnlyCollection<MockInformation> interfaces)
    {
        return $$"""
                  public static TInterface For<TInterface>() 
                  {
                      var (methodName, declaringType) = GhostHelpers.GetCallingMethodInfo();
                      object instance = GhostHelpers.GetTypeNameToInstantiate(methodName, declaringType) switch
                      {
                         {{GenerateSwitchArmClassConstructors(interfaces.Select(x => x.ClassNameToGenerate).ToArray())}}
                          _ => throw new NotImplementedException()
                      };
                      return (TInterface) instance;
                  }
                  
                  public static TInterface Received<TInterface>(this TInterface inter, int count)
                  {
                      var typeName = inter.GetType().Name;
                  
                      object instance = typeName switch
                      {
                         {{GenerateSwitchArmReceivedCall(interfaces.Select(x => x.ClassNameToGenerate).ToArray())}}
                          
                          _ => throw new NotImplementedException()
                      };
                      return (TInterface)instance;
                  }
                  
                  public static TInterface DidNotReceived<TInterface>(this TInterface inter)
                  {
                      return inter.Received<TInterface>(0);
                  }
                  """;
    }

    private static string GenerateMockClass(MockInformation metadata)
    {
        return $$"""
                 using System;
                 using Pinky;
                 {{GenerateUsings(metadata.Usings)}}
     
                 namespace Pinky.Mocks;
     
                 public class {{metadata.ClassNameToGenerate}} : {{metadata.InterfaceToImplement}}{
                 
                    private readonly GateKeeper _gateKeeper = new();
                 
                    {{GenerateMethods(metadata.Methods)}}
                    
                    public {{metadata.ClassNameToGenerate}} Received(int count) => new Verifier(_gateKeeper, count);
                 
                    private class Verifier(GateKeeper gateKeeper, int count) : {{metadata.ClassNameToGenerate}}
                    {
                        private readonly int _count = count;
                        private readonly GateKeeper _gateKeeper = gateKeeper;
                     
                        {{GenerateVerifierMethods(metadata.Methods)}}
                    }
                 }
                 """;
    }

    private const string NewLine = "\r\n";

    private static string GenerateUsings(IReadOnlyCollection<string> metadataUsings)
    {
        return metadataUsings.Aggregate(string.Empty, (current, item) => $"{current}using {item};{NewLine}");
    }

    private static string GenerateSwitchArmClassConstructors(IReadOnlyCollection<string> classNamesToGenerate)
    {
        return classNamesToGenerate.Aggregate(string.Empty, (current, item) => $"{current}nameof({item}) => new {item}(),{NewLine}");
    }

    private static string GenerateSwitchArmReceivedCall(IReadOnlyCollection<string> classNamesToGenerate)
    {
        return classNamesToGenerate.Aggregate(string.Empty, (current, item) => $"{current}nameof({item}) => (inter as {item}).Received(count),{NewLine}");
    }
    

    private static string GenerateMethods(IReadOnlyCollection<Method> methods)
    {
        return methods.Aggregate(string.Empty, (current, item) => $"{current}{GenerateMethod(item)}{NewLine}");
    }

    private static string GenerateMethod(Method method)
    {
        return $$"""
                 public virtual {{method.ReturnType.ToFriendlyString()}} {{method.Name}}(){
                    _gateKeeper.Track("{{method.Name}}", Array.Empty<object>());
                    {{GenerateMethodReturnType(method)}}
                 }
                 """;
    }

    private static string GenerateMethodReturnType(Method method)
    {
        if (method.ReturnType == typeof(void))
        {
            return string.Empty;
        }

        return "return default;";
    }

    private static string GenerateVerifierMethods(IReadOnlyCollection<Method> methods)
    {
        return methods.Aggregate(string.Empty, (current, item) => $"{current}{GenerateVerifierMethod(item)}{NewLine}");
    }

    private static string GenerateVerifierMethod(Method method)
    {
        return $$"""
                 public override {{method.ReturnType.ToFriendlyString()}} {{method.Name}}(){
                    _gateKeeper.Check("{{method.Name}}", Array.Empty<object>(), count);
                    {{GenerateMethodReturnType(method)}}
                 }
                 """;
    }
}