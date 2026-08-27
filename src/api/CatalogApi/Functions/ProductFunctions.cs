
using System.Net;
using System.Text.Json;
using CatalogApi.Data;
using CatalogApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;


namespace CatalogApi.Functions;

public class ProductFunctions
{
    private readonly CatalogDbContext _dbContext;
    private readonly ILogger<ProductFunctions> _logger;

    // Inject DbContext and Logger in the isolated worker model
    public ProductFunctions(CatalogDbContext dbContext, ILogger<ProductFunctions> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [Function("GetProducts")]
    public async Task<HttpResponseData> GetProducts(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route ="products")] HttpRequestData req)
    {
        var products = await _dbContext.Products
        .Include(p => p.ProductCategories)
        .ThenInclude(p => p.Category)
        .ToListAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(products);        
        
        return response;
        
    }


    [Function("CreateProduct")]
    public async Task<HttpResponseData> CreateProduct(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route ="products")] HttpRequestData req)
    {
        _logger.LogInformation("Processing creation of new product.");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var product = JsonSerializer.Deserialize<Product>(requestBody, new JsonSerializerOptions {PropertyNameCaseInsensitive = true});

        if(product == null || string.IsNullOrWhiteSpace(product.SKU))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid product payload or missing SKU");
            return badRequest;
        }

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var response = req.CreateResponse(HttpStatusCode.Created);
        return response;
        
    }

    [Function("UpdateProduct")]
    public async Task<HttpResponseData> UpdateProduct(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route ="products/{id:guid}")] HttpRequestData req, Guid id)
    {
        _logger.LogInformation($"Processing update for product with ID: {id}");
        var existingProduct = await _dbContext.Products.FindAsync(id);

        if(existingProduct == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync($"Product with ID {id} not found.");
            return notFoundResponse;
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updatedProduct = JsonSerializer.Deserialize<Product>(requestBody, new JsonSerializerOptions {PropertyNameCaseInsensitive = true});

        if(updatedProduct == null || string.IsNullOrWhiteSpace(updatedProduct.SKU))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid product payload or missing SKU");
            return badRequest;
        }

        // Update properties
        existingProduct.SKU = updatedProduct.SKU;
        existingProduct.Name = updatedProduct.Name;
        existingProduct.Description = updatedProduct.Description;
        existingProduct.StockQuantity = updatedProduct.StockQuantity;
        existingProduct.Price = updatedProduct.Price;
        existingProduct.IsActive = updatedProduct.IsActive;

        await _dbContext.SaveChangesAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        return response;
        
    }

    [Function("DeleteProduct")]
    public async Task<HttpResponseData> DeleteProduct(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route ="products/{id:guid}")] HttpRequestData req, Guid id)
    {
        _logger.LogInformation($"Processing deletion for product with ID: {id}");
        var existingProduct = await _dbContext.Products.FindAsync(id);

        if(existingProduct == null)
        {
            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync($"Product with ID {id} not found.");
            return notFoundResponse;
        }

        _dbContext.Products.Remove(existingProduct);
        await _dbContext.SaveChangesAsync();

        var response = req.CreateResponse(HttpStatusCode.NoContent);
        return response;
        
    }    
    
    
    
    
}