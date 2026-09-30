using Microsoft.OpenApi.Models;

namespace ShopNet.API.Extensions;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ShopNet E-Commerce API",
                Version = "v1",
                Description = "RESTful API Thương mại Điện tử chuẩn Clean Architecture (.NET 8)",
                Contact = new OpenApiContact
                {
                    Name = "ShopNet Dev Team",
                    Email = "dev@shopnet.com"
                }
            });

            // Add JWT Authentication to Swagger
            var securitySchema = new OpenApiSecurityScheme
            {
                Description = "Nhập JWT Bearer token theo định dạng: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            c.AddSecurityDefinition("Bearer", securitySchema);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                { securitySchema, new[] { "Bearer" } }
            };

            c.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShopNet API v1");
            c.RoutePrefix = string.Empty; // Serve Swagger UI at application root (http://localhost:5000/)
        });

        return app;
    }
}
