// Infrastructure de peroche.now : Azure Container Apps (consommation, mise à zéro) + Azure SQL Database (offre gratuite).
// Déployée par le workflow « Infrastructure » (.github/workflows/infrastructure.yml).

@description('Région Azure. France Central par défaut.')
param location string = resourceGroup().location

@description('Nom de la Container App (sert aussi de préfixe aux autres ressources).')
param appName string = 'peroche-now'

@description('Image du site, par exemple ghcr.io/velskudd/peroche-now:latest.')
param image string

@description('Identifiant administrateur du serveur SQL.')
param sqlAdminLogin string = 'perochenow'

@secure()
@description('Mot de passe administrateur du serveur SQL.')
param sqlAdminPassword string

@description('Délai avant mise en pause de la base, en minutes. 60 par défaut ; 15 consomme moins de quota gratuit.')
@minValue(15)
param autoPauseDelayMinutes int = 60

@description('Utilisateur GitHub pour tirer une image privée de GHCR. Vide si le paquet est public.')
param ghcrUser string = ''

@secure()
@description('Jeton GitHub (read:packages) pour une image privée. Vide si le paquet est public.')
param ghcrToken string = ''

@description('Domaine personnalisé déjà lié (ex. peroche.now). Vide tant que le domaine n\'est pas configuré.')
param customDomain string = ''

@description('Identifiant du certificat géré lié au domaine. Vide tant que le domaine n\'est pas configuré.')
param certificateId string = ''

var suffix = uniqueString(resourceGroup().id)
var databaseName = 'perochenow'
var usePrivateRegistry = !empty(ghcrToken)
var hasCustomDomain = !empty(customDomain) && !empty(certificateId)

// ---------- Base de données ----------

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${appName}-sql-${suffix}'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Les adresses sortantes d'une Container App de consommation ne sont pas fixes : on autorise les services Azure.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
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
    autoPauseDelay: autoPauseDelayMinutes
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
}

var connectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'

// ---------- Hébergement ----------

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${appName}-env'
  location: location
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
  name: appName
  location: location
  properties: {
    environmentId: managedEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        customDomains: hasCustomDomain
          ? [
              {
                name: customDomain
                certificateId: certificateId
                bindingType: 'SniEnabled'
              }
            ]
          : []
      }
      secrets: concat(
        [
          {
            name: 'sql-connection-string'
            value: connectionString
          }
        ],
        usePrivateRegistry
          ? [
              {
                name: 'ghcr-token'
                value: ghcrToken
              }
            ]
          : []
      )
      registries: usePrivateRegistry
        ? [
            {
              server: 'ghcr.io'
              username: ghcrUser
              passwordSecretRef: 'ghcr-token'
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
          env: [
            {
              name: 'ConnectionStrings__PerocheNow'
              secretRef: 'sql-connection-string'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
          ]
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

output defaultUrl string = 'https://${app.properties.configuration.ingress.fqdn}'
output staticIp string = managedEnvironment.properties.staticIp
output domainVerificationId string = app.properties.customDomainVerificationId
output environmentName string = managedEnvironment.name
