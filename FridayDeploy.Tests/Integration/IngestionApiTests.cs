using System.Net;
using System.Net.Http.Json;
using FridayDeploy.Web.Data;
using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FridayDeploy.Tests.Integration;

public sealed class IngestionApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public IngestionApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_is_anonymous_and_returns_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Post_single_log_is_anonymous_and_returns_created()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/logs", new
        {
            application = "IntegrationTest",
            level = "Information",
            message = "hello from a test"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_batch_without_api_key_is_rejected()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/logs/batch", new[]
        {
            new { level = "Information", message = "no key here" }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_batch_with_valid_api_key_ingests_and_attributes_to_the_keys_application()
    {
        var client = _factory.CreateClient();
        string rawKey;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ApiKeyService>();
            var created = await apiKeyService.CreateAsync("KeyedApp", CancellationToken.None);
            rawKey = created.RawKey;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs/batch")
        {
            Content = JsonContent.Create(new[] { new { level = "Warning", message = "spoof attempt", application = "SomeoneElse" } })
        };
        request.Headers.Add("X-Api-Key", rawKey);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = db.Logs.Single(x => x.Message == "spoof attempt");
        Assert.Equal("KeyedApp", log.Application);
    }

    [Fact]
    public async Task Post_batch_with_disabled_api_key_is_rejected()
    {
        var client = _factory.CreateClient();
        string rawKey;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ApiKeyService>();
            var created = await apiKeyService.CreateAsync("DisabledApp", CancellationToken.None);
            await apiKeyService.SetDisabledAsync(created.Id, true, CancellationToken.None);
            rawKey = created.RawKey;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/logs/batch")
        {
            Content = JsonContent.Create(new[] { new { level = "Information", message = "should be rejected" } })
        };
        request.Headers.Add("X-Api-Key", rawKey);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_logs_without_a_session_requires_authentication()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/logs");

        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect or HttpStatusCode.Found);
    }
}
