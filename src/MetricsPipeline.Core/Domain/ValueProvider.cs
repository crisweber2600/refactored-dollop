namespace MetricsPipeline.Core.Domain;

public class ValueProvider
{
    private int[] _values = Array.Empty<int>();

    public int[] GetValues() => _values;

    public void SetInitial() => _values = new[] { 10, 20 };
    public void SetWithin() => _values = new[] { 15, 25 };
    public void SetOutside() => _values = new[] { 100, 10 };
}
