using EvacuationPlanningApi;
using EvacuationPlanningApi.Cache;
using EvacuationPlanningApi.Service;
using EvacuationPlanningApi.Service.Interface;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IEvacuationRepository, EvacuationRepository>();
builder.Services.AddSingleton<IEvacuationService, EvacuationService>();

builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.AddRedisDistributedCache("cache");

var app = builder.Build();


app.MapOpenApi();
app.MapScalarApiReference("/scalar",
    (options, context) =>
    {
        if (context.Request.Scheme == "https")
            options.AddServer(new ScalarServer($"https://{context.Request.Host}"));
        options.AddServer(new ScalarServer($"http://{context.Request.Host}"));
    });

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
