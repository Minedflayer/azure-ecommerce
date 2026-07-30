@description('The location for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Short prefix for resources to maintain naming length limits.')
param prefix string = 'ecom'

// Create a 13-character unique hash based on the resource group
var uniqueSeed = uniqueString(resourceGroup().id) 
var baseName = '${prefix}${uniqueSeed}' // Total: 17 characters

// Service Bus Namespace (Changed suffix from '-sb' to 'ns' to avoid reserved suffix error)
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
    name: '${baseName}ns'
    location: location
    sku: {
        name: 'Basic'
        tier: 'Basic'
    }
}

resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
    parent: serviceBusNamespace
    name: 'orders-queue'
}

// Storage account (Total length is now 22 characters, safely under the 24-character limit)
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: '${baseName}store'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

// App Service Plan
resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
    name: '${baseName}-plan'
    location: location
    sku: {
        name: 'Y1'
        tier: 'Dynamic'
    }
    properties: {
        reserved: false
    }
}

// Azure Function App (The API Entry Point)
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: '${baseName}-api'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower('${baseName}-share')
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'ServiceBusConnection'
          value: listKeys(resourceId('Microsoft.ServiceBus/namespaces/authorizationRules', serviceBusNamespace.name, 'RootManageSharedAccessKey'), '2022-10-01-preview').primaryConnectionString
        }
      ]
    }
  }
}
