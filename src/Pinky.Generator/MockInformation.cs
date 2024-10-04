using System.Collections.Generic;

namespace Pinky;

internal record MockInformation(
    string ClassNameToGenerate, 
    string InterfaceToImplement, 
    IReadOnlyCollection<string> Usings,
    IReadOnlyCollection<Method> Methods);


internal record Method(string Name, string ReturnType);