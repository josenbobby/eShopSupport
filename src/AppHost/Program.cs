﻿using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var dbPassword = builder.AddParameter("PostgresPassword", secret: true);

var backendDb = builder
    .AddPostgres("eshopsupport-postgres", password: dbPassword)
    .WithDataVolume()
    .AddDatabase("backenddb");

var vectorDb = builder
    .AddContainer("vector-db", "qdrant/qdrant", "latest")
    .WithVolume("eshopsupport-vector-db-storage", "/qdrant/storage")
    .WithHttpEndpoint(port: 62392, targetPort: 6333);

var llmModelName = builder.Configuration["LlmModelName"]!;
var ollama = builder.AddOllama("eshopsupport-ollama", enableGpu: true, models: [llmModelName])
    .WithHttpEndpoint(port: 62393, targetPort: 11434);

var backend = builder.AddProject<Backend>("backend")
    .WithReference(backendDb)
    .WithReference(vectorDb.GetEndpoint("http"))
    .WithReference(builder.AddConnectionString("openAiConnection"))
    .WithEnvironment("LlmModelName", llmModelName)
    .WithEnvironment("ImportInitialDataDir", Path.Combine(builder.AppHostDirectory, "..", "..", "seeddata", "dev"));

builder.AddProject<StaffWebUI>("staffwebui")
    .WithExternalHttpEndpoints()
    .WithReference(backend);

builder.Build().Run();
