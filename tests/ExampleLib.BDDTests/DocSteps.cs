using System.IO;
using Reqnroll;
using ExampleLib;

namespace ExampleLib.BDDTests;

public class FileGuideReader : IGuideReader
{
    public string ReadGuide()
    {
        return File.ReadAllText(Path.Combine("docs", "EFCoreReplicationGuide.md"));
    }
}

[Binding]
public class DocSteps
{
    private readonly IGuideReader _reader;
    private string _content = string.Empty;

    public DocSteps(IGuideReader reader)
    {
        _reader = reader;
    }

    [Given("the EF Core replication guide is read")]
    public void GivenTheGuideIsRead()
    {
        _content = _reader.ReadGuide();
    }

    [Then("it should contain \"(.*)\"")]
    public void ThenItShouldContain(string text)
    {
        if (!_content.Contains(text))
        {
            throw new Exception($"Guide does not contain '{text}'");
        }
    }
}
