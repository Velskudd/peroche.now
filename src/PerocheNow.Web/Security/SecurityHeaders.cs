namespace PerocheNow.Web.Security;

public static class SecurityHeaders
{
    // Tout est servi depuis le site lui-même : polices, images, scripts et styles.
    private const string ContentPolicy =
        "default-src 'self'; img-src 'self' data:; font-src 'self'; style-src 'self'; script-src 'self'; " +
        "connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    // En développement, « dotnet watch » rafraîchit le navigateur par un WebSocket sur un autre port de localhost.
    private const string DevelopmentContentPolicy =
        "default-src 'self'; img-src 'self' data:; font-src 'self'; style-src 'self'; script-src 'self'; " +
        "connect-src 'self' ws://localhost:* wss://localhost:*; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        var policy = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
            ? DevelopmentContentPolicy
            : ContentPolicy;

        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.ContentSecurityPolicy = policy;
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await next();
        });
    }
}
