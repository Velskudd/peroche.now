# peroche.now

Le site d'Alexandre Peroche : ce qu'il fait en ce moment, et les projets qui le composent, à commencer par NUR24.

Blazor (.NET 10, rendu statique côté serveur), architecture en oignon, EF Core, hébergé sur Azure Container Apps avec une base Azure SQL gratuite.

## Architecture

```
src/
  PerocheNow.Domain          Le cœur : entités et règles métier, sans aucune dépendance.
  PerocheNow.Business        Cas d'usage, médiateur maison, ports (interfaces), audit. Dépend du Domain.
  PerocheNow.Infrastructure  Adaptateurs : EF Core (SQL Server / SQLite), horloge, utilisateur courant.
  PerocheNow.Web             Blazor : pages, composants, démarrage. Assemble les couches.
tests/
  PerocheNow.Domain.Tests          Règles métier.
  PerocheNow.Business.Tests        Médiateur et cas d'usage, avec des doublures des ports.
  PerocheNow.Infrastructure.Tests  EF Core de bout en bout sur SQLite en mémoire, schéma SQL Server.
```

Les dépendances vont toujours vers le centre : l'Infrastructure implémente les ports déclarés par Business, jamais l'inverse.

### Le médiateur

Une requête est soit une **consultation** (`IQuery<T>`, lecture), soit une **commande** (`ICommand<T>`, écriture). Le médiateur (`IMediator`) trouve son gestionnaire (`IHandler<TRequest, TResponse>`) et l'enveloppe dans les **comportements** enregistrés (`IPipelineBehavior<TRequest, TResponse>`), du plus extérieur au plus intérieur :

1. `UnitOfWorkBehavior` enregistre tout en une fois après une commande réussie.
2. `AuditBehavior` ajoute une trace (`AuditEntry`) pour chaque commande réussie.

Les gestionnaires n'appellent donc jamais `SaveChanges` : si une règle métier échoue, rien n'est écrit, ni la modification ni la trace.

Ajouter un cas d'usage : un `record` qui implémente `ICommand<T>` ou `IQuery<T>` et une classe `IHandler<,>` dans `PerocheNow.Business`. L'enregistrement est automatique.

### Conventions

Le code est en anglais (noms de types, membres, fichiers, dossiers, classes CSS, schéma de la base) ; les commentaires sont en français. Les textes affichés aux visiteurs et les adresses publiques du site (`/nur24`, `/contenus`, `/partenaires`, `/sante`) restent en français.

## Travailler en local

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/PerocheNow.Web
```

En développement, le site utilise une base SQLite (`perochenow-dev.db`), créée et remplie au premier lancement. Aucun serveur SQL n'est nécessaire.

```bash
dotnet test PerocheNow.slnx
```

### Configuration

| Clé | Rôle |
| --- | --- |
| `ConnectionStrings:PerocheNow` | Chaîne de connexion. En production, fournie par la Container App (secret). |
| `Database:Provider` | `SqlServer` (défaut) ou `Sqlite`. |
| `Links:Twitch`, `Links:YouTube`, `Links:Instagram`, `Links:Facebook`, `Links:TikTok` | Réseaux affichés. Un lien vide n'apparaît pas. |
| `Links:PartnerKit` | Fait apparaître le bouton « Télécharger le dossier partenaire ». |
| `Links:PartnershipContact` | L'e-mail de Trinity Management pour les partenariats (page Partenaires et pied de page). |

Pour ajouter un réseau, il suffit de renseigner son adresse dans `src/PerocheNow.Web/appsettings.json`.

## Mise en ligne

Tout passe par GitHub Actions :

- **CI/CD** (à chaque push sur `main`) : compile, teste, publie l'image sur GitHub Container Registry, puis met à jour la Container App si l'infrastructure existe.
- **Infrastructure** (lancement manuel) : crée ou met à jour les ressources Azure décrites dans `infra/main.bicep`.

### 1. GitHub

1. Pousser le dépôt sur `main`. Le premier passage de **CI/CD** publie l'image `ghcr.io/velskudd/peroche.now`.
2. Vérifier que le paquet est public : GitHub → profil → *Packages* → `peroche.now` → *Package settings* → *Change visibility* → *Public*. Le dépôt étant public, l'image peut l'être aussi : la Container App la tire alors sans jeton.
   Pour garder l'image privée, créer un jeton classique avec la seule portée `read:packages` et l'enregistrer dans le secret `GHCR_JETON_LECTURE`.
3. Créer l'environnement `production` : *Settings* → *Environments* → *New environment*.

### 2. Azure

Dans [Azure Cloud Shell](https://shell.azure.com) (Bash) :

```bash
GROUPE=rg-peroche-now
# Sujet OIDC au format « immuable » de GitHub (depuis avril 2026) : propriétaire@id/dépôt@id.
# Les deux identifiants s'obtiennent avec : gh api repos/Velskudd/peroche.now --jq '.owner.id, .id'
SUJET_OIDC="repo:Velskudd@20286630/peroche.now@1423298996:environment:production"
ABONNEMENT=$(az account show --query id -o tsv)

az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.Sql
az group create --name $GROUPE --location francecentral

# Identité utilisée par GitHub Actions, sans mot de passe (OpenID Connect).
APP_ID=$(az ad app create --display-name peroche-now-github --query appId -o tsv)
az ad sp create --id $APP_ID
az role assignment create --assignee $APP_ID --role Contributor \
  --scope /subscriptions/$ABONNEMENT/resourceGroups/$GROUPE
az ad app federated-credential create --id $APP_ID --parameters "{
  \"name\": \"github-production\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"$SUJET_OIDC\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}"

echo "AZURE_CLIENT_ID=$APP_ID"
echo "AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)"
echo "AZURE_SUBSCRIPTION_ID=$ABONNEMENT"
echo "SQL_MOT_DE_PASSE=Pn15-$(openssl rand -hex 10)-Aa"
```

Si GitHub Actions échoue avec `AADSTS700213: No matching federated identity record`, le sujet ne correspond pas : l'erreur affiche le sujet exact envoyé par GitHub, à reporter avec `az ad app federated-credential update` ([procédure Microsoft](https://learn.microsoft.com/entra/workload-id/workload-identities-github-immutable-subjects)).

Dans GitHub, *Settings* → *Secrets and variables* → *Actions* :

| Type | Nom | Valeur |
| --- | --- | --- |
| Secret | `AZURE_CLIENT_ID` | affiché ci-dessus |
| Secret | `AZURE_TENANT_ID` | affiché ci-dessus |
| Secret | `AZURE_SUBSCRIPTION_ID` | affiché ci-dessus |
| Secret | `SQL_MOT_DE_PASSE` | la valeur générée ci-dessus (sans `!` : Bash l'interprète) |
| Variable | `AZURE_RESOURCE_GROUP` | `rg-peroche-now` |
| Variable | `AZURE_CONTAINERAPP` | `peroche-now` |

Puis lancer le workflow **Infrastructure** (*Actions* → *Infrastructure* → *Run workflow*). Il affiche à la fin l'adresse provisoire du site (`…azurecontainerapps.io`), l'IP de l'environnement et l'identifiant de vérification du domaine. Les pushs suivants sur `main` déploient automatiquement.

### 3. Le domaine peroche.now

Un domaine nu (apex) se valide par HTTP et se pointe avec un enregistrement A ([documentation](https://learn.microsoft.com/azure/container-apps/custom-domains-managed-certificates)).

1. Chez le registrar, créer :
   - `A` sur `@` → l'IP statique affichée par le workflow ;
   - `TXT` sur `asuid` → l'identifiant de vérification affiché par le workflow.
   - Si le domaine a des enregistrements `CAA`, ajouter `0 issue "digicert.com"`.
2. Une fois le DNS propagé, dans Cloud Shell :

```bash
az containerapp hostname add --hostname peroche.now \
  --name peroche-now --resource-group rg-peroche-now
az containerapp hostname bind --hostname peroche.now \
  --name peroche-now --resource-group rg-peroche-now \
  --environment peroche-now-env --validation-method HTTP
az containerapp env certificate list --name peroche-now-env --resource-group rg-peroche-now \
  --managed-certificates-only --query "[0].id" -o tsv
```

3. Enregistrer les variables GitHub `DOMAINE` = `peroche.now` et `ID_CERTIFICAT` = l'identifiant affiché, pour que le prochain passage du workflow **Infrastructure** conserve la liaison.

Le certificat géré est gratuit et renouvelé automatiquement, tant que le site répond sur le domaine.

## Qualité du code (Sonar)

Les règles Sonar (`SonarAnalyzer.CSharp`, les mêmes que SonarQube et SonarLint) s'appliquent à chaque compilation, et tout avertissement fait échouer le build (sauf les alertes de vulnérabilité NuGet, qui restent des avertissements). Une exception justifiée se déclare avec `[SuppressMessage(..., Justification = "...")]`.

Le job **Analyse SonarCloud** du workflow CI/CD envoie en plus l'analyse et la couverture des tests à [SonarCloud](https://sonarcloud.io). Il est ignoré tant que le secret `SONAR_TOKEN` n'existe pas. Pour l'activer :

1. Sur sonarcloud.io, se connecter avec GitHub, importer l'organisation `Velskudd` puis le dépôt `peroche.now` (gratuit pour un dépôt public).
2. Dans le projet SonarCloud, *Administration* → *Analysis Method* : **désactiver l'analyse automatique** (elle entre en conflit avec l'analyse par la CI).
3. *My Account* → *Security* : générer un jeton.
4. Dans GitHub, *Settings* → *Secrets and variables* → *Actions* :

| Type | Nom | Valeur |
| --- | --- | --- |
| Secret | `SONAR_TOKEN` | le jeton généré |
| Variable | `SONAR_ORGANISATION` | la clé de l'organisation, si elle diffère de `velskudd` |
| Variable | `SONAR_PROJET` | la clé du projet, si elle diffère de `Velskudd_peroche.now` |

## Coûts et limites gratuites

- **Container Apps** (consommation) : le site s'arrête sans visiteur. Chaque mois, les 180 000 premières vCPU-secondes, 360 000 Gio-secondes et 2 millions de requêtes sont gratuites ([tarifs](https://azure.microsoft.com/pricing/details/container-apps/)). Le premier visiteur après une pause attend le démarrage.
- **Azure SQL Database, offre gratuite** : 100 000 vCore-secondes et 32 Go par base et par mois ([documentation](https://learn.microsoft.com/azure/azure-sql/database/free-offer)). La base se met en pause après `delaiPauseMinutes` d'inactivité (60 par défaut, 15 au minimum). Si le quota du mois est épuisé, elle reste en pause jusqu'au mois suivant : aucune facture, et l'accueil affiche alors le statut de secours (`InitialStatus`).
- Surveiller la métrique *Free amount remaining* de la base dans le portail Azure.

## Migrations

Le schéma de la base est géré par les migrations EF Core (`src/PerocheNow.Infrastructure/Persistence/Migrations`), appliquées au démarrage sur SQL Server. En local, SQLite crée le schéma directement à partir du modèle.

Après une modification du modèle (entité, configuration), générer la migration :

- **Sans rien installer** : GitHub → *Actions* → **Migration EF Core** → *Run workflow*, avec un nom en anglais (ex. `AddSectionToLines`). Le workflow génère la migration et la pousse sur `main` ; relancer ensuite **CI/CD** pour déployer.
- **En local** :

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add MigrationName \
  --project src/PerocheNow.Infrastructure \
  --startup-project src/PerocheNow.Web \
  --output-dir Persistence/Migrations
```

Les toutes premières versions du site créaient le schéma sans migration. Au premier démarrage avec migrations, ces tables (qui ne contenaient que le statut initial) sont supprimées puis recréées par les migrations.

## Crédits

- Police : [Archivo](https://fonts.google.com/specimen/Archivo) en version variable (graisse et largeur), licence SIL Open Font License, servie depuis le site. Les titres sont élargis à 125 %.
- Tracé du Nürburgring : © les contributeurs d'[OpenStreetMap](https://www.openstreetmap.org/copyright), licence ODbL.
