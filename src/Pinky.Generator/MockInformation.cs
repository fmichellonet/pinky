using System.Collections.Generic;

namespace Pinky;

internal record MockInformation(
    string ClassNameToGenerate,
    string InterfaceToImplement,
    IReadOnlyCollection<string> Usings,
    IReadOnlyCollection<IMethod> Methods);