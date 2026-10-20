using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using PerocheNow.Business;
using PerocheNow.Infrastructure;
using PerocheNow.Web.Components;
using PerocheNow.Web.Configuration;
using PerocheNow.Web.Security;
using PerocheNow.Web.Startup;
using PerocheNow.Web.Status;

var french = CultureInfo.GetCultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentCulture = french;
CultureInfo.DefaultThreadCurrentUICulture = french;

var builder = WebApplication.CreateBuilder(args);

// Couches de l'oignon : Business (cas d'usage, médiateur, audit) puis Infrastructure (EF Core, horloge…).
builder.Services.AddBusiness();
builder.Services.AddInfrastructure(builder.Configuration);

// Présentation.
builder.Services.AddRazorComponents();
// Les accents restent lisibles dans le HTML (« é » plutôt que « &#xE9; »).
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CurrentStatusReader>();
builder.Services.Configure<LinksOptions>(builder.Configuration.GetSection(LinksOptions.Section));
builder.Services.AddHostedService<DatabaseStartup>();

// La sonde de santé ne touche pas la base : elle ne doit pas réveiller la base serverless.
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseSecurityHeaders();
app.UseAntiforgery();

app.MapStaticAssets();
// Adresse inchangée : les sondes de la Container App l'appellent (infra/main.bicep).
app.MapHealthChecks("/sante");
app.MapRazorComponents<App>();

await app.RunAsync();
