// Module: container-app-gateway.bicep
// Azure Container App for TokenShield gateway-api

param location string
param containerAppsEnvironmentId string
param containerAppsEnvironmentName string
param appName string
param acrLoginServer string
param imageTag string = 'latest'

@secure()
param dbConnectionString string

param appInsightsConnectionString string = ''
param keyVaultName string = ''
param corsAllowedOrigin string = ''

var imageName = '${acrLoginServer}/tokenshield/gateway-api:${imageTag}'

// System-assigned managed identity for Key Vault & ACR access
resource gatewayApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      ingress: {
        external: true
        targetPort: 5000
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: empty(corsAllowedOrigin) ? [] : [corsAllowedOrigin]
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
          allowedHeaders: ['*']
        }
      }
      registries: [
        {
          server: acrLoginServer
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'gateway-api'
          image: imageName
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:5000'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              value: dbConnectionString
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              value: appInsightsConnectionString
            }
            {
              name: 'OpenTelemetry__ServiceName'
              value: 'tokenshield-gateway'
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: corsAllowedOrigin
            }
            {
              name: 'SeedDatabase'
              value: 'false'   // Do not seed in production
            }
            {
              name: 'Providers__UseRealProviders'
              value: 'false'   // Set to true when provider credentials are in Key Vault
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 5000
                scheme: 'HTTP'
              }
              initialDelaySeconds: 15
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 5000
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 15
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '20'
              }
            }
          }
        ]
      }
    }
  }
}

output fqdn string = gatewayApp.properties.configuration.ingress.fqdn
output principalId string = gatewayApp.identity.principalId
output appId string = gatewayApp.id
