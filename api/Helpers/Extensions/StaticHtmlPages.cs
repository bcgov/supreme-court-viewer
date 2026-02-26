namespace Scv.Api.Helpers.Extensions
{
    public class StaticHtmlPages
    {

        public const string FileAccessDeniedHtml = """
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8" />
        <title>File Access Denied</title>
    </head>
    <body style="font-family: Arial; text-align:center; margin-top:100px;">
    <p style="color:#8B0000; font-size:16px; font-weight:500;"> Access to this court record is not currently available in ACM. Please contact the court registry location responsible for this file to request access or further information.</p> 
    <p><a href="https://www2.gov.bc.ca/gov/content/justice/courthouse-services/courthouse-locations">Courthouse locations - Province of British Columbia</a></p>
    </body>
    </html>
    """;
    }
}
