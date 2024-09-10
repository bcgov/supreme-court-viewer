namespace Scv.Api.Helpers
{
    public static class XForwardedForHelper
    {
        public static string BuildUrlString(string forwardedHost, string forwardedPort, string baseUrl)
        {
            //var portComponent = string.IsNullOrEmpty(forwardedPort) || forwardedPort == "80" || forwardedPort == "443" ? "" : $":{forwardedPort}";
            var portComponent = string.IsNullOrEmpty(forwardedPort) || forwardedPort == "80" || forwardedPort == "443" || forwardedPort == "8080" ? "" : $":{forwardedPort}";
            return $"https://{forwardedHost}{portComponent}{baseUrl}";
        }
    }
}
