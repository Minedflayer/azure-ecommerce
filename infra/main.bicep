@description('The location for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Short prefix for resources to maintain naming length limits.')
param prefix string = 'ecom'

@secure()
param sqlAdminLogin string

@secure()
param sqlAdminPassword string

// Create a 13-character unique hash based on the resource group
var uniqueSeed = uniqueString(resourceGroup().id) 
var baseName = '${prefix}${uniqueSeed}' // Total: 17 characters

// 1. Log Analytics Workspace (Required backend for modern Application Insights)
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${baseName}-law'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// 2. Application Insights Component
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
  name: '${baseName}ns'
  location: location
  sku: {
    name: 'Standard' 
    tier: 'Standard'
  }
}

//====================================================
// Service Bus Queues, Topics & Subscriptions
//====================================================

// Queue for ingestion (OrderApi -> CatalogApi)
resource ordersQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'orders-queue'
}

// Topic for Domain Events (CatalogApi -> Downstream Systems)
resource catalogTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'catalog-topic'
}

// Subscriptions

// Subscription for the Logic app to process events
resource logicAppCatalogSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: catalogTopic
  name: 'logic-app-processing'
}

// Subscription for the WMS API to listen to product updates (e.g., dimension/SKU changes)
resource wmsInventorySub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: catalogTopic
  name: 'wms-inventory-updates'
}

// resource ordersTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
//   parent: serviceBusNamespace
//   name: 'orders-topic'
// }



// // Sub fo the Logic app to process order events
// resource logicAppOrderSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
//   parent:ordersTopic
//   name:'logic-app-order-processing'
// }

//===================================================

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
// Function Apps
//================================================================
//================================================================
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
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }

        
      ]
    }
  }
}


//  Azure Function App (CatalogApi)
//  New microservice dedicated to Product/Category CRUD operations. It publishes 
// 'ProductUpdated' events to the Service Bus to keep the architecture decoupled.
resource catalogFunctionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: '${baseName}-catalog-api'
  location:location
  kind: 'functionapp'
  properties: {
    serverFarmId:hostingPlan.id // Reusing existing app service
    siteConfig:{
      appSettings: [
        {
          name:'AzureWebJobsStorage'
          value:'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name:'ServiceBusConnection'
          value:listKeys(resourceId('Microsoft.ServiceBus/namespaces/authorizationRules', serviceBusNamespace.name, 'RootManageSharedAccessKey'), '2022-10-01-preview').primaryConnectionString
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'CatalogTopicName'
          value: catalogTopic.name
        }
      ]
    }
  }

}

// Azure Function App (WmsApi)
// The WMS API needs connection details to actively listen to the catalog-topic 
// for inventory/product changes via a ServiceBusTrigger.
resource wmsFunctionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: '${baseName}-wms-api'
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
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'ServiceBusConnection' // Required for the WmsApi to subscribe to Service Bus events
          value: listKeys(resourceId('Microsoft.ServiceBus/namespaces/authorizationRules', serviceBusNamespace.name, 'RootManageSharedAccessKey'), '2022-10-01-preview').primaryConnectionString
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'CatalogTopicName'
          value: catalogTopic.name
        }
      ]
    }
  }
}
//================================================================
//================================================================


// Azure SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: '${baseName}-sqlserver'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
  }
}

// Azure SQL Database (Serverless Free/Low-Cost Tier Configuration)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: 'crm-db'
  location: location
  sku: {
    name: 'GP_S_Gen5_1' // General Purpose, Serverless, Gen5, 1 vCore
    tier: 'GeneralPurpose'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    autoPauseDelay: 60 // Automatically pauses database after 60 minutes of inactivity to zero out costs
  }
}

// Allow Azure Services to access the SQL Server (Required for the Logic App later)
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}


