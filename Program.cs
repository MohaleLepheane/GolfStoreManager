using System.Globalization;
using GolfStore.Data;
using GolfStore.Repositories;
using GolfStore.UI;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US"); // show prices in dollars

var db = new Database(Path.Combine(AppContext.BaseDirectory, "golfstore.db"));
db.Initialize();

var products = new ProductRepository(db);
var customers = new CustomerRepository(db);
var orders = new OrderRepository(db);

var productMenu = new ProductMenu(products);
var customerMenu = new CustomerMenu(customers, orders);
var orderMenu = new OrderMenu(orders, customers, products);

while (true)
{
    ConsoleUi.Header("Golf Store Manager - Inventory & Orders");
    Console.WriteLine("1) Products & inventory");
    Console.WriteLine("2) Customers");
    Console.WriteLine("3) Orders");
    Console.WriteLine("4) Low-stock report");
    Console.WriteLine("5) Sales by category");
    Console.WriteLine("0) Exit");

    switch (ConsoleUi.ReadMenuChoice())
    {
        case "1": productMenu.Run(); break;
        case "2": customerMenu.Run(); break;
        case "3": orderMenu.Run(); break;
        case "4":
            ConsoleUi.Execute(() =>
            {
                var threshold = ConsoleUi.ReadInt("Show products with stock at or below", min: 0, current: 5);
                ProductMenu.PrintProducts(products.GetLowStock(threshold));
            });
            break;
        case "5":
            ConsoleUi.Execute(() =>
            {
                var rows = orders.GetSalesByCategory();
                ConsoleUi.PrintTable(
                    ["Category", "Units Sold", "Revenue"],
                    rows.Select(r => new[] { r.Category, r.UnitsSold.ToString(), r.Revenue.ToString("C") }),
                    1, 2);
                Console.WriteLine($"\nTotal revenue (excluding cancelled orders): {rows.Sum(r => r.Revenue):C}");
            });
            break;
        case "0": return;
        default: ConsoleUi.Execute(() => ConsoleUi.Error("Unknown option.")); break;
    }
}
