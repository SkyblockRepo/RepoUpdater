using System.Net;
using FastEndpoints;
using Xunit;

namespace RepoAPI.Tests.Features;

/// <summary>
/// Example integration test using FastEndpoints.Testing
/// Read more: https://fast-endpoints.com/docs/integration-unit-testing#integration-testing
/// </summary>
public class App : AppFixture<Program>;

public class ExampleTests(App app) : TestBase<App>
{
	[Fact]
	public async Task PingTest()
	{
		var (response, result) = await app.Client.GETAsync<PingEndpoint, string>();
		response.StatusCode.ShouldBe(HttpStatusCode.OK);
		response.Content.ShouldNotBeNull();
		result.ShouldBe("pong");
	}
}