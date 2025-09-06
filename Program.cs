using UserManagementAPI.Services;
using UserManagementAPI.Models.Audit;
using UserManagementAPI.Models.Auth;
using UserManagementAPI.Middleware;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure JWT settings
        var jwtSettings = new JwtSettings();
        builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);

        // Use a default secret key if not configured (for demo purposes)
        if (string.IsNullOrEmpty(jwtSettings.SecretKey))
        {
            jwtSettings.SecretKey = "TechHive-UserManagement-SuperSecretKey-2025-MinLength32Characters!";
        }

        // Configure audit settings
        var auditConfig = new AuditConfiguration();
        builder.Configuration.GetSection("AuditSettings").Bind(auditConfig);

        // Register configurations as singletons
        builder.Services.AddSingleton(jwtSettings);
        builder.Services.AddSingleton(auditConfig);

        // Add services to the container
        builder.Services.AddControllers(options =>
        {
            // Configure model validation behavior
            options.ModelValidatorProviders.Clear();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep PascalCase
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            // Customize automatic model validation responses
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors?.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                var response = new
                {
                    Message = "Validation failed",
                    Errors = errors,
                    Timestamp = DateTime.UtcNow
                };

                return new BadRequestObjectResult(response);
            };
        });

        // Add OpenAPI/Swagger services
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "TechHive User Management API", Version = "v1" });

            // Add JWT authentication to Swagger
            c.AddSecurityDefinition("Bearer", new()
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter JWT Bearer token"
            });

            c.AddSecurityRequirement(new()
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Add custom services
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<IUserService, UserService>();
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Add logging
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddDebug();
            if (builder.Environment.IsDevelopment())
            {
                logging.SetMinimumLevel(LogLevel.Debug);
            }
        });

        // Add CORS policy for development
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        // Order is important: Error handling should be first, then authentication, then audit logging

        // 1. Error handling middleware (catches all exceptions)
        app.UseErrorHandling();

        // 2. CORS (if in development)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechHive User Management API v1");
                c.RoutePrefix = string.Empty; // Serve Swagger at root
            });
            app.UseCors("AllowAll");
        }

        // 3. HTTPS redirection
        app.UseHttpsRedirection();

        // 4. Authentication middleware (validates JWT tokens)
        app.UseJwtAuthentication();

        // 5. Audit logging middleware (logs all requests/responses)
        app.UseAuditLogging();

        // 6. Routing
        app.UseRouting();

        // 7. Controllers
        app.MapControllers();

        app.Run();
    }
}
