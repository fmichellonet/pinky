using System.Collections.Generic;

namespace Pinky;

internal record GhostExtensionInformation(
    IReadOnlyCollection<MockInformation> MockInterfaces,
    IReadOnlyCollection<string> Usings);