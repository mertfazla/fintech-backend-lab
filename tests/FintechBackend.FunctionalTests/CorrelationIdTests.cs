using Microsoft.AspNetCore.Mvc.Testing;

namespace FintechBackend.FunctionalTests
{
    public class CorrelationIdTests
    {
        [Theory]
        [InlineData("/api/v1/system/status")]
        [InlineData("/api/v1/unknown")]
        public async Task Response_ContainsCorrelationId(string path)
        {
            await using var factory = new WebApplicationFactory<Program>();

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            using var response = await client.GetAsync(path);

            Assert.True(
                response.Headers.TryGetValues(
                    "X-Correlation-ID",
                    out var values));

            var correlationId = Assert.Single(values);

            Assert.False(
                string.IsNullOrWhiteSpace(correlationId));
        }
    }
}
