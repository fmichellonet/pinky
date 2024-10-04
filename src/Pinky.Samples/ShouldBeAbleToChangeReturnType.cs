using NUnit.Framework;

namespace Pinky.Samples;

[TestFixture]
public class ShouldBeAbleToChangeReturnType
{

    [Test]
    public void ChangeIntReturn()
    {
        // Arrange
        var instance = Ghost.For<IWithIntMethod>();

        const int desiredReturnValue = 15;
        instance
            .IntMethod()
            .Returns(desiredReturnValue);

        // Act
        var res = instance.IntMethod();

        // Assert
        Assert.That(res, Is.EqualTo(desiredReturnValue));
    }

    [Test]
    public void ChangeCharReturn()
    {
        // Arrange
        var instance = Ghost.For<IWithCharMethod>();

        const char desiredReturnValue = 'x';
        instance
            .CharMethod()
            .Returns(desiredReturnValue);

        // Act
        var res = instance.CharMethod();

        // Assert
        Assert.That(res, Is.EqualTo(desiredReturnValue));
    }

    [Test]
    public void ChangeStringReturn()
    {
        // Arrange
        var instance = Ghost.For<IWithStringMethod>();

        const string desiredReturnValue = "hello world";
        instance
            .StringMethod()
            .Returns(desiredReturnValue);

        // Act
        var res = instance.StringMethod();

        // Assert
        Assert.That(res, Is.EqualTo(desiredReturnValue));
    }
}


