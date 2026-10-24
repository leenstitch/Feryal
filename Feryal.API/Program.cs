using Feryal.Infrastructure.Extensions;
using Feryal.Infrastructure.Identity;
using Microsoft.OpenApi;
using Feryal.API.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;

using Feryal.Application.Validators;

// Creates the application builder and loads the application configuration.
var builder = WebApplication.CreateBuilder(args);

// Registers MVC controllers.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

// Registers validators from the assembly containing CreateProductRequestValidator.
//builder.Services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

// Enables automatic FluentValidation integration with ASP.NET Core.
builder.Services.AddFluentValidationAutoValidation();

// Configures Swagger documentation and JWT Bearer authentication.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Feryal API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

// Registers hybrid caching services.
//builder.Services.AddHybridCache();

// Registers infrastructure services and configures them using application settings.
builder.Services.AddInfrastructure(
    builder.Configuration);

// Configures Swagger schema IDs to use fully qualified type names.
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
});

// Builds the web application.
var app = builder.Build();

// Registers middleware for handling application exceptions.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Creates a dependency injection scope and seeds the identity data.
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(
        scope.ServiceProvider);
}

// Enables Swagger JSON document generation.
app.UseSwagger();

// Configures the Swagger UI and its API documentation endpoint.
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Feryal API V1");
});

// Redirects HTTP requests to HTTPS.
app.UseHttpsRedirection();

// Enables authentication middleware to identify the current user.
app.UseAuthentication();

// Enables authorization middleware to enforce access policies.
app.UseAuthorization();

// Maps controller actions to HTTP endpoints.
app.MapControllers();

// Starts the application.
app.Run();