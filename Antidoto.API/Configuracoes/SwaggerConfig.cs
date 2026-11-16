using Antidoto.API.Filters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Antidoto.API.Configuracoes;

public static class SwaggerConfig
{
    public static IServiceCollection AddAntidotoSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Antídoto API",
                Version = "v1",
                Description = "SOS para acidentes com animais peçonhentos, unidades com soro e ocorrências. " +
                              "Gestor do seed: gestor@antidoto.gov.br / Gestor@2026."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Cole só o token (sem a palavra Bearer)."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            options.OperationFilter<IdempotencyKeyHeaderFilter>();
        });
        return services;
    }

    /// <summary>Mostra o header Idempotency-Key no Swagger do POST /api/ocorrencias.</summary>
    private class IdempotencyKeyHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.HttpMethod != "POST" ||
                context.ApiDescription.RelativePath?.TrimEnd('/') != "api/ocorrencias")
                return;

            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = IdempotenciaFilter<object>.Header,
                In = ParameterLocation.Header,
                Required = true,
                Description = "UUID único por tentativa. Repetir a mesma chave devolve a mesma resposta.",
                Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
            });
        }
    }
}
