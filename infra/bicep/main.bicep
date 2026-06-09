// TokenShield AI Gateway — Azure Bicep Main Orchestration
// Deploys: Container Apps Environment, PostgreSQL Flexible Server,
//          Key Vault, Application Insights, Log Analytics, Container Registry

targetScope = 'resourceGroup'

@description('Short name prefix used for all resources (e.g. tokenshield)')
param prefix string = 'tokenshield'

@description('Azure region to deploy into')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('PostgreSQL administrator login')
param dbAdminLogin string = 'tsadmin'

@description('PostgreSQL administrator password — use a Key Vault reference in production')
@secure()
param dbAdminPassword string

@description('Container image tag to deploy for gateway-api')
param gatewayImageTag string = 'latest'

@description('Container image tag to deploy for web-admin')
param webAdminImageTag string = 'latest'

var resourceSuffix = '${prefix}-${environment}'
var acrName = '${replace(prefix, '-', '')}${environment}acr'

// ─── Log Analytics Workspace + Application Insights ────────────────────────
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    workspaceName: 'log-${resourceSuffix}'
    appInsightsName: 'appi-${resourceSuffix}'
  }
}

// ─── Azure Container Registry ───────────────────────────────────────────────
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

// ─── Key Vault ──────────────────────────────────────────────────────────────
module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    keyVaultName: 'kv-${resourceSuffix}'
  }
}

// ─── PostgreSQL Flexible Server ─────────────────────────────────────────────
module postgresql 'modules/postgresql.bicep' = {
  name: 'postgresql'
  params: {
    location: location
    serverName: 'psql-${resourceSuffix}'
    adminLogin: dbAdminLogin
    adminPassword: dbAdminPassword
    databaseName: 'tokenshield'
  }
}

// ─── Container Apps Environment ─────────────────────────────────────────────
resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${resourceSuffix}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: monitoring.outputs.workspaceCustomerId
        sharedKey: monitoring.outputs.workspaceSharedKey
      }
    }
  }
}

// ─── Gateway API Container App ──────────────────────────────────────────────
module gatewayApp 'modules/container-app-gateway.bicep' = {
  name: 'gateway-app'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnv.id
    containerAppsEnvironmentName: containerAppsEnv.name
    appName: 'ca-${prefix}-gateway-${environment}'
    acrLoginServer: acr.properties.loginServer
    imageTag: gatewayImageTag
    dbConnectionString: 'Host=${postgresql.outputs.serverFqdn};Port=5432;Database=tokenshield;Username=${dbAdminLogin};Password=${dbAdminPassword};Ssl Mode=VerifyFull'
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    keyVaultName: keyVault.outputs.keyVaultName
  }
}

// ─── Web Admin Container App ────────────────────────────────────────────────
module webAdminApp 'modules/container-app-webadmin.bicep' = {
  name: 'webadmin-app'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnv.id
    appName: 'ca-${prefix}-webadmin-${environment}'
    acrLoginServer: acr.properties.loginServer
    imageTag: webAdminImageTag
    gatewayApiUrl: 'https://${gatewayApp.outputs.fqdn}'
  }
}

// ─── Outputs ─────────────────────────────────────────────────────────────────
output gatewayApiUrl string = 'https://${gatewayApp.outputs.fqdn}'
output webAdminUrl string = 'https://${webAdminApp.outputs.fqdn}'
output acrLoginServer string = acr.properties.loginServer
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString
