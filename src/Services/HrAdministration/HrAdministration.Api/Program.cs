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

app.MapDefaultEndpoints();

await app.RunAsync();
