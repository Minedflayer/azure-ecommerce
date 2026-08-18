# E-Commerce Integration Pipeline

An event-driven integration application designed to process e-commerce orders, route messages securely through cloud infrastructure, and synchronize data with a Warehouse Management System (WMS). The application serves the purpose of acquiring a basic understanding of event-driven applications in Azure.

## Architecture Overview

*   **Order API (Azure Function):** Receives incoming HTTP order payloads (e.g., `OrderId`, `CustomerEmail`, `TotalAmount`), validates the data, and queues a message for downstream processing.
*   **Message Broker (Azure Service Bus):** Decouples order ingestion from warehouse processing, ensuring reliable message delivery.
*   **Workflow Orchestration (Azure Logic Apps):** Subscribes to the Service Bus and triggers downstream APIs upon message receipt.
*   **WMS API (Azure Function):** Receives delivery processing requests from the Logic App and finalizes the warehouse synchronization.
*   **Data Store (Azure SQL Database):** Maintains records of customers and orders using idempotent `UPSERT` logic.

![Description](images\architechture.png)

## Technology Stack

*   **Core:** C# .NET 8.0, Azure Functions (Isolated Worker Model)
*   **Infrastructure as Code (IaC):** Azure Bicep
*   **CI/CD:** GitHub Actions
*   **Testing:** xUnit, Moq

## Repository Structure

```text

├── .github/workflows/deploy-pipeline.yml  # Automated CI/CD pipeline
├── infra/main.bicep                       # Azure resource definitions
├── src/
│   ├── api/OrderApi/                      # Ingestion Function App
│   └── api/WmsApi/                        # Downstream Warehouse Function App
└── tests/
    ├── OrderApi.Tests/                    # xUnit tests and mocked contexts
    └── WmsApi.Tests/                      # xUnit tests for WMS logic
```
## Continuous Integration and Deployment
Deployments are automated via a GitHub Actions pipeline (deploy-pipeline.yml). The workflow is separated into the following phases:
1. **Build and Test:** Compiles the .NET 8 source code and executes the xUnit test suites to validate logic and configuration prior to deployment.
2. **Deploy Infrastructure:** Connects to Azure and provisions the required resources (e.g., rg-ecommerce-prod) using the defined main.bicep template.
3. **Deploy Functions:** Publishes the compiled artifacts for both the Order API and WMS API to the Azure App Service.

