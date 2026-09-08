namespace CatalogApi.Models;

public class Product
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public string SKU {get; set;} = string.Empty;
    public string Name {get; set;} = string.Empty;
    public string Description {get; set;} = string.Empty;
    public int StockQuantity {get; set;}    
    public decimal Price {get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
}


