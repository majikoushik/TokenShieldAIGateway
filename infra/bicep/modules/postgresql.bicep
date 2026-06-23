// Module: postgresql.bicep
// Azure Database for PostgreSQL Flexible Server

param location string
param serverName string
param adminLogin string

@secure()
param adminPassword string

param databaseName string = 'tokenshield'

@description('PostgreSQL version')
param postgresVersion string = '16'

@description('SKU name - use Standard_D2ds_v4 for production')
param skuName string = 'Standard_B2ms'

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: serverName
  location: location
  sku: {
    name: skuName
    tier: startsWith(skuName, 'Standard_B') ? 'Burstable' : 'GeneralPurpose'
  }
  properties: {
    version: postgresVersion
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  parent: postgresServer
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// AllowAllAzureServices has been intentionally removed to harden public access.
// In a production scenario, Private Endpoints (VNet integration) must be used instead.

output serverFqdn string = postgresServer.properties.fullyQualifiedDomainName
output serverId string = postgresServer.id
output databaseName string = database.name
