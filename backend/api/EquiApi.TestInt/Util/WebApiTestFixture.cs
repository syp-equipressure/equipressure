using EquiApi.Persistence.Util;
using EquiApi.Util;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EquiApi.TestInt.Util;

// ReSharper disable once ClassNeverInstantiated.Global - Instantiated by xUnit
public sealed class WebApiTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:17")
                                                              .WithDatabase("equipressure")
                                                              .WithUsername("postgres")
                                                              .WithPassword("postgres")
                                                              .Build();

    public const string DefaultTestUser = "test-user";

    /// <summary>
    ///     Client logged in as <see cref="DefaultTestUser" /> with the admin role, so it may access all data.
    ///     The access rules are tested with <see cref="WebApiTestBase.SendAsUserAsync" /> and real persons
    /// </summary>
    public HttpClient Client
    {
        get => field ?? throw new InvalidOperationException("Client not created");
        private set;
    }

    /// <summary>
    ///     Client without a logged-in user
    /// </summary>
    public HttpClient AnonymousClient
    {
        get => field ?? throw new InvalidOperationException("Client not created");
        private set;
    }

    public IClock Clock
    {
        get => field ?? throw new InvalidOperationException("Clock not created");
        private set;
    }

    private WebAppFactory Factory
    {
        get => field ?? throw new InvalidOperationException("Factory not created");
        set;
    }

    public async ValueTask InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        Factory = new WebAppFactory(_postgresContainer.GetConnectionString());
        Client = Factory.CreateClient();
        Client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, DefaultTestUser);
        Client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, ClaimsPrincipalExtensions.AdminRole);
        AnonymousClient = Factory.CreateClient();
        Clock = Factory.TestClock;
    }

    /// <summary>
    ///     Creates a client for websocket endpoints of the test server, logged in like <see cref="Client" />
    ///     or as the given user
    /// </summary>
    public Microsoft.AspNetCore.TestHost.WebSocketClient CreateWebSocketClient(string? externalId = null)
    {
        var client = Factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request =>
        {
            request.Headers[TestAuthHandler.UserHeader] = externalId ?? DefaultTestUser;
            if (externalId is null)
            {
                request.Headers[TestAuthHandler.RoleHeader] = ClaimsPrincipalExtensions.AdminRole;
            }
        };

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    public async ValueTask RestoreDatabaseAsync(Func<DatabaseContext, ValueTask> seedDataImporter)
    {
        await using var contextScope = CreateContextScope();

        // Drop schema, if it exists
        await contextScope.Context.Database
                          .ExecuteSqlRawAsync($"DROP SCHEMA IF EXISTS \"{DatabaseContext.SchemaName}\" CASCADE;");

        // Apply migrations (which also seed the account roles) and ensure the database is created
        await contextScope.Context.Database.MigrateAsync();

        await seedDataImporter(contextScope.Context);
    }

    public async ValueTask ModifyDatabaseContentAsync(Func<DatabaseContext, ValueTask> modifier)
    {
        await using var contextScope = CreateContextScope();

        await modifier(contextScope.Context);
    }

    private ContextScope CreateContextScope() => new(Factory.Services);

    private sealed class ContextScope : IAsyncDisposable
    {
        private readonly AsyncServiceScope _scope;

        public ContextScope(IServiceProvider serviceProvider)
        {
            _scope = serviceProvider.CreateAsyncScope();
            Context = _scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        public DatabaseContext Context { get; }

        // the context is owned by the scope and disposed together with it
        public ValueTask DisposeAsync() => _scope.DisposeAsync();
    }
}