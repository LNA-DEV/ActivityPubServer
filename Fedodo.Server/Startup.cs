using System.Security.Cryptography;
using Fedodo.NuGet.Common.Handlers;
using Fedodo.NuGet.Common.Interfaces;
using Fedodo.NuGet.Common.Repositories;
using Fedodo.Server.APIs;
using Fedodo.Server.Handlers;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Fedodo.Server;

public class Startup
{
    public void AddSwagger(WebApplicationBuilder webApplicationBuilder)
    {
#if DEBUG
        var tokenUrl = new Uri("http://localhost/oauth/token");
        var authUrl = new Uri("http://localhost/oauth/authorize");
#else
        var tokenUrl = new Uri(
            $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/oauth/token");
        var authUrl = new Uri(
            $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/oauth/authorize");
#endif

        webApplicationBuilder.Services.AddSwaggerGen(
            c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApiPlayground", Version = "v1" });
                c.AddSecurityDefinition(
                    "oauth2",
                    new OpenApiSecurityScheme
                    {
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                Scopes = new Dictionary<string, string>
                                {
                                    ["email"] = "api scope description",
                                    ["profile"] = "api scope description",
                                    ["roles"] = "api scope description"
                                },
                                TokenUrl = tokenUrl,
                                AuthorizationUrl = authUrl
                            }
                        },
                        In = ParameterLocation.Header,
                        Name = HeaderNames.Authorization,
                        Type = SecuritySchemeType.OAuth2
                    }
                );

                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "oauth2", //The name of the previously defined security scheme.
                                    Type = ReferenceType.SecurityScheme
                                }
                            },
                            new List<string>()
                        }
                    });
            }
        );
    }

    public void AddApp(WebApplication app, bool httpLogging = false)
    {
        if (httpLogging) app.UseHttpLogging();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.OAuthClientId("swagger2");
            c.OAuthClientSecret("test");
            c.OAuthAppName("Swagger API");
            c.OAuthScopeSeparator(",");
            // c.OAuthUsePkce();
        });
        app.UseCors(x => x.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins("*"));

        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(options =>
        {
            options.MapRazorPages();
            options.MapControllers();
            options.MapFallbackToFile("index.html");
        });

        app.Run();
    }

    public void AddCustomServices(WebApplicationBuilder builder, MongoClient mongoClient1)
    {
        builder.Services.AddSingleton<IMongoDbRepository, MongoDbRepository>();
        builder.Services.AddSingleton<IHttpSignatureHandler, HttpSignatureHandler>();
        builder.Services.AddSingleton<IActivityHandler, ActivityHandler>();
        builder.Services.AddSingleton<IUserHandler, UserHandler>();
        builder.Services.AddSingleton<IMongoClient>(mongoClient1);
        builder.Services.AddSingleton<IAuthenticationHandler, AuthenticationHandler>();
        builder.Services.AddSingleton<IActorAPI, ActorApi>();
        builder.Services.AddSingleton<IActivityAPI, ActivityAPI>();
        builder.Services.AddSingleton<IKnownSharedInboxHandler, KnownSharedInboxHandler>();
        builder.Services.AddSingleton<ICollectionApi, CollectionApi>();
    }

    public void SetupMongoDb()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
        BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));
        var objectSerializer = new ObjectSerializer(type =>
            ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("Fedodo.Server"));
        BsonSerializer.RegisterSerializer(objectSerializer);
        BsonSerializer.RegisterDiscriminator(typeof(Post), "Post");
    }
}