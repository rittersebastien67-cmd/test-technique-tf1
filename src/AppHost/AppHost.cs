var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL server, one database per host (AD50). Payroll lives in the
// HrAdministration database under its own schema (AD57). No RabbitMQ nor Redis in V1 (AD56).
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var hrAdministrationDb = postgres.AddDatabase("hr-administration-db", databaseName: "hr_administration");
var assistantDb = postgres.AddDatabase("assistant-db", databaseName: "assistant");

var hrAdministration = builder.AddProject<Projects.HrAdministration_Api>("hr-administration")
    .WithReference(hrAdministrationDb)
    .WaitFor(hrAdministrationDb)
    .WithEnvironment("Database__MigrateOnStartup", "true");

var assistant = builder.AddProject<Projects.Assistant_Api>("assistant")
    .WithReference(assistantDb)
    .WaitFor(assistantDb)
    .WithReference(hrAdministration);

// The BFF is the only public entry point (ARCHITECTURE §4).
builder.AddProject<Projects.Gateway_Bff>("bff")
    .WithReference(hrAdministration)
    .WithReference(assistant)
    .WithExternalHttpEndpoints();

builder.Build().Run();
