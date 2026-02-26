using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Scv.Api.Helpers.Extensions
{
    public static class ControllerExtensions
    {
        public static ContentResult FileAccessDenied(this ControllerBase controller)
        {
            return new ContentResult
            {
                Content = StaticHtmlPages.FileAccessDeniedHtml,
                ContentType = "text/html",
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
