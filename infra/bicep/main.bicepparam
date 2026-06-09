// TokenShield AI Gateway — Bicep Parameters (Development)
// Copy to main.dev.bicepparam / main.prod.bicepparam for each environment.
// NEVER commit real passwords or secrets here.

using './main.bicep'

param prefix = 'tokenshield'
param location = 'eastus'
param environment = 'dev'
param dbAdminLogin = 'tsadmin'

// Set this via: az deployment group create ... --parameters dbAdminPassword=$DB_PASSWORD
// Or use a Key Vault reference:
//   param dbAdminPassword = getSecret('<subscriptionId>', '<resourceGroup>', '<keyVaultName>', 'DbPassword')
param dbAdminPassword = ''

param gatewayImageTag = 'latest'
param webAdminImageTag = 'latest'
