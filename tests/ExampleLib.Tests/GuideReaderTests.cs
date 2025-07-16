using Moq;

namespace ExampleLib.Tests;

public class GuideReaderTests
{
    [Fact]
    public void ReadGuide_ReturnsContent()
    {
        var mock = new Mock<IGuideReader>();
        mock.Setup(r => r.ReadGuide()).Returns("Some content with YourDbContext");

        var result = mock.Object.ReadGuide();

        Assert.Contains("YourDbContext", result);
    }
}
