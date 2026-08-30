using System.ComponentModel.DataAnnotations;

namespace CatalogApi.Models;

public class Order
{
    [Key]
    public string OrderId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string IntegrationStatus { get; set; } = "Queued";
    public DateTime CreatedAt { get; set; }
}