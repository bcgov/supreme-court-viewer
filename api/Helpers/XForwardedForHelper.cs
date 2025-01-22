using System;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Scv.Api.Helpers
{
    public static class XForwardedForHelper
    {
        private static readonly ILogger _logger;

        static XForwardedForHelper()
        {
            using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = factory.CreateLogger("XForwardedForHelper");
        }

        public static string BuildUrlString(string forwardedHost, string forwardedPort, string baseUrl, string remainingPath = "", string query = "")
        {
            // _logger.LogInformation($"XForwardedForHelper - forwardedHost: `{forwardedHost}`, forwardedPort: `{forwardedPort}`, baseUrl: `{baseUrl}`, remainingPath: `{remainingPath}`, query: `{query}`");

            // Default: Assume the code is running as Court Viewer locally, unless specified.
            forwardedHost = forwardedHost.IsNullOrEmpty() ? "localhost" : forwardedHost;
            forwardedPort = forwardedPort.IsNullOrEmpty() ? "8080" : forwardedPort;
            baseUrl = baseUrl.IsNullOrEmpty() ? "/court-viewer/" : baseUrl;

            var sanitizedPath = baseUrl;
            var isLocalhost = forwardedHost.Contains("localhost");
            if (!string.IsNullOrEmpty(remainingPath))
            {
                sanitizedPath = string.Format("{0}/{1}", baseUrl.TrimEnd('/'), remainingPath.TrimStart('/'));
            }

            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = forwardedHost,
                Path = sanitizedPath,
                Query = query
            };

            // Prevent removing the 8080 on localhost
            var portComponent =
                string.IsNullOrEmpty(forwardedPort) || forwardedPort == "80" || forwardedPort == "443" || (forwardedPort == "8080" && !isLocalhost)
                    ? ""
                    : $":{forwardedPort}";

            if (!string.IsNullOrEmpty(portComponent))
            {
                int port;
                int.TryParse(forwardedPort, out port);
                uriBuilder.Port = port;
            }

            _logger.LogInformation($"uriBuilder.Uri.AbsoluteUri `{uriBuilder.Uri.AbsoluteUri}`");
            return uriBuilder.Uri.AbsoluteUri;
        }
    }
}
