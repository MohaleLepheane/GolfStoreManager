using GolfStore.Data;
using GolfStore.Models;
using GolfStore.Repositories;
using Microsoft.Data.Sqlite;

namespace GolfStore.UI;

public sealed class ProductMenu(ProductRepository products)
{
    public void Run()
    {
        while (true)
        {
            ConsoleUi.Header("Products & Inventory");
            Console.WriteLine("1) List all products");
            Console.WriteLine("2) Search products");
            Console.WriteLine("3) Add product");
            Console.WriteLine("4) Edit product");
            Console.WriteLine("5) Adjust stock (receive shipment / write off)");
            Console.WriteLine("6) Delete product");
            Console.WriteLine("0) Back");

            switch (ConsoleUi.ReadMenuChoice())
            {
                case "1": ConsoleUi.Execute(() => PrintProducts(products.GetAll())); break;
                case "2": ConsoleUi.Execute(Search); break;
                case "3": ConsoleUi.Execute(Add); break;
                case "4": ConsoleUi.Execute(Edit); break;
                case "5": ConsoleUi.Execute(AdjustStock); break;
                case "6": ConsoleUi.Execute(Delete); break;
                case "0": return;
                default: ConsoleUi.Execute(() => ConsoleUi.Error("Unknown option.")); break;
            }
        }
    }

    public static void PrintProducts(IEnumerable<Product> list) =>
        ConsoleUi.PrintTable(
            ["ID", "SKU", "Name", "Brand", "Category", "Price", "Stock"],
            list.Select(p => new[]
            {
                p.Id.ToString(), p.Sku, p.Name, p.Brand, p.Category,
                p.Price.ToString("C"), p.QuantityInStock.ToString()
            }),
            0, 5, 6);

    private void Search()
    {
        var term = ConsoleUi.ReadString("Search by name, brand, SKU or category");
        PrintProducts(products.Search(term));
    }

    private void Add()
    {
        var product = new Product(
            Id: 0,
            Sku: ConsoleUi.ReadString("SKU").ToUpperInvariant(),
            Name: ConsoleUi.ReadString("Name"),
            Brand: ConsoleUi.ReadString("Brand"),
            Category: ConsoleUi.ReadString("Category (e.g. Drivers, Irons, Balls)"),
            Price: ConsoleUi.ReadDecimal("Price"),
            QuantityInStock: ConsoleUi.ReadInt("Starting stock", min: 0));

        var id = products.Add(product);
        ConsoleUi.Success($"Added product #{id}: {product.Name}.");
    }

    private void Edit()
    {
        var existing = Pick();
        Console.WriteLine("Press Enter to keep the current value.");

        var updated = existing with
        {
            Sku = ConsoleUi.ReadString("SKU", existing.Sku).ToUpperInvariant(),
            Name = ConsoleUi.ReadString("Name", existing.Name),
            Brand = ConsoleUi.ReadString("Brand", existing.Brand),
            Category = ConsoleUi.ReadString("Category", existing.Category),
            Price = ConsoleUi.ReadDecimal("Price", current: existing.Price),
            QuantityInStock = ConsoleUi.ReadInt("Stock", min: 0, current: existing.QuantityInStock)
        };

        products.Update(updated);
        ConsoleUi.Success($"Product #{updated.Id} updated.");
    }

    private void AdjustStock()
    {
        var product = Pick();
        Console.WriteLine($"{product.Name} currently has {product.QuantityInStock} in stock.");
        var delta = ConsoleUi.ReadInt("Change in quantity (e.g. 10 to receive, -2 to write off)");

        if (!products.AdjustStock(product.Id, delta))
            throw new InvalidOperationException("Stock can't go below zero.");

        ConsoleUi.Success($"Stock for {product.Name} is now {product.QuantityInStock + delta}.");
    }

    private void Delete()
    {
        var product = Pick();
        if (!ConsoleUi.Confirm($"Delete {product.Name}?")) return;

        try
        {
            products.Delete(product.Id);
            ConsoleUi.Success("Product deleted.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteExtensions.ConstraintViolation)
        {
            ConsoleUi.Error("This product appears on existing orders, so it can't be deleted. " +
                            "Set its stock to 0 instead.");
        }
    }

    private Product Pick()
    {
        var id = ConsoleUi.ReadInt("Product ID", min: 1);
        return products.GetById(id) ?? throw new InvalidOperationException($"Product #{id} was not found.");
    }
}
