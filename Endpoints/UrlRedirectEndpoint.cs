using System.Security.Cryptography;
using System.Text;
using Shortly.Application.Interfaces;

namespace Shortly.Endpoints;

public static class UrlRedirectEndpoint
{
    public static void MapUrlRedirect(this WebApplication app)
    {
        app.MapGet("/{shortUrl}", async (string shortUrl, HttpContext context, ILinkService linkService) =>
        {
            Application.DTOs.LinkResponse link;
            try
            {
                link = await linkService.GetLink(shortUrl);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }

            var etag = ComputeETag(link.ShortUrl, link.Url);
            var lastModified = DateTime.SpecifyKind(link.CreatedAt, DateTimeKind.Utc);
            lastModified = lastModified.AddTicks(-(lastModified.Ticks % TimeSpan.TicksPerSecond));

            context.Response.Headers.CacheControl = "private, must-revalidate";
            context.Response.Headers.ETag = etag;
            context.Response.Headers.LastModified = lastModified.ToString("R");

            var ifNoneMatch = context.Request.Headers.IfNoneMatch;
            if (ifNoneMatch.Count > 0)
            {
                if (ifNoneMatch.Any(v => v == etag || v == "*"))
                    return Results.StatusCode(StatusCodes.Status304NotModified);
            }
            else if (context.Request.Headers.TryGetValue("If-Modified-Since", out var raw)
                     && DateTime.TryParse(raw, out var ifModifiedSince)
                     && lastModified <= ifModifiedSince.ToUniversalTime())
            {
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            await linkService.IncrementClicks(link.Id);
            return Results.Redirect(link.Url);
        });
    }

    private static string ComputeETag(string shortUrl, string url)
    {
        var input = $"{shortUrl}:{url}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return $"\"{Convert.ToHexString(hash)[..16]}\"";
    }
}