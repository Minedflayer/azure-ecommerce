
namespace CatalogApi.Models;
public class Category
{
    public Guid Id {get; set;} = Guid.NewGuid();

    public string Name {get; set;} = string.Empty;
    public string Description {get; set;} = string.Empty;


    // Hierachical categories
    public Guid? ParentCategoryId {get; set;}
    public Category? ParentCategory {get; set;}
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();

    // Navigation property
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();

}