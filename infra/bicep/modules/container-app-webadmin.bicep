// Module: container-app-webadmin.bicep
// Azure Container App for TokenShield web-admin (Next.js)

param location string
param containerAppsEnvironmentId string
param appName string
param acrLoginServer string
param imageTag string = 'latest'
param gatewayApiUrl string = ''

var imageName = '${acrLoginServer}/tokenshield/web-admin:${imageTag}'

resource webAdminApp 'Microsoft.App/containerApps@2024-03-01' = {
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
        targetPort: 3000
        transport: 'auto'
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
          name: 'web-admin'
          image: imageName
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'NODE_ENV'
              value: 'production'
            }
            {
              name: 'PORT'
              value: '3000'
            }
            {
              name: 'HOSTNAME'
              value: '0.0.0.0'
            }
            {
              name: 'NEXT_PUBLIC_API_URL'
              value: gatewayApiUrl
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/'
                port: 3000
                scheme: 'HTTP'
              }
              initialDelaySeconds: 20
              periodSeconds: 30
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '30'
              }
            }
          }
        ]
      }
    }
  }
}

output fqdn string = webAdminApp.properties.configuration.ingress.fqdn
output principalId string = webAdminApp.identity.principalId
output appId string = webAdminApp.id
