using Microsoft.EntityFrameworkCore;
using CatalogApi.Models;

namespace CatalogApi.Data;

// <summary>
// DbContext for the Catalog API, managing Products, Categories, and their relationships.
// This context is configured to use SQL Server and includes configurations for many-to-many relationships
// between Products and Categories, as well as hierarchical relationships for Categories.
// </summary>
public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductCategory>().HasKey(pc => new {pc.ProductId, pc.CategoryId});

        modelBuilder.Entity<ProductCategory>()
        .HasOne(pc => pc.Product)
        .WithMany(p => p.ProductCategories)
        .HasForeignKey(pc => pc.ProductId);

        modelBuilder.Entity<ProductCategory>()
        .HasOne(pc => pc.Category)
        .WithMany(c => c.ProductCategories)
        .HasForeignKey(pc => pc.CategoryId);


        modelBuilder.Entity<Category>()
        .HasOne(c => c.ParentCategory)
        .WithMany(c => c.SubCategories)
        .HasForeignKey(c => c.ParentCategoryId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
        .HasIndex(p => p.SKU)
        .IsUnique();


    }
    
}