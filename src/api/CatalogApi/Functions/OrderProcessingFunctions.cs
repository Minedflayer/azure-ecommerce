using System.Text.Json;
using Azure.Messaging.ServiceBus;
using CatalogApi.Data;
using CatalogApi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CatalogApi.Functions;

public class OrderProcessingFunctions
{
    private readonly CatalogDbContext _dbContext;
    private readonly ILogger<OrderProcessingFunctions> _logger;
    private readonly ServiceBusClient _serviceBusClient;

    public OrderProcessingFunctions(
        CatalogDbContext dbContext,
        ILogger<OrderProcessingFunctions> logger,
        ServiceBusClient serviceBusClient)
    {
        _dbContext = dbContext;
        _logger = logger;
        _serviceBusClient = serviceBusClient;
    }

    [Function("ProcessOrderQueue")]
    public async Task Run(
        [ServiceBusTrigger("orders-queue", Connection = "ServiceBusConnection")] string queueMessage)
    {
        _logger.LogInformation("Dequeued order message: {Message}", queueMessage);

        // Deserialize the incoming payload from the OrderApi
        var payload = JsonSerializer.Deserialize<OrderPayload>(queueMessage, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload == null || string.IsNullOrWhiteSpace(payload.OrderId))
        {
            _logger.LogError("Invalid order payload or missing OrderId received.");
            return;
        }

        // Map the payload to the EF Core Entity
        var newOrder = new Order
        {
            OrderId = payload.OrderId,
            CustomerEmail = payload.CustomerEmail,
            TotalAmount = payload.TotalAmount,
            IntegrationStatus = "Queued",
            CreatedAt = DateTime.UtcNow
        };

        // Save to Azure SQL Database
        _dbContext.Orders.Add(newOrder);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Successfully persisted Order {OrderId} to SQL Database.", newOrder.OrderId);

        //  Broadcast the OrderCreated domain event to the Topic
        var topicName = Environment.GetEnvironmentVariable("CatalogTopicName");
        await using var sender = _serviceBusClient.CreateSender(topicName);

        var eventPayload = new
        {
            EventType = "OrderCreated",
            Order = newOrder
        };

        var broadcastMessage = new ServiceBusMessage(JsonSerializer.Serialize(eventPayload));
        broadcastMessage.ApplicationProperties.Add("EventType", "OrderCreated");

        await sender.SendMessageAsync(broadcastMessage);
        _logger.LogInformation("Dispatched OrderCreated event to topic: {Topic}", topicName);
    }
}

// DTO to match the structure published by the OrderApi
public class OrderPayload
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}