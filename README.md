# refactored-dollop

This sample demonstrates a simple workflow engine using EF Core and MassTransit.

## Setup

```
dotnet add package refactored-dollop
```

Register the services in your application:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureWorkflow(builder.Configuration);
```

Provide a `Default` connection string pointing to a SQL Server instance.

## Running Tests

```
dotnet test
```
