using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

builder.AddProject<EvacuationPlanningApi>("evacuationapi")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/scalar")
    .WithReference(cache)
    .WaitFor(cache);

builder.Build().Run();
