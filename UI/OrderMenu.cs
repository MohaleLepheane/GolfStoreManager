using GolfStore.Models;
using GolfStore.Repositories;

namespace GolfStore.UI;

public sealed class OrderMenu(OrderRepository orders, CustomerRepository customers, ProductRepository products)
{
    public void Run()
    {
        while (true)
        {
            ConsoleUi.Header("Orders");
            Console.WriteLine("1) List all orders");
            Console.WriteLine("2) View order details");
            Console.WriteLine("3) Create order");
            Console.WriteLine("4) Update order status");
            Console.WriteLine("5) Cancel order (returns stock)");
            Console.WriteLine("6) Delete order");
            Console.WriteLine("0) Back");

            switch (ConsoleUi.ReadMenuChoice())
            {
                case "1": ConsoleUi.Execute(() => PrintOrders(orders.GetAll())); break;
                case "2": ConsoleUi.Execute(View); break;
                case "3": ConsoleUi.Execute(Create); break;
                case "4": ConsoleUi.Execute(UpdateStatus); break;
                case "5": ConsoleUi.Execute(Cancel); break;
                case "6": ConsoleUi.Execute(Delete); break;
                case "0": return;
                default: ConsoleUi.Execute(() => ConsoleUi.Error("Unknown option.")); break;
            }
        }
    }

    public static void PrintOrders(IEnumerable<OrderSummary> list) =>
        ConsoleUi.PrintTable(
            ["Order", "Date", "Customer", "Status", "Items", "Total"],
            list.Select(o => new[]
            {
                $"#{o.Id}", o.OrderDate.ToString("yyyy-MM-dd HH:mm"), o.CustomerName,
                o.Status.ToString(), o.ItemCount.ToString(), o.Total.ToString("C")
            }),
            0, 4, 5);

    private void View()
    {
        var order = Pick();
        Console.WriteLine();
        Console.WriteLine($"Order #{order.Id}  |  {order.CustomerName}  |  {order.OrderDate:yyyy-MM-dd HH:mm}  |  {order.Status}");
        Console.WriteLine();

        ConsoleUi.PrintTable(
            ["SKU", "Product", "Qty", "Unit Price", "Line Total"],
            orders.GetLines(order.Id).Select(l => new[]
            {
                l.Sku, l.ProductName, l.Quantity.ToString(), l.UnitPrice.ToString("C"), l.LineTotal.ToString("C")
            }),
            2, 3, 4);

        Console.WriteLine($"\nOrder total: {order.Total:C}");
    }

    private void Create()
    {
        var allCustomers = customers.GetAll();
        if (allCustomers.Count == 0)
            throw new InvalidOperationException("Add a customer before creating an order.");

        CustomerMenu.PrintCustomers(allCustomers);
        var customerId = ConsoleUi.ReadInt("\nCustomer ID", min: 1);
        var customer = customers.GetById(customerId)
            ?? throw new InvalidOperationException($"Customer #{customerId} was not found.");

        var catalog = products.GetAll().Where(p => p.QuantityInStock > 0).ToDictionary(p => p.Id);
        if (catalog.Count == 0)
            throw new InvalidOperationException("Nothing is in stock right now.");

        Console.WriteLine($"\nNew order for {customer.FullName}. Products in stock:\n");
        ProductMenu.PrintProducts(catalog.Values);

        var cart = new Dictionary<int, int>(); // productId -> quantity
        while (true)
        {
            var productId = ConsoleUi.ReadInt("\nProduct ID to add (0 to finish)", min: 0);
            if (productId == 0) break;

            if (!catalog.TryGetValue(productId, out var product))
            {
                ConsoleUi.Error("That product isn't in the in-stock list.");
                continue;
            }

            var inCart = cart.GetValueOrDefault(productId);
            var available = product.QuantityInStock - inCart;
            if (available == 0)
            {
                ConsoleUi.Error("All available stock of that product is already in the cart.");
                continue;
            }

            cart[productId] = inCart + ConsoleUi.ReadInt($"Quantity (1-{available})", 1, available);
            PrintCart(cart, catalog);
        }

        if (cart.Count == 0)
        {
            ConsoleUi.Warning("No items added. Order discarded.");
            return;
        }

        if (!ConsoleUi.Confirm("\nPlace this order?"))
        {
            ConsoleUi.Warning("Order discarded.");
            return;
        }

        var orderId = orders.Create(customer.Id, cart.Select(kv => new CartItem(kv.Key, kv.Value)).ToList());
        ConsoleUi.Success($"Order #{orderId} placed. Inventory has been updated.");
    }

    private void UpdateStatus()
    {
        var order = Pick();
        Console.WriteLine($"Order #{order.Id} is currently {order.Status}.");
        Console.WriteLine("1) Pending   2) Shipped   3) Delivered");

        var newStatus = ConsoleUi.ReadInt("New status", 1, 3) switch
        {
            1 => OrderStatus.Pending,
            2 => OrderStatus.Shipped,
            _ => OrderStatus.Delivered
        };

        orders.UpdateStatus(order.Id, newStatus);
        ConsoleUi.Success($"Order #{order.Id} is now {newStatus}.");
    }

    private void Cancel()
    {
        var order = Pick();
        if (!ConsoleUi.Confirm($"Cancel order #{order.Id} for {order.CustomerName}?")) return;

        orders.Cancel(order.Id);
        ConsoleUi.Success($"Order #{order.Id} cancelled and its items returned to stock.");
    }

    private void Delete()
    {
        var order = Pick();
        var note = order.Status == OrderStatus.Pending ? " Its items will be returned to stock." : "";
        if (!ConsoleUi.Confirm($"Permanently delete order #{order.Id}?{note}")) return;

        orders.Delete(order.Id);
        ConsoleUi.Success($"Order #{order.Id} deleted.");
    }

    private OrderSummary Pick()
    {
        var id = ConsoleUi.ReadInt("Order number", min: 1);
        return orders.GetById(id) ?? throw new InvalidOperationException($"Order #{id} was not found.");
    }

    private static void PrintCart(Dictionary<int, int> cart, Dictionary<int, Product> catalog)
    {
        Console.WriteLine("\nCart:");
        var lines = cart.Select(kv => (Product: catalog[kv.Key], Quantity: kv.Value)).ToList();
        ConsoleUi.PrintTable(
            ["SKU", "Product", "Qty", "Unit Price", "Line Total"],
            lines.Select(l => new[]
            {
                l.Product.Sku, l.Product.Name, l.Quantity.ToString(),
                l.Product.Price.ToString("C"), (l.Product.Price * l.Quantity).ToString("C")
            }),
            2, 3, 4);
        Console.WriteLine($"Cart total: {lines.Sum(l => l.Product.Price * l.Quantity):C}");
    }
}
