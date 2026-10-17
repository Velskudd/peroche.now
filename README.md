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

Une requête est soit une **consultation** (`IConsultation<T>`, lecture), soit une **commande** (`ICommande<T>`, écriture). Le médiateur trouve son gestionnaire (`IGestionnaire<TRequete, TReponse>`) et l'enveloppe dans les **comportements** enregistrés, du plus extérieur au plus intérieur :

1. `ComportementUniteDeTravail` enregistre tout en une fois après une commande réussie.
2. `ComportementAudit` ajoute une trace (`TraceAudit`) pour chaque commande réussie.

Les gestionnaires n'appellent donc jamais `SaveChanges` : si une règle métier échoue, rien n'est écrit, ni la modification ni la trace.

Ajouter un cas d'usage : un `record` qui implémente `ICommande<T>` ou `IConsultation<T>` et une classe `IGestionnaire<,>` dans `PerocheNow.Business`. L'enregistrement est automatique.

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
| `BaseDeDonnees:Fournisseur` | `SqlServer` (défaut) ou `Sqlite`. |
| `Liens:Twitch`, `Liens:YouTube`, `Liens:Instagram`, `Liens:Facebook`, `Liens:TikTok` | Réseaux affichés. Un lien vide n'apparaît pas. |
| `Liens:DossierPartenaire` | Fait apparaître le bouton « Découvrir le dossier partenaire ». |

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
DEPOT=Velskudd/peroche.now
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
  \"subject\": \"repo:$DEPOT:environment:production\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}"

echo "AZURE_CLIENT_ID=$APP_ID"
echo "AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)"
echo "AZURE_SUBSCRIPTION_ID=$ABONNEMENT"
```

Dans GitHub, *Settings* → *Secrets and variables* → *Actions* :

| Type | Nom | Valeur |
| --- | --- | --- |
| Secret | `AZURE_CLIENT_ID` | affiché ci-dessus |
| Secret | `AZURE_TENANT_ID` | affiché ci-dessus |
| Secret | `AZURE_SUBSCRIPTION_ID` | affiché ci-dessus |
| Secret | `SQL_MOT_DE_PASSE` | un mot de passe fort (au moins 12 caractères, majuscules, minuscules, chiffres et symboles) |
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

## Coûts et limites gratuites

- **Container Apps** (consommation) : le site s'arrête sans visiteur. Chaque mois, les 180 000 premières vCPU-secondes, 360 000 Gio-secondes et 2 millions de requêtes sont gratuites ([tarifs](https://azure.microsoft.com/pricing/details/container-apps/)). Le premier visiteur après une pause attend le démarrage.
- **Azure SQL Database, offre gratuite** : 100 000 vCore-secondes et 32 Go par base et par mois ([documentation](https://learn.microsoft.com/azure/azure-sql/database/free-offer)). La base se met en pause après `delaiPauseMinutes` d'inactivité (60 par défaut, 15 au minimum). Si le quota du mois est épuisé, elle reste en pause jusqu'au mois suivant : aucune facture, et l'accueil affiche alors le statut de secours (`StatutInitial`).
- Surveiller la métrique *Free amount remaining* de la base dans le portail Azure.

## Migrations

Tant qu'aucune migration n'existe, le schéma est créé directement à partir du modèle. Pour passer aux migrations (avant d'avoir des données à garder) :

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial \
  --project src/PerocheNow.Infrastructure \
  --startup-project src/PerocheNow.Infrastructure \
  --output-dir Persistance/Migrations
```

Les migrations sont appliquées au démarrage. Une base déjà créée sans migration doit être vidée une fois (supprimer ses tables) pour que la migration initiale s'applique.

## Crédits

- Polices : [Chakra Petch](https://fonts.google.com/specimen/Chakra+Petch) et [Manrope](https://fonts.google.com/specimen/Manrope), licence SIL Open Font License, servies depuis le site.
- Tracé du Nürburgring : © les contributeurs d'[OpenStreetMap](https://www.openstreetmap.org/copyright), licence ODbL.
