using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var sql = builder.AddSqlServer("Sql").WithLifetime(ContainerLifetime.Persistent).WithDataVolume();
    var db = sql.AddDatabase("DB");
var mongo = builder.AddMongoDB("mongo").WithLifetime(ContainerLifetime.Persistent).WithDataVolume().AddDatabase("mongodb");
builder.AddProject<WorkerService1>("workerservice1").WithReference(sql).WithReference(db).WithReference(mongo).WaitFor(mongo).WaitFor(db);

builder.Build().Run();
