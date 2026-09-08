namespace CatalogApi.Models;


// Junction table entity for many-to-many mapping
public class ProductCategory
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
}