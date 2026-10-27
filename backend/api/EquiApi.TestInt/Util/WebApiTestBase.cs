using System.Net;
using System.Text.Json;
using EquiApi.Persistence.Util;
using EquiApi.Shared;

namespace EquiApi.TestInt.Util;

public abstract class WebApiTestBase(WebApiTestFixture webApiFixture) : IClassFixture<WebApiTestFixture>, IAsyncLifetime
{
    private const string ProblemJsonMediaType = "application/problem+json";

    private static readonly Lazy<JsonSerializerOptions> jsonOptions = new(() =>
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Web);
        JsonConfig.ConfigureJsonSerialization(options, false);

        return options;
    });

    protected static JsonSerializerOptions JsonOptions => jsonOptions.Value;

    protected HttpClient ApiClient => webApiFixture.Client;
    protected IClock TestClock => webApiFixture.Clock;
    protected CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await webApiFixture.RestoreDatabaseAsync(ImportSeedDataAsync);
    }

    public ValueTask DisposeAsync()
    {
        // nothing to dispose
        // database is reset during init to ensure a clean slate even if a test run is interrupted
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask ImportSeedDataAsync(DatabaseContext context)
    {
        // add seed data for all tests here if needed, override in derived classes to add test class specific seed data
        return ValueTask.CompletedTask;
    }

    protected async ValueTask ModifyDatabaseContentAsync(Func<DatabaseContext, ValueTask> modifier)
    {
        await webApiFixture.ModifyDatabaseContentAsync(modifier);
    }

    /// <summary>
    ///     Adds the given entities (including all entities reachable from them) to the database.
    ///     Generated ids are set on the passed instances afterwards.
    /// </summary>
    protected async ValueTask SeedAsync(params IEnumerable<object> entities)
    {
        await ModifyDatabaseContentAsync(async context =>
        {
            context.AddRange(entities);
            await context.SaveChangesAsync();
        });
    }

    /// <summary>
    ///     Sends a request as a logged-in user, see <see cref="TestAuthHandler" />
    /// </summary>
    protected async ValueTask<HttpResponseMessage> SendAsUserAsync(HttpMethod method, string url, string externalId,
                                                                   string? email = null, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.UserHeader, externalId);
        if (email is not null)
        {
            request.Headers.Add(TestAuthHandler.EmailHeader, email);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return await ApiClient.SendAsync(request, TestCancellationToken);
    }

    protected async ValueTask<T> ReadContentAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<T>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();

        return content;
    }

    /// <summary>
    ///     Asserts that the response is a problem details response with the expected status code.
    ///     This also makes sure that the endpoint exists, because a missing route results in a 404 without a body.
    /// </summary>
    protected static void ShouldBeProblem(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        response.StatusCode.Should().Be(expectedStatusCode);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.MediaType.Should().Be(ProblemJsonMediaType);
    }
}