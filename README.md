# Metrics Pipeline Demo

This repository contains a simple metrics processing pipeline. The original Infrastructure project has been merged into `MetricsPipeline.Core`.

## Running the demo

A console application `MetricsPipeline.Console` demonstrates the pipeline.

```
# restore packages and run
 dotnet run --project MetricsPipeline.Console
```

The application fetches metrics from an in-memory source, computes an average and prints the result.
