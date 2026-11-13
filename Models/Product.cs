namespace GolfStore.Models;

public record Product(
    int Id,
    string Sku,
    string Name,
    string Brand,
    string Category,
    decimal Price,
    int QuantityInStock);
