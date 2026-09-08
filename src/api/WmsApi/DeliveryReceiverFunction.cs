using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WmsApi.Models;

namespace WmsApi;

public class DeliveryReceiverFunction
{
    private readonly ILogger<DeliveryReceiverFunction> _logger;

    public DeliveryReceiverFunction(ILogger<DeliveryReceiverFunction> logger)
    {
        _logger = logger;
    }

    [Function("DeliveryReceiverFunction")]
    public void Run(
        [ServiceBusTrigger("catalog-topic", "wms-inventory-updates", Connection = "ServiceBusConnection")] string message)
    {
        try
        {
            Console.WriteLine($"\n[RAW MESSAGE RECEIVED IN WMS]: {message}\n");

            var catalogEvent = JsonSerializer.Deserialize<CatalogEvent>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            // Route the event based on its type
            if (catalogEvent?.EventType == "OrderCreated" && catalogEvent.Order != null)
            {
                ProcessNewOrder(catalogEvent.Order);
            }
            else
            {
                // Handles ProductCreated, ProductUpdated, etc.
                _logger.LogInformation($"Received event type '{catalogEvent?.EventType}'. Metadata updated in WMS.");
            }
        }
        catch (JsonException ex)
        {

            _logger.LogError($"Failed to deserialize message: {ex.Message}");
        }
        _logger.LogInformation("WMS API received a new domain event from the catalog-topic.");

        _logger.LogInformation($"Incoming payload details:\n{message}");

    }

    private void ProcessNewOrder(OrderDetails order)
    {
    _logger.LogInformation("WMS processing started for order: {OrderId}. Customer: {CustomerEmail}. Amount: {TotalAmount}", 
        order.OrderId, 
        order.CustomerEmail, 
        order.TotalAmount);        
    }
}