namespace RepoAPI.Util;

[RegisterService<LoggingDelegatingHandler>(LifeTime.Transient)]
public class LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		logger.LogDebug("Sending request to {Url}", request.RequestUri);
		
		var response = await base.SendAsync(request, cancellationToken);

		logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);

		if (response.IsSuccessStatusCode) return response;
		
		var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
		logger.LogError("Request failed with status {StatusCode}. Response Body: {ResponseBody}", 
			response.StatusCode, 
			responseBody);

		return response;
	}
}