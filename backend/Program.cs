using VmtFabric.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// No revelar el servidor web en la cabecera "Server".
builder.WebHost.ConfigureKestrel(k => k.AddServerHeader = false);

// Development: la cadena de la base compartida se arma desde backend/.env (no versionado).
builder.Configuration.AddDotEnvDatabase(builder.Environment);

builder.Services
    .AddPersistence(builder.Configuration, builder.Environment)
    .AddApiServices()
    .AddReverseProxySupport(builder.Configuration)
    .AddFrontendCors(builder.Configuration);

var app = builder.Build();

app.ValidateEfModel()
   .UseApiPipeline();

app.Run();
