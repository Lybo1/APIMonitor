using HotChocolate;
using HotChocolate.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
       .AddGraphQLServer()
       .AddQueryType<Query>()
       .AddAuthorization();

WebApplication app = builder.Build();

app.MapGraphQL();
app.MapGet("/", () => "GraphQL API is running. Use /graphql endpoint.");

app.Run();

internal sealed class Query
{
    public string Hello() => "Hello World!";
}