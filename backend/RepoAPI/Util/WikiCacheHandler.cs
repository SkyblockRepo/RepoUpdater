using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace RepoAPI.Util;

public class WikiCacheHandler(IWebHostEnvironment env) : DelegatingHandler
{
    private readonly string _cacheDirectory = Path.Combine(Path.GetTempPath(), "SkyblockRepo_WikiCache");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Only cache GET requests and only in Development
        if (!env.IsDevelopment() || request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }

        var key = GetCacheKey(request.RequestUri?.ToString() ?? "");
        var cacheFilePath = Path.Combine(_cacheDirectory, key);

        if (File.Exists(cacheFilePath))
        {
            // Check if cache is older than 24 hours
            var fileInfo = new FileInfo(cacheFilePath);
            if (fileInfo.CreationTimeUtc > DateTime.UtcNow.AddHours(-24))
            {
                var cachedContent = await File.ReadAllTextAsync(cacheFilePath, cancellationToken);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(cachedContent, Encoding.UTF8, "application/json"),
                    RequestMessage = request
                };
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            await File.WriteAllTextAsync(cacheFilePath, content, cancellationToken);
        }

        return response;
    }

    private static string GetCacheKey(string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(hash);
    }
}
