namespace WmsApi.Models;
public class OrderDetails
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}