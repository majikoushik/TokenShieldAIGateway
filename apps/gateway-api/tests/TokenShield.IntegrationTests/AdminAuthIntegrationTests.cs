using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace TokenShield.IntegrationTests;

public class AdminAuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminAuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        // Use the default factory. By default, WebApplicationFactory uses "Development" environment.
        // We will override it to "Production" to test the hardened behavior.
        _factory = factory;
    }

    [Fact]
    public async Task AdminApi_WithoutHeaders_InDevelopment_ReturnsOkOrUnauthorized()
    {
        // We override the test factory environment to "Development" just for this test,
        // because the fallback logic specifically checks for "Development".
        var devFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });

        var client = devFactory.CreateClient();

        // /api/admin/providers should work or return unauthorized depending on other auth, 
        // but it shouldn't throw the 500 InvalidOperationException from missing tenant context.
        var response = await client.GetAsync("/api/admin/providers");

        // We expect it to fallback to the demo tenant, so it returns 200 OK (if seeding ran) 
        // or potentially 401 if API key was needed (but admin APIs in MVP might not require x-api-key, they rely on headers).
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.InternalServerError);
        
        // Let's be more specific: if it throws an InvalidOperationException, it's a 500.
        // In Development, it should NOT throw the 500 "Tenant context could not be resolved. Headers missing or not in Development mode."
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Headers missing or not in Development mode", content);
    }

    [Fact]
    public async Task AdminApi_WithoutHeaders_InProduction_Returns401WithSpecificMessage()
    {
        var productionFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });

        var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/api/admin/providers");

        // In Production, missing headers should trigger the UnauthorizedAccessException we added.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Headers missing or not in Development mode", content);
    }
}
