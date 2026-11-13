namespace GolfStore.Models;

public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>An order header with its totals, as shown in order lists.</summary>
public record OrderSummary(
    int Id,
    int CustomerId,
    string CustomerName,
    DateTime OrderDate,
    OrderStatus Status,
    int ItemCount,
    decimal Total);

/// <summary>One product line on an order.</summary>
public record OrderLine(
    int ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

/// <summary>A product and quantity the user wants to put on a new order.</summary>
public record CartItem(int ProductId, int Quantity);

/// <summary>A row of the sales-by-category report.</summary>
public record CategorySales(string Category, int UnitsSold, decimal Revenue);
