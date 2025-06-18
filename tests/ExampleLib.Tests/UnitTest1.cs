using ExampleLib;
using Moq;

namespace ExampleLib.Tests;

public class UnitTest1
{
    [Fact]
    public void Add_ReturnsSum()
    {
        var mock = new Mock<ICalculator>();
        mock.Setup(m => m.Add(2, 3)).Returns(5);

        var result = mock.Object.Add(2, 3);

        Assert.Equal(5, result);
    }
}
