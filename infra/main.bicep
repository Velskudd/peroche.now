// Infrastructure de peroche.now : Azure Container Apps (consommation, mise à zéro) + Azure SQL Database (offre gratuite)
// + un compte de stockage pour les copies de lecture (ce que les pages publiques lisent à la place de la base).
// Déployée par le workflow « Infrastructure » (.github/workflows/infrastructure.yml).

@description('Région Azure. France Central par défaut.')
param emplacement string = resourceGroup().location

@description('Nom de la Container App (sert aussi de préfixe aux autres ressources).')
param nomApp string = 'peroche-now'

@description('Image du site, par exemple ghcr.io/velskudd/peroche-now:latest.')
param image string

@description('Identifiant administrateur du serveur SQL.')
param adminSql string = 'perochenow'

@secure()
@description('Mot de passe administrateur du serveur SQL.')
param motDePasseSql string

// Avec l'offre gratuite en mode « pause si quota épuisé », Azure n'accepte que la valeur par défaut (60) :
// « Only default value for auto pause delay is allowed for Free Limit database with auto pause exhaustion behavior ».
@description('Délai avant mise en pause de la base, en minutes. Imposé à 60 par l\'offre gratuite.')
@minValue(15)
param delaiPauseMinutes int = 60

@description('Utilisateur GitHub pour tirer une image privée de GHCR. Vide si le paquet est public.')
param utilisateurGhcr string = ''

@secure()
@description('Jeton GitHub (read:packages) pour une image privée. Vide si le paquet est public.')
param jetonGhcr string = ''

@description('Domaine personnalisé déjà lié (ex. peroche.now). Vide tant que le domaine n\'est pas configuré.')
param domainePersonnalise string = ''

@description('Identifiant du certificat géré lié au domaine. Vide tant que le domaine n\'est pas configuré.')
param idCertificat string = ''

@description('Identifiant client de l\'application Twitch (pastille « En direct »). Vide : pastille désactivée.')
param twitchIdentifiantClient string = ''

@secure()
@description('Secret client de l\'application Twitch. Vide : pastille désactivée.')
param twitchSecretClient string = ''

@description('Identifiant client de l\'application OAuth GitHub (connexion à l\'administration). Vide : administration fermée.')
param adminGitHubIdentifiantClient string = ''

@secure()
@description('Secret client de l\'application OAuth GitHub. Vide : administration fermée.')
param adminGitHubSecretClient string = ''

var suffixe = uniqueString(resourceGroup().id)
var nomBase = 'perochenow'
var registrePrive = !empty(jetonGhcr)
var avecDomaine = !empty(domainePersonnalise) && !empty(idCertificat)
var avecTwitch = !empty(twitchIdentifiantClient) && !empty(twitchSecretClient)
var avecAdmin = !empty(adminGitHubIdentifiantClient) && !empty(adminGitHubSecretClient)

// ---------- Base de données ----------

resource serveurSql 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${nomApp}-sql-${suffixe}'
  location: emplacement
  properties: {
    administratorLogin: adminSql
    administratorLoginPassword: motDePasseSql
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Les adresses sortantes d'une Container App de consommation ne sont pas fixes : on autorise les services Azure.
resource accesServicesAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: serveurSql
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource base 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: serveurSql
  name: nomBase
  location: emplacement
  sku: {
    name: 'GP_S_Gen5_2'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    // Offre gratuite : 100 000 vCore-secondes et 32 Go par mois ; la base se met en pause si le quota est épuisé.
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    autoPauseDelay: delaiPauseMinutes
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
}

var chaineDeConnexion = 'Server=tcp:${serveurSql.properties.fullyQualifiedDomainName},1433;Initial Catalog=${nomBase};User ID=${adminSql};Password=${motDePasseSql};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'

// ---------- Copies de lecture ----------
// Petits documents JSON tirés de la base, relus par les pages publiques : la base serverless reste en pause
// (et son quota gratuit intact) tant que personne ne passe par l'administration. Coût : moins d'un centime par mois.

resource stockage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take('perochenow${suffixe}', 24)
  location: emplacement
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    // Le site signe ses requêtes avec la clé du compte (la CI n'a pas le droit d'attribuer des rôles).
    allowSharedKeyAccess: true
  }
}

resource servicesBlob 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: stockage
  name: 'default'
  properties: {
    // Chaque copie remplacée est gardée : l'historique de « En ce moment », restaurable depuis le portail.
    isVersioningEnabled: true
  }
}

resource conteneurCopies 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: servicesBlob
  name: 'copies'
  properties: {
    publicAccess: 'None'
  }
}

// ---------- Hébergement ----------

resource environnement 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${nomApp}-env'
  location: emplacement
  properties: {
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: nomApp
  location: emplacement
  properties: {
    environmentId: environnement.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        customDomains: avecDomaine
          ? [
              {
                name: domainePersonnalise
                certificateId: idCertificat
                bindingType: 'SniEnabled'
              }
            ]
          : []
      }
      secrets: concat(
        [
          {
            name: 'chaine-sql'
            value: chaineDeConnexion
          }
          {
            name: 'cle-stockage'
            value: stockage.listKeys().keys[0].value
          }
        ],
        registrePrive
          ? [
              {
                name: 'jeton-ghcr'
                value: jetonGhcr
              }
            ]
          : [],
        avecTwitch
          ? [
              {
                name: 'twitch-secret'
                value: twitchSecretClient
              }
            ]
          : [],
        avecAdmin
          ? [
              {
                name: 'github-secret'
                value: adminGitHubSecretClient
              }
            ]
          : []
      )
      registries: registrePrive
        ? [
            {
              server: 'ghcr.io'
              username: utilisateurGhcr
              passwordSecretRef: 'jeton-ghcr'
            }
          ]
        : []
    }
    template: {
      containers: [
        {
          name: 'web'
          image: image
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: concat(
            [
              {
                name: 'ConnectionStrings__PerocheNow'
                secretRef: 'chaine-sql'
              }
              {
                name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
                value: 'true'
              }
              {
                name: 'Copies__AdresseConteneur'
                value: '${stockage.properties.primaryEndpoints.blob}${conteneurCopies.name}'
              }
              {
                name: 'Copies__Compte'
                value: stockage.name
              }
              {
                name: 'Copies__Cle'
                secretRef: 'cle-stockage'
              }
            ],
            avecTwitch
              ? [
                  {
                    name: 'Twitch__IdentifiantClient'
                    value: twitchIdentifiantClient
                  }
                  {
                    name: 'Twitch__SecretClient'
                    secretRef: 'twitch-secret'
                  }
                ]
              : [],
            avecAdmin
              ? [
                  {
                    name: 'Admin__GitHub__IdentifiantClient'
                    value: adminGitHubIdentifiantClient
                  }
                  {
                    name: 'Admin__GitHub__SecretClient'
                    secretRef: 'github-secret'
                  }
                ]
              : []
          )
          probes: [
            {
              // /sante ne touche pas la base : les sondes ne la réveillent pas.
              type: 'Startup'
              httpGet: {
                path: '/sante'
                port: 8080
              }
              periodSeconds: 2
              failureThreshold: 30
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/sante'
                port: 8080
              }
              periodSeconds: 30
            }
          ]
        }
      ]
      scale: {
        // Zéro réplique sans visiteur : le premier visiteur réveille le site (démarrage à froid accepté).
        minReplicas: 0
        maxReplicas: 1
        rules: [
          {
            name: 'http'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

output adresseParDefaut string = 'https://${app.properties.configuration.ingress.fqdn}'
output ipStatique string = environnement.properties.staticIp
output idVerificationDomaine string = app.properties.customDomainVerificationId
output nomEnvironnement string = environnement.name
output compteDeStockage string = stockage.name
