using NUnit.Framework;

namespace Pinky.Samples;

[TestFixture]
public class ShouldMockInterfaceWithMethod
{

    [Test]
    public void GenerateCorrectlyVoidMethod()
    {
        // Arrange
        var instance = Ghost.For<IWithVoidMethod>();

        // Assert
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void GenerateCorrectlyIntMethod()
    {
        // Arrange
        var instance = Ghost.For<IWithIntMethod>();

        // Act
        var res = instance.IntMethod();

        // Assert
        Assert.That(instance, Is.Not.Null);
        Assert.That(res, Is.EqualTo(default(int)));
    }

    [Test]
    public void Track_MethodCall()
    {
        // Arrange
        var instance = Ghost.For<IWithVoidMethod>();

        // Act
        instance.VoidMethod();

        // Assert
        instance
            .Received(1)
            .VoidMethod();
    }

    [Test]
    public void Throws_When_Call_Count_Differs()
    {
        // Arrange
        var instance = Ghost.For<IWithVoidMethod>();

        // Act
        instance.VoidMethod();

        // Assert
        Assert.Throws<ReceivedCallsException>(() => instance
                .Received(2)
                .VoidMethod(),
            "Expected to receive exactly 2 call(s) matching:\r\n\tVoidMethod()\r\nActually received 1 matching call(s):\r\n\tVoidMethod()");
    }

    [Test]
    public void Accept_No_Receiving_Call()
    {
        // Arrange
        var instance = Ghost.For<IWithVoidMethod>();

        instance
            .DidNotReceived()
            .VoidMethod();
    }
}

public interface IWithVoidMethod
{
    void VoidMethod();
}

public interface IWithIntMethod
{
    int IntMethod();
}

public interface IWithCharMethod
{
    char CharMethod();
}

public interface IWithStringMethod
{
    string StringMethod();
}

