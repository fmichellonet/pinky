using NUnit.Framework;


namespace Pinky.Samples;

[TestFixture]
public class ShouldMockEmptyInterface
{

    [Test]
    public void Empty()
    {
        var instance = Ghost.For<IEmptyInterface>();

        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void Empty2()
    {
        var instance = Ghost.For<IEmptyInterface>();

        Assert.That(instance, Is.Not.Null);
    }

}

public interface IEmptyInterface { }