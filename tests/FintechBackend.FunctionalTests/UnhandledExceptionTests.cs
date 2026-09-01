using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace FintechBackend.FunctionalTests
{
    public class UnhandledExceptionTests
    {
        [Fact]
        public async Task UnhandledException_ReturnsSafeProblemDetails()
        {
            await using var factory =
                new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        builder.UseEnvironment("Production");

                        builder.ConfigureTestServices(services =>
                        {
                            services
                                .AddControllers()
                                .AddApplicationPart(
                                    typeof(ThrowingTestController)
                                        .Assembly);
                        });
                    });

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            client.DefaultRequestHeaders.Accept.ParseAdd(
                "application/problem+json");

            using var response = await client.GetAsync(
                "/_tests/unhandled-exception");

            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);

            var problem =
                await response.Content.ReadFromJsonAsync<ProblemDetails>();

            Assert.NotNull(problem);
            Assert.Equal(500, problem.Status);

            Assert.Equal(
                "An error occurred while processing your request.",
                problem.Title);

            Assert.Null(problem.Detail);
            Assert.True(problem.Extensions.ContainsKey("traceId"));

            Assert.True(
                response.Headers.Contains("X-Correlation-ID"));
        }
    }

    [ApiController]
    [Route("_tests/unhandled-exception")]
    public sealed class ThrowingTestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            throw new InvalidOperationException(
                "Sensitive test exception.");
        }
    }
}
