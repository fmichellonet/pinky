namespace Pinky;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class GhostHelpers
{
    public static (string MethodName, Type DeclaringType) GetCallingMethodInfo([CallerMemberName] string callerName = "")
    {
        var stackTrace = new StackTrace();
        for (var i = 1; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            if (method.Name == callerName)
            {
                var caller = stackTrace.GetFrame(i + 1).GetMethod();
                return (caller.Name, caller.DeclaringType);
            }
        }

        throw new ArgumentException("Unable to find the calling method");
    }

    public static string GetTypeNameToInstantiate(string methodName, Type declaringType)
    {
        return $"{declaringType.FullName.Replace(".", "_")}_{methodName}";
    }
}