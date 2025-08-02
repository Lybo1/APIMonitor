WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services .AddGraphQLServer();

WebApplication app = builder.Build();

app.MapGraphQL("/");

app.Run();
