using System;

namespace MetricsPipeline.Seeding;

public class SeedValidationException : Exception
{
    public string FileName { get; }
    public int LineNumber { get; }

    public SeedValidationException(string fileName, int lineNumber, Exception inner)
        : base($"Invalid seed data in {fileName} at line {lineNumber}", inner)
    {
        FileName = fileName;
        LineNumber = lineNumber;
    }
}
