using System.IO;
using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using WmsApi;

namespace WmsApi.Tests
{
    public class DeliveryReceiverFunctionTests
    {
        private readonly Mock<ILogger<DeliveryReceiverFunction>> _loggerMock;
        private readonly DeliveryReceiverFunction _sut; // System Under Test

        public DeliveryReceiverFunctionTests()
        {
            _loggerMock = new Mock<ILogger<DeliveryReceiverFunction>>();
            _sut = new DeliveryReceiverFunction(_loggerMock.Object);
        }

        [Fact]
        public async Task DeliveryReceiverFunction_ValidPayload_ReturnsOkWithSuccessResponseBody()
        {
            // Arrange
            string validPayload = "{\"OrderId\":\"ORD-CLOUD-1002\",\"CustomerEmail\":\"victor@example.com\",\"TotalAmount\":299.99}";
            var (mockRequest, mockResponse) = CreateMockRequest(validPayload);

            // Act
            var response = await _sut.Run(mockRequest.Object);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Read response stream body to verify written string
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body, Encoding.UTF8);
            string responseBody = await reader.ReadToEndAsync();

            Assert.Contains("Delivery details successfully registered in mock WMS.", responseBody);
            Assert.Contains("success", responseBody);
            
        }

        [Fact]
        public async Task DeliveryReceiverFunction_EmptyPayload_ReturnsOk()
        {
            // Arrange
            string emptyPayload = "";
            var (mockRequest, mockResponse) = CreateMockRequest(emptyPayload);

            // Act
            var response = await _sut.Run(mockRequest.Object);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private (Mock<HttpRequestData>, Mock<HttpResponseData>) CreateMockRequest(string body)
        {
            var mockContext = new Mock<FunctionContext>();
            
            var services = new ServiceCollection();
            services.AddOptions();
            var serviceProvider = services.BuildServiceProvider();

            mockContext.SetupProperty(c => c.InstanceServices, serviceProvider);

            var mockRequest = new Mock<HttpRequestData>(mockContext.Object);
            var mockResponse = new Mock<HttpResponseData>(mockContext.Object);

            // Mock Request Body
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            mockRequest.Setup(r => r.Body).Returns(stream);

            // Mock Response Body and Properties
            var responseStream = new MemoryStream();
            mockResponse.Setup(r => r.Body).Returns(responseStream);
            mockResponse.SetupProperty(r => r.StatusCode);

            // HttpHeader
            var mockHeaders = new Mock<HttpHeadersCollection>();
            mockResponse.SetupProperty(r => r.Headers, mockHeaders.Object);

            // Link CreateResponse to return Mocked Response
            mockRequest.Setup(r => r.CreateResponse()).Returns(mockResponse.Object);


            return(mockRequest, mockResponse);
            
        }
    }
}