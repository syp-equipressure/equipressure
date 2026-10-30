using System.Threading.RateLimiting;
using EquiApi.Core.Util;
using EquiApi.Persistence.Util;
using EquiApi.Util;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EquiApi;

public static class Setup
{
    public const string CorsPolicyName = "DefaultCorsPolicy";

    /// <summary>
    ///     Query parameter with the access token, only accepted for the chat websocket and the content of chat files
    /// </summary>
    public const string AccessTokenQueryKey = "access_token";

    public const string RegisterRateLimitPolicy = "Register";

    // browsers cannot send an Authorization header when opening a websocket or loading an image (<img src>)
    private static bool AllowsTokenInQuery(PathString path) =>
        path.Value is { } value
        && value.StartsWith("/api/persons/", StringComparison.Ordinal)
        && (value.EndsWith("/chats/ws", StringComparison.Ordinal)
            || (value.Contains("/chats/messages/", StringComparison.Ordinal)
                && value.EndsWith("/file", StringComparison.Ordinal)));

    extension(IServiceCollection services)
    {
        public void AddApplicationServices(IConfigurationManager configurationManager,
                                           bool isDev)
        {
            services.ConfigurePersistence(configurationManager, isDev);
            services.ConfigureCore();
            services.AddSingleton<IChatSocketManager, ChatSocketManager>();
        }

        public Settings LoadAndConfigureSettings(IConfigurationManager configurationManager)
        {
            var configSection = configurationManager.GetSection(Settings.SectionKey);

            services.Configure<Settings>(s => configSection.Bind(s));

            // different instance, but the same values - used for startup config outside of DI context
            var settings = Activator.CreateInstance<Settings>();
            configSection.Bind(settings);

            return settings;
        }
    }

    extension(WebApplicationBuilder builder)
    {
        public void AddLogging()
        {
            builder.Logging.ClearProviders();
            builder.Host.UseSerilog((_, _, config) =>
            {
                config
                    .ReadFrom.Configuration(builder.Configuration)
                    .Enrich.FromLogContext()
                    .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            });
        }
    }

    extension(IServiceCollection services)
    {
        public void AddCors(Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ClientOrigin))
            {
                throw new InvalidOperationException("Client origin has to be configured");
            }

            services.AddCors(o => o.AddPolicy(CorsPolicyName, builder =>
            {
                builder.SetIsOriginAllowed(_ => true)
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials();
            }));

            Log.Logger.Debug("Added CORS policy with client origin {ClientOrigin}", settings.ClientOrigin);
        }

        /// <summary>
        ///     Validates the access tokens (JWT) issued by Keycloak, the backend itself never handles logins
        /// </summary>
        public void AddKeycloakAuthentication(IConfigurationManager configurationManager)
        {
            var keycloakSettings = configurationManager.GetSection(KeycloakSettings.SectionKey)
                                                       .Get<KeycloakSettings>()
                                   ?? throw new InvalidOperationException("Keycloak has to be configured");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Authority = keycloakSettings.Authority;
                        options.Audience = keycloakSettings.Audience;
                        options.RequireHttpsMetadata = keycloakSettings.RequireHttpsMetadata;
                        // keep the original claim names (sub, email, ...) instead of the long Microsoft URIs
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters.RoleClaimType = ClaimsPrincipalExtensions.RoleClaimType;
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                if (AllowsTokenInQuery(context.Request.Path)
                                    && context.Request.Query.TryGetValue(AccessTokenQueryKey, out var token))
                                {
                                    context.Token = token;
                                }

                                return Task.CompletedTask;
                            }
                        };
                    });
            services.AddAuthorization();
            services.AddSingleton(keycloakSettings);
            services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>();
            // anybody can register, so the number of attempts is limited per address
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(RegisterRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                                      context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                                      _ => new FixedWindowRateLimiterOptions
                                      {
                                          PermitLimit = 10,
                                          Window = TimeSpan.FromMinutes(1)
                                      }));
            });

            Log.Logger.Debug("Added Keycloak authentication with authority {Authority}", keycloakSettings.Authority);
        }

        public void ConfigureAdditionalRouteConstraints()
        {
            services.Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add(nameof(LocalDate),
                                          typeof(LocalDateRouteConstraint));
            });
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        ///     Migrates the database and adds the development test data if the database is empty
        /// </summary>
        public async ValueTask SeedDevelopmentDataAsync()
        {
            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            await context.Database.MigrateAsync();
            bool seeded = await DevelopmentSeedData.SeedAsync(context);

            Log.Logger.Information(seeded
                                       ? "Added development test data to the empty database"
                                       : "Database already contains data, development test data was not added");
        }
    }
}