
using Aspire.Hosting.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole(); // Konsola loglama ekle
});

var configuration = builder.Configuration;
var postgresConfig = configuration.GetSection("Postgres");

var username = builder.AddParameter("postgresuser", secret: true);
var password = builder.AddParameter("postgrespassword", secret: true);

var postgresServer = builder.AddPostgres(
    name: "postgres-server",
    userName: username,
    password: password,
    port: 5432
).WithHealthCheck();

var postgreAdmin = postgresServer.WithPgAdmin(c => c.WithHostPort(5050).WaitFor(dependency: postgresServer));


var postgresProductDb = postgresServer.AddDatabase("pDb", databaseName: "ProductDb");

builder.AddProject<Projects.ExampleWebAPI>("examplewebapi")
    .WithReference(postgresProductDb)
    .WithEnvironment("ConnectionStrings__DefaultConnection", builder.Configuration.GetConnectionString("DefaultConnection"))
    .WaitFor(dependency: postgresServer);

builder.Build().Run();
