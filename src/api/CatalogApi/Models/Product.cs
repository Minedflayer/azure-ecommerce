using System;
using System.Collections.Generic;

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

public class Category
{
    public Guid Id {get; set;} = Guid.NewGuid();

    public string Name {get; set;} = string.Empty;
    public string Description {get; set;} = string.Empty;


    // Hierachical categories
    public Guid? ParentCategoryId {get; set;}
    public Category? ParentCategory {get; set;}

    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();

}

// Junction table entity for many-to-many mapping
public class ProductCategory
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
}

