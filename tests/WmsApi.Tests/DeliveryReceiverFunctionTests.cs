using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;
using WmsApi; // Ensure this matches your actual namespace

namespace WmsApi.Tests;

public class DeliveryReceiverFunctionTests
{
    private readonly Mock<ILogger<DeliveryReceiverFunction>> _mockLogger;
    private readonly DeliveryReceiverFunction _function;

    public DeliveryReceiverFunctionTests()
    {
        // 1. Arrange: Setup the shared mock logger and function instance
        _mockLogger = new Mock<ILogger<DeliveryReceiverFunction>>();
        _function = new DeliveryReceiverFunction(_mockLogger.Object);
    }

    [Fact]
    public void Run_WithValidOrderCreatedEvent_ExecutesWithoutException()
    {
        // Arrange: Create a valid JSON string matching the expected CatalogEvent structure
        var validPayload = JsonSerializer.Serialize(new
        {
            EventType = "OrderCreated",
            Order = new
            {
                OrderId = "ORD-TEST-123",
                CustomerEmail = "test@example.com",
                TotalAmount = 250.00m
            }
        });

        // Act: Pass the string directly into the function
        var exception = Record.Exception(() => _function.Run(validPayload));

        // Assert: Ensure the processing logic completes without throwing errors
        Assert.Null(exception);
    }

    [Fact]
    public void Run_WithInvalidJson_CatchesExceptionGracefully()
    {
        // Arrange: Create a malformed string
        string invalidPayload = "This is not valid JSON";

        // Act: Pass the bad string into the function
        var exception = Record.Exception(() => _function.Run(invalidPayload));

        // Assert: The function contains a try-catch for JsonException, 
        // so it should handle it internally and NOT throw it back to the runtime.
        Assert.Null(exception);
    }
}