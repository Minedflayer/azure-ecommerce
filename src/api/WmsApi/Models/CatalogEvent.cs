namespace WmsApi.Models;

public class CatalogEvent
{
    public string EventType { get; set; } = string.Empty;
    public OrderDetails? Order { get; set; }
}