// Infrastructure de peroche.now : Azure Container Apps (consommation, mise à zéro) + Azure SQL Database (offre gratuite).
// Déployée par le workflow « Infrastructure » (.github/workflows/infrastructure.yml).

@description('Région Azure. France Central par défaut.')
param emplacement string = resourceGroup().location

@description('Nom de la Container App (sert aussi de préfixe aux autres ressources).')
param nomApp string = 'peroche-now'

@description('Image du site, par exemple ghcr.io/velskudd/peroche-now:latest.')
param image string

@description('Délai avant mise en pause de la base, en minutes. 60 par défaut ; 15 consomme moins de quota gratuit.')
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

var suffixe = uniqueString(resourceGroup().id)
var nomServeurSql = '${nomApp}-sql-${suffixe}'
var nomBase = 'perochenow'
var registrePrive = !empty(jetonGhcr)
var avecDomaine = !empty(domainePersonnalise) && !empty(idCertificat)

// ---------- Base de données ----------

// Aucun mot de passe : seule l'identité managée de la Container App peut se connecter (administrateur Microsoft Entra).
resource serveurSql 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: nomServeurSql
  location: emplacement
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    // Pris en compte à la création du serveur ; sur un serveur existant, l'administrateur est mis à jour
    // mais l'authentification Entra seule passe par la ressource « entraUniquement » ci-dessous.
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Application'
      login: app.name
      sid: app.identity.principalId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// Désactive l'authentification SQL (identifiant + mot de passe) sur un serveur créé avant ce changement.
resource entraUniquement 'Microsoft.Sql/servers/azureADOnlyAuthentications@2023-08-01-preview' = {
  parent: serveurSql
  name: 'Default'
  properties: {
    azureADOnlyAuthentication: true
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

// Nom d'hôte calculé plutôt que lu sur le serveur : le serveur dépend de l'identité de l'app, l'inverse créerait un cycle.
// Pas de secret : l'app s'authentifie avec son identité managée.
var chaineDeConnexion = 'Server=tcp:${nomServeurSql}${environment().suffixes.sqlServerHostname},1433;Initial Catalog=${nomBase};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'

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
  // Identité utilisée pour se connecter à la base sans mot de passe.
  identity: {
    type: 'SystemAssigned'
  }
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
      secrets: registrePrive
        ? [
            {
              name: 'jeton-ghcr'
              value: jetonGhcr
            }
          ]
        : []
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
          env: [
            {
              name: 'ConnectionStrings__PerocheNow'
              value: chaineDeConnexion
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

output adresseParDefaut string = 'https://${app.properties.configuration.ingress.fqdn}'
output ipStatique string = environnement.properties.staticIp
output idVerificationDomaine string = app.properties.customDomainVerificationId
output nomEnvironnement string = environnement.name
