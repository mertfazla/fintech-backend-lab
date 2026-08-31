using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

namespace FintechBackend.FunctionalTests
{
    public class SystemStatusTests
    {
        [Fact]
        public async Task GetStatus_ReturnsApplicationStatus()
        {
            // Start the application inside a test host.
            await using var factory = new WebApplicationFactory<Program>();

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            // Send an http request through the application.
            using var response = await client.GetAsync("/api/v1/system/status");

            // verify the response.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "application/json",
                response.Content.Headers.ContentType?.MediaType);

            using var body = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

            Assert.Equal(
                "Fintech Backend Lab",
                body.RootElement.GetProperty("application").GetString());

            Assert.Equal(
                "Running",
                body.RootElement.GetProperty("status").GetString());
        }
    }
}
