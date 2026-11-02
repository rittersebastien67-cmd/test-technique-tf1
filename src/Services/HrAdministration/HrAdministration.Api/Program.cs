using HrAdministration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The name of the database declared in the AppHost.
builder.Services.AddHrAdministrationInfrastructure(builder.Configuration.GetConnectionString("hr-administration-db"));

var app = builder.Build();

// Schema applied on startup in development only (AD51): the AppHost asks for it.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.MigrateHrAdministrationDatabaseAsync();
}

// On request only, before the host accepts requests: an append during the replay would be missed.
if (app.Configuration.GetValue<bool>("Database:RebuildProjectionsOnStartup"))
{
    await app.Services.RebuildHrAdministrationProjectionsAsync();
}

app.MapDefaultEndpoints();

await app.RunAsync();
