using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FintechBackend.FunctionalTests
{
    public class ProblemDetailsTests
    {
        [Fact]
        public async Task UnknownEndpoint_ReturnsProblemDetails()
        {
            await using var factory = new WebApplicationFactory<Program>();

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            client.DefaultRequestHeaders.Accept.ParseAdd(
                "application/problem+json");

            using var response = await client.GetAsync(
                "/api/v1/unknown");

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);

            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);

            var problem =
                await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.NotNull(problem);
            Assert.Equal("Not Found", problem.Title);
            Assert.Equal(404, problem.Status);
            Assert.True(problem.Extensions.ContainsKey("traceId"));
        }
    }
}
