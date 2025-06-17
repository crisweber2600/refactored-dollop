using MetricsPipeline.Core;
using Xunit;
using System;
using System.Linq;

public class PipelineStateTests
{
    [Fact]
    public void RecordStoresValues()
    {
        var ts = DateTime.UtcNow;
        var metrics = new double[] { 1.0, 2.0 };
        var state = new PipelineState<double>("pipe", new Uri("https://example.com"), metrics, 3.0, 2.0, 1.5, ts);

        Assert.Equal("pipe", state.PipelineName);
        Assert.Equal(new Uri("https://example.com"), state.SourceEndpoint);
        Assert.True(metrics.SequenceEqual(state.RawItems));
        Assert.Equal(3.0, state.Summary);
        Assert.Equal(2.0, state.LastCommittedSummary);
        Assert.Equal(1.5, state.AcceptableDelta);
        Assert.Equal(ts, state.Timestamp);
    }
}
