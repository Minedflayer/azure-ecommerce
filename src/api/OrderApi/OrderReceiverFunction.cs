using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace OrderApi;

public class OrderReceiverFunction
{
    private readonly ILogger<OrderReceiverFunction> _logger;

    public OrderReceiverFunction(ILogger<OrderReceiverFunction> logger)
    {
        _logger = logger;
    }

    [Function("OrderReceiverFunction")]
    public async Task<OrderResponse> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req) 
    {
        _logger.LogInformation("Receiving new order payload.");

        
// Read and strictly deserialize the incoming JSON
var order = await req.ReadFromJsonAsync<OrderPayload>();

// Validate specific business logic
if (order == null || string.IsNullOrWhiteSpace(order.OrderId)) 
{
    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
    await badResponse.WriteStringAsync("Invalid order format. OrderId is required.");
    return new OrderResponse { HttpResponse = badResponse };
}

// Serialize the validated object back to a JSON string for the Service Bus
string validatedMessage = System.Text.Json.JsonSerializer.Serialize(order);

        // Create HTTP 202 Accepted Response
        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteStringAsync("Order received and queued for processing.");

        // Return both HTTP response and payload for the Service Bus
        return new OrderResponse 
        {
            HttpResponse = response,
            ServiceBusMessage = validatedMessage
        };
    } 
}

// 1. Define the Expected Payload
public class OrderPayload
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}


public class OrderResponse
{
    // Returns the HTTP response to the Webshop
    [HttpResult]
    public HttpResponseData HttpResponse { get; set; } = null!;

    // Drops the payload into the Service Bus Queue
    [ServiceBusOutput("orders-queue", Connection = "ServiceBusConnection")]
    public string? ServiceBusMessage { get; set; }
}


