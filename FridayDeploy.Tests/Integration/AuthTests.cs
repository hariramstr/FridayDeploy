using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FridayDeploy.Tests.Integration;

public sealed class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_with_correct_default_admin_credentials_signs_in_and_redirects_to_dashboard()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (token, cookies) = await GetAntiForgeryTokenAsync(client, "/Auth/Login");

        var response = await PostFormAsync(client, "/Auth/Login", cookies, new Dictionary<string, string>
        {
            ["Username"] = "admin",
            ["Password"] = "ChangeThisPassword123!",
            ["__RequestVerificationToken"] = token
        });

        // The default route maps Dashboard/Index to "/", so a successful login redirects to the site root.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_with_wrong_password_does_not_sign_in()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (token, cookies) = await GetAntiForgeryTokenAsync(client, "/Auth/Login");

        var response = await PostFormAsync(client, "/Auth/Login", cookies, new Dictionary<string, string>
        {
            ["Username"] = "admin",
            ["Password"] = "definitely-wrong",
            ["__RequestVerificationToken"] = token
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid username or password", body);
    }

    private static async Task<(string Token, string Cookies)> GetAntiForgeryTokenAsync(HttpClient client, string path)
    {
        var page = await client.GetAsync(path);
        var html = await page.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var cookies = string.Join("; ", page.Headers.TryGetValues("Set-Cookie", out var values) ? values.Select(v => v.Split(';')[0]) : []);
        return (token, cookies);
    }

    private static Task<HttpResponseMessage> PostFormAsync(HttpClient client, string path, string cookies, Dictionary<string, string> fields)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(fields) };
        request.Headers.Add("Cookie", cookies);
        return client.SendAsync(request);
    }
}
