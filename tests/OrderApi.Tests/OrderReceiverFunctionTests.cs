using System.Text.Json;
using System.Net;
using System.IO;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Azure.Core.Serialization;

namespace OrderApi.Tests
{
    public class OrderReceiverFunctionTests
    {
        private readonly Mock<ILogger<OrderReceiverFunction>> _loggerMock;
        private readonly OrderReceiverFunction _sut; // System Under Test

        public OrderReceiverFunctionTests()
        {
            // Set up the mock logger required by the function constructor
            _loggerMock = new Mock<ILogger<OrderReceiverFunction>>();

            // Initialize the function (assuming constructor injection)
            _sut = new OrderReceiverFunction(_loggerMock.Object);
        }

        [Fact]
        public async Task OrderReceiverFunction_ValidPayload_ReturnsAcceptedAndQueuesMessage()
        {
           
            // Set up a mock HTTP request or payload here depending on your trigger setup
            var validOrder = new OrderPayload
            {
                OrderId = "ORD-12345",
                CustomerEmail = "customer@example.com",
                TotalAmount = 99.99m
            };

            var jsonPayload = JsonSerializer.Serialize(validOrder);

            var (mockRequest, mockResponse) = CreateMockRequest(jsonPayload);

            var result = await _sut.Run(mockRequest.Object);

            
            Assert.NotNull(result);
            Assert.NotNull(result.HttpResponse);
            Assert.Equal(HttpStatusCode.Accepted, result.HttpResponse.StatusCode);

            // Verify that the message IS queued for the Service Bus
            Assert.NotNull(result.ServiceBusMessage);
            Assert.Contains("ORD-12345", result.ServiceBusMessage);

            // Temporary placeholder assertion to ensure the test runner works
            Assert.True(true);
        }

        // Helper function
        private (Mock<HttpRequestData>, Mock<HttpResponseData>) CreateMockRequest(string body)
        {
            var mockContext = new Mock<FunctionContext>();

            var services = new ServiceCollection();
            services.AddOptions(); // Required for JSON serialization options

            // Add the specific WorkerOptions the isolated framework requires for JSON parsing
            services.Configure<WorkerOptions>(workerOptions =>
            {
                workerOptions.Serializer = new JsonObjectSerializer(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
            });


            var serviceProvider = services.BuildServiceProvider();

            mockContext.SetupProperty(c => c.InstanceServices, serviceProvider);

            var mockRequest = new Mock<HttpRequestData>(mockContext.Object);
            var mockResponse = new Mock<HttpResponseData>(mockContext.Object);

            // Request Body
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            mockRequest.Setup(r => r.Body).Returns(stream);

            // Response Body
            var responseStream = new MemoryStream();
            mockResponse.Setup(r => r.Body).Returns(responseStream);
            mockResponse.SetupProperty(r => r.StatusCode);

            // Safely mock the abstract HttpHeadersCollection
            var mockHeaders = new Mock<HttpHeadersCollection>();
            mockResponse.SetupProperty(r => r.Headers, mockHeaders.Object);

            // Link CreateResponse to return Mocked Response
            mockRequest.Setup(r => r.CreateResponse()).Returns(mockResponse.Object);


            return (mockRequest, mockResponse);
        }
    }
}