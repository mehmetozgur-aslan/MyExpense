// using MyExpense.API.Agent.Interfaces;
// using MyExpense.API.Agent.Services;
using MyExpense.API.Api.Endpoints;
using MyExpense.API.Api.Mcp;
using MyExpense.API.Application.Interfaces;
using MyExpense.API.Application.Services;
using MyExpense.API.Infrastructure.Data;
using MyExpense.API.Infrastructure.Middleware;
using Microsoft.EntityFrameworkCore;
//using MyExpense.API.Agent.Extensions;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ExpenseDbContext>(options =>
{
    options.UseInMemoryDatabase("InMemoryDb");
});

#if DEBUG
builder.Services.AddCors(options =>
{
    options.AddPolicy("McpInspector", policy =>
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Mcp-Session-Id"));
});
#endif


// Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IExpenseApplicationService, ExpenseApplicationService>();
//builder.Services.AddAiOrchestration(builder.Configuration);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services
        .AddMcpServer()
        //.WithTools<ExpenseMcpTools>()
        .WithToolsFromAssembly()
        .WithHttpTransport()
        //.WithStdioServerTransport()
        ;


var app = builder.Build();

#if DEBUG
app.UseCors("McpInspector");
#endif


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await DataSeeder.SeedAsync(app.Services);
app.UseExceptionHandler();


#if !DEBUG
app.Use(async (context, next) =>
{
    var requiredKey = app.Configuration["ApiAuth:ApiKey"];
    if (!string.IsNullOrWhiteSpace(requiredKey) &&
        (!context.Request.Headers.TryGetValue("X-Api-Key", out var key) || key != requiredKey))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }
    await next();
});
#endif


app.UseHttpsRedirection();
app.MapExpenseEndpoints();

app.MapMcp("/mcp");

app.Run();