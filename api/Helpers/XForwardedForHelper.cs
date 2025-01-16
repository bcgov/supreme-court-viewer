using System;

namespace Scv.Api.Helpers
{
    public static class XForwardedForHelper
    {
        public static string BuildUrlString(string forwardedHost, string forwardedPort, string baseUrl, string remainingPath = "", string query = "")
        {
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

            return uriBuilder.Uri.AbsoluteUri;
        }
    }
}
