using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace WmsApi;

public class DeliveryReceiverFunction
{
    private readonly ILogger<DeliveryReceiverFunction> _logger;

    public DeliveryReceiverFunction(ILogger<DeliveryReceiverFunction> logger)
    {
        _logger = logger;
    }

    [Function("DeliveryReceiverFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "delivery")] HttpRequestData req)
    {
        _logger.LogInformation("WMS API received a new delivery processing request from Logic App.");

        // Read incoming JSON
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        // In a real scenario, you would deserialize this into a strongly-typed C# object.
        // For the mock WMS, we simply log the raw JSON payload to verify the data flow.
        _logger.LogInformation($"Incoming delivery payload details:\n{requestBody}");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        await response.WriteStringAsync("{\"status\": \"success\", \"message\": \"Delivery details successfully registered in mock WMS.\"}");

        return response;

    }

}
