using System.Text;
using C4.Host;
using C4.Modules.Discovery.Api;
using C4.Modules.Graph.Api;
using C4.Modules.Identity.Api;
using C4.Modules.Telemetry.Api;
using C4.Modules.Feedback.Api;
using C4.Modules.Visualization.Api;
using C4.Modules.Visualization.Api.Hubs;
using C4.Shared.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        string signingKey = builder.Configuration["Jwt:SigningKey"] ?? "c4-development-signing-key-min-32-chars!!";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "c4-api",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "c4-web",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetValue<string>("Frontend:Origin") ?? "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddIdentityModule(builder.Configuration)
    .AddDiscoveryModule(builder.Configuration)
    .AddGraphModule(builder.Configuration)
    .AddTelemetryModule(builder.Configuration)
    .AddVisualizationModule(builder.Configuration)
    .AddFeedbackModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await SeedDataService.MigrateAndSeedAsync(app);
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapHealthChecks("/health");
app.MapHub<DiagramHub>("/hubs/diagram");
app.MapEndpoints();

app.Run();

public partial class Program { }
