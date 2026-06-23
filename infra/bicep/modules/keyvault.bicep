// Module: keyvault.bicep
// Creates Azure Key Vault with RBAC authorization model
// Secrets must be added manually or via az keyvault secret set after deployment

param location string
param keyVaultName string

@description('Object ID of the managed identity or user that needs secret access (optional - can be added post-deploy)')
param secretReaderObjectId string = ''

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true   // Use RBAC instead of access policies (recommended)
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Disabled'  // Restrict to VNet in production
  }
}

// Grant Key Vault Secrets User role to caller if provided
// Role: Key Vault Secrets User (4633458b-17de-408a-b874-0445c86b69e6)
resource secretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(secretReaderObjectId)) {
  name: guid(keyVault.id, secretReaderObjectId, '4633458b-17de-408a-b874-0445c86b69e6')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: secretReaderObjectId
    principalType: 'ServicePrincipal'
  }
}

output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
