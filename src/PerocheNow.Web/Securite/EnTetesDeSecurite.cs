namespace PerocheNow.Web.Securite;

public static class EnTetesDeSecurite
{
    // Tout est servi depuis le site lui-même : polices, images, scripts et styles.
    private const string PolitiqueDeContenu =
        "default-src 'self'; img-src 'self' data:; font-src 'self'; style-src 'self'; script-src 'self'; " +
        "connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    public static IApplicationBuilder UseEnTetesDeSecurite(this IApplicationBuilder app) =>
        app.Use(async (contexte, suite) =>
        {
            var entetes = contexte.Response.Headers;
            entetes.ContentSecurityPolicy = PolitiqueDeContenu;
            entetes.XContentTypeOptions = "nosniff";
            entetes["Referrer-Policy"] = "strict-origin-when-cross-origin";
            entetes["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await suite();
        });
}
