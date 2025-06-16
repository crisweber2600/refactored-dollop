namespace MetricsPipeline.Core;

public class HardDeleteNotPermittedException : Exception
{
    public HardDeleteNotPermittedException() : base("Hard delete is not permitted") {}
}
