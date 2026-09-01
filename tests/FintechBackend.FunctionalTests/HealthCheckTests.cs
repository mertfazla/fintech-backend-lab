using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace FintechBackend.FunctionalTests
{
    public class HealthCheckTests
    {
        [Theory]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public async Task HealthEndpoint_ReturnsHealthy(string path)
        {
            await using var factory = new WebApplicationFactory<Program>();

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            using var response = await client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(
                "text/plain",
                response.Content.Headers.ContentType?.MediaType);

            Assert.Equal("Healthy", body);
        }
    }
}
