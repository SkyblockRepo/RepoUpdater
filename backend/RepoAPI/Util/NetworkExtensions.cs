using System.Net;
using System.Net.Sockets;

namespace RepoAPI.Util;

public static class NetworkExtensions
{
    public static bool IsFromPrivateNetwork(this HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        return ip is not null && ip.IsPrivate();
    }
    
    /// <summary>
    /// Determines if an IP address is within one of the RFC 1918 private ranges.
    /// </summary>
    public static bool IsPrivate(this IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }
        
        // if (ConfigGlobalRateLimitSettings.Settings.WhitelistedIp != string.Empty)
        // {
        //     if (ip.ToString() == ConfigGlobalRateLimitSettings.Settings.WhitelistedIp)
        //     {
        //         return true;
        //     }
        // }

        if (ip.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var ipBytes = ip.GetAddressBytes();

        return ipBytes[0] switch
        {
            // Range: 10.0.0.0/8
            10 => true,
            // Range: 172.16.0.0/12
            172 => ipBytes[1] >= 16 && ipBytes[1] <= 31,
            // Range: 192.168.0.0/16
            192 => ipBytes[1] == 168,
            _ => false
        };
    }
    
    /// <summary>
    /// Known bot detection middleware that leverages Cloudflare's known bot detection.
    /// This needs to be configured in Cloudflare dashboard - Request Header Transform Rules
    /// - All Incoming Requests
    ///   - Set Dynamic "X-Known-Bot" = "to_string(cf.client.bot)"
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static WebApplication UseKnownBotDetection(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("X-Known-Bot", out var bot))
            {
                if (bot.Count > 0 && bot[0]!.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    context.Items["known_bot"] = true;
                }
            }

            await next.Invoke();
        });

        return app;
    }

    public static bool IsKnownBot(this HttpContext? context)
    {
        if (context is null) return false;
        return context.Items.TryGetValue("known_bot", out var isKnownBot) && isKnownBot is true;
    }
}