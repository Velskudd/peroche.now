using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using PerocheNow.Business;
using PerocheNow.Infrastructure;
using PerocheNow.Web.Components;
using PerocheNow.Web.Configuration;
using PerocheNow.Web.Demarrage;
using PerocheNow.Web.Maintenant;
using PerocheNow.Web.Securite;

var francais = CultureInfo.GetCultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentCulture = francais;
CultureInfo.DefaultThreadCurrentUICulture = francais;

var builder = WebApplication.CreateBuilder(args);

// Couches de l'oignon : Business (cas d'usage, médiateur, audit) puis Infrastructure (EF Core, horloge…).
builder.Services.AjouterBusiness();
builder.Services.AjouterInfrastructure(builder.Configuration);

// Présentation.
builder.Services.AddRazorComponents();
// Les accents restent lisibles dans le HTML (« é » plutôt que « &#xE9; »).
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<LecteurDuMoment>();
builder.Services.Configure<LiensOptions>(builder.Configuration.GetSection(LiensOptions.Section));
builder.Services.AddHostedService<InitialisationAuDemarrage>();

// La sonde de santé ne touche pas la base : elle ne doit pas réveiller la base serverless.
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseEnTetesDeSecurite();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/sante");
app.MapRazorComponents<App>();

await app.RunAsync();
