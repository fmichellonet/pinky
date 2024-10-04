using System.Collections.Generic;

namespace Pinky;

public class GateKeeper
{
    private readonly Dictionary<RecordedCall, int> _recordedCalls = new();

    public void Track(string methodName, object[] parameters)
    {
        var call = new RecordedCall(methodName, parameters);
        if (!_recordedCalls.TryAdd(call, 1))
        {
            _recordedCalls[call]++;
        }
    }

    public void Check(string methodName, object[] parameters, int expectedCount)
    {
        var call = new RecordedCall(methodName, parameters);
        
        if (!_recordedCalls.TryGetValue(call, out var actualCount) && expectedCount != 0)
        {
            throw ReceivedCallsException.For(methodName, expectedCount, actualCount);
        }

        if (actualCount != expectedCount)
        {
            throw ReceivedCallsException.For(methodName, expectedCount, actualCount);
        }
    }

    internal record RecordedCall(string MethodName, IReadOnlyCollection<object> Parameters);
}