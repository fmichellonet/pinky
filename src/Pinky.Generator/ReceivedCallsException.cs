using System;

namespace Pinky;

public class ReceivedCallsException : Exception
{
    private ReceivedCallsException(string message) : base(message) {}

    public static ReceivedCallsException For(string methodName, int expectedCalls, int actualCalls)
    {
        return new ReceivedCallsException($"""
                                           Expected to receive exactly {expectedCalls} call(s) matching:
                                           	{methodName}()
                                           Actually received {actualCalls} matching call(s):
                                           	{methodName}()
                                           """);
    }
}