using FastEndpoints.Swagger;
using NSwag;
using NSwag.Generation.AspNetCore;
using Scalar.AspNetCore;

namespace RepoAPI.Util;

public static class SwaggerExtensions {
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services) {
        services
            .SwaggerDocument(o => {
                o.ShortSchemaNames = true;
                o.MaxEndpointVersion = 1;
                o.MinEndpointVersion = 1;
                o.DocumentSettings = doc => {
                    doc.ConfigureApiDoc("v1");
                };
            });

        return services;
    }

    public static void ConfigureApiDoc(this AspNetCoreOpenApiDocumentGeneratorSettings doc, string version) {
        doc.MarkNonNullablePropsAsRequired();
        doc.DocumentName = version;
        doc.Version = version;
        
        doc.SchemaSettings.FlattenInheritanceHierarchy = true;
        doc.SchemaSettings.SchemaProcessors.Add(new EnumAttributeSchemaProcessor());
    }
    
    public static WebApplication UseOpenApiConfiguration(this WebApplication app) {
        app.UseOpenApi(c => {
            c.Path = "/openapi/{documentName}.json";
            c.PostProcess = (document, _) => {
                document.Info = CreateInfoForApiVersion(document.Info.Version);
            };
        });
        app.MapScalarApiReference("/", opt => {
            opt.Title = "Skyblock Repo API";
            opt.Favicon = "https://skyblockrepo.com/favicon.ico";
        });
        return app;
    }
    
    private static OpenApiInfo CreateInfoForApiVersion(string version) {
        const string description = 
            """
            A backend API for https://skyblockrepo.com/ that provides Hypixel Skyblock data.
            <br><br>
            Use of this API requires following the [Skyblock Repo API TOS](https://skyblockrepo.com/api-terms). This API is not affiliated with Hypixel or Mojang.
            """;
        
        var info = new OpenApiInfo
        {
            Title = "Skyblock Repo API",
            Version = version,
            Contact = new OpenApiContact
            {
                Name = "GitHub",
                Url = "https://github.com/SkyblockRepo/RepoTools"
            },
            License = new OpenApiLicense 
            {
                Name = "MIT",
                Url = "https://github.com/SkyblockRepo/RepoTools/blob/main/LICENSE"
            },
            TermsOfService = "https://skyblockrepo.com/api-terms",
            Description = description
        };

        return info;
    }
}