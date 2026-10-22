using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PerocheNow.Business.Ports;

namespace PerocheNow.Web.Administration;

/// <summary>
/// L'administration du site : connexion avec GitHub (OAuth), session par cookie,
/// accès réservé aux identifiants GitHub listés dans <see cref="AdminOptions.Administrateurs"/>.
/// </summary>
public static class EnregistrementAdministration
{
    public const string PolitiqueAdministrateur = "Administrateur";
    public const string PageRefus = "/admin/refuse";

    private const string SchemaGitHub = "GitHub";
    private const string PageAdmin = "/admin";

    public static WebApplicationBuilder AjouterAdministration(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(AdminOptions.Section).Get<AdminOptions>() ?? new AdminOptions();
        builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.Section));

        // Les actions sont tracées au nom de la personne connectée.
        builder.Services.AddHttpContextAccessor();
        builder.Services.Replace(ServiceDescriptor.Singleton<IUtilisateurCourant, UtilisateurConnecte>());

        var authentification = builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(cookie =>
            {
                cookie.Cookie.Name = "peroche.admin";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookie.Cookie.SameSite = SameSiteMode.Lax;
                cookie.LoginPath = "/connexion";
                cookie.AccessDeniedPath = PageRefus;
                cookie.ExpireTimeSpan = TimeSpan.FromHours(8);
                cookie.SlidingExpiration = true;
            });

        if (options.GitHub.EstConfiguree)
            authentification.AddOAuth(SchemaGitHub, oauth => ConfigurerGitHub(oauth, options));

        builder.Services.AddAuthorizationBuilder().AddPolicy(PolitiqueAdministrateur, politique =>
        {
            politique.RequireAuthenticatedUser();
            if (options.Administrateurs.Count > 0)
                politique.RequireClaim(ClaimTypes.NameIdentifier, options.Administrateurs);
            else
                politique.RequireAssertion(_ => false); // Liste vide : personne, et surtout pas « tout le monde ».
        });

        return builder;
    }

    /// <summary>Les deux adresses de session : se connecter (vers GitHub) et se déconnecter.</summary>
    public static WebApplication MapAdministration(this WebApplication app)
    {
        var configuree = (app.Configuration.GetSection(AdminOptions.Section).Get<AdminOptions>() ?? new AdminOptions())
            .GitHub.EstConfiguree;

        app.MapGet("/connexion", ([FromQuery(Name = "ReturnUrl")] string? retour) =>
            configuree
                ? Results.Challenge(new AuthenticationProperties { RedirectUri = RetourLocal(retour) }, [SchemaGitHub])
                : Results.Redirect(PageRefus + "?motif=configuration"));

        app.MapPost("/deconnexion", async (HttpContext contexte) =>
            {
                await contexte.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/");
            })
            .WithMetadata(new RequireAntiforgeryTokenAttribute());

        return app;
    }

    /// <summary>N'accepte qu'un retour vers une page du site, jamais vers un autre domaine.</summary>
    private static string RetourLocal(string? retour) =>
        retour is { Length: > 0 } && retour[0] == '/' && !retour.StartsWith("//", StringComparison.Ordinal)
            && !retour.StartsWith("/\\", StringComparison.Ordinal)
            ? retour
            : PageAdmin;

    private static void ConfigurerGitHub(OAuthOptions oauth, AdminOptions options)
    {
        var github = options.GitHub;
        oauth.ClientId = github.IdentifiantClient!;
        oauth.ClientSecret = github.SecretClient!;
        oauth.AuthorizationEndpoint = github.AdresseAutorisation!.AbsoluteUri;
        oauth.TokenEndpoint = github.AdresseJeton!.AbsoluteUri;
        oauth.UserInformationEndpoint = github.AdresseUtilisateur!.AbsoluteUri;
        oauth.CallbackPath = "/connexion/github";
        oauth.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        // Aucune permission demandée : le profil public suffit pour savoir qui se connecte.
        oauth.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        oauth.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");

        oauth.Events.OnCreatingTicket = LireProfilAsync;
        oauth.Events.OnTicketReceived = contexte =>
        {
            // Un compte GitHub qui n'est pas dans la liste n'ouvre même pas de session.
            var identifiant = contexte.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (identifiant is null || !options.Administrateurs.Contains(identifiant, StringComparer.Ordinal))
            {
                contexte.Response.Redirect(PageRefus + "?motif=compte");
                contexte.HandleResponse();
            }

            return Task.CompletedTask;
        };
        oauth.Events.OnRemoteFailure = contexte =>
        {
            contexte.Response.Redirect(PageRefus + "?motif=echec");
            contexte.HandleResponse();
            return Task.CompletedTask;
        };
    }

    private static async Task LireProfilAsync(OAuthCreatingTicketContext contexte)
    {
        using var requete = new HttpRequestMessage(HttpMethod.Get, contexte.Options.UserInformationEndpoint);
        requete.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", contexte.AccessToken);
        requete.Headers.UserAgent.Add(new ProductInfoHeaderValue("peroche.now", "1.0")); // exigé par l'API GitHub

        using var reponse = await contexte.Backchannel.SendAsync(requete, contexte.HttpContext.RequestAborted);
        reponse.EnsureSuccessStatusCode();

        using var profil = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(contexte.HttpContext.RequestAborted));
        contexte.RunClaimActions(profil.RootElement);
    }
}
