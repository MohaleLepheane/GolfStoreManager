using GolfStore.Data;
using GolfStore.Models;
using GolfStore.Repositories;
using Microsoft.Data.Sqlite;

namespace GolfStore.UI;

public sealed class CustomerMenu(CustomerRepository customers, OrderRepository orders)
{
    public void Run()
    {
        while (true)
        {
            ConsoleUi.Header("Customers");
            Console.WriteLine("1) List all customers");
            Console.WriteLine("2) Search customers");
            Console.WriteLine("3) View customer & order history");
            Console.WriteLine("4) Add customer");
            Console.WriteLine("5) Edit customer");
            Console.WriteLine("6) Delete customer");
            Console.WriteLine("0) Back");

            switch (ConsoleUi.ReadMenuChoice())
            {
                case "1": ConsoleUi.Execute(() => PrintCustomers(customers.GetAll())); break;
                case "2": ConsoleUi.Execute(Search); break;
                case "3": ConsoleUi.Execute(View); break;
                case "4": ConsoleUi.Execute(Add); break;
                case "5": ConsoleUi.Execute(Edit); break;
                case "6": ConsoleUi.Execute(Delete); break;
                case "0": return;
                default: ConsoleUi.Execute(() => ConsoleUi.Error("Unknown option.")); break;
            }
        }
    }

    public static void PrintCustomers(IEnumerable<Customer> list) =>
        ConsoleUi.PrintTable(
            ["ID", "Name", "Email", "Phone"],
            list.Select(c => new[] { c.Id.ToString(), c.FullName, c.Email, c.Phone ?? "" }),
            0);

    private void Search()
    {
        var term = ConsoleUi.ReadString("Search by name, email or phone");
        PrintCustomers(customers.Search(term));
    }

    private void View()
    {
        var customer = Pick();
        Console.WriteLine();
        Console.WriteLine($"Name:  {customer.FullName}");
        Console.WriteLine($"Email: {customer.Email}");
        Console.WriteLine($"Phone: {customer.Phone ?? "(none)"}");
        Console.WriteLine();
        OrderMenu.PrintOrders(orders.GetByCustomer(customer.Id));
    }

    private void Add()
    {
        var firstName = ConsoleUi.ReadString("First name");
        var lastName = ConsoleUi.ReadString("Last name");
        var email = ReadEmail();
        var phone = ConsoleUi.ReadString("Phone (optional)", required: false);
        var customer = new Customer(0, firstName, lastName, email, phone.Length == 0 ? null : phone);

        var id = customers.Add(customer);
        ConsoleUi.Success($"Added customer #{id}: {customer.FullName}.");
    }

    private void Edit()
    {
        var existing = Pick();
        Console.WriteLine("Press Enter to keep the current value.");

        var updated = existing with
        {
            FirstName = ConsoleUi.ReadString("First name", existing.FirstName),
            LastName = ConsoleUi.ReadString("Last name", existing.LastName),
            Email = ReadEmail(existing.Email),
            Phone = ConsoleUi.ReadString("Phone (optional)", existing.Phone, required: false) is { Length: > 0 } p ? p : null
        };

        customers.Update(updated);
        ConsoleUi.Success($"Customer #{updated.Id} updated.");
    }

    private void Delete()
    {
        var customer = Pick();
        if (!ConsoleUi.Confirm($"Delete {customer.FullName}?")) return;

        try
        {
            customers.Delete(customer.Id);
            ConsoleUi.Success("Customer deleted.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteExtensions.ConstraintViolation)
        {
            ConsoleUi.Error("This customer has orders on file, so they can't be deleted. " +
                            "Delete their orders first.");
        }
    }

    private Customer Pick()
    {
        var id = ConsoleUi.ReadInt("Customer ID", min: 1);
        return customers.GetById(id) ?? throw new InvalidOperationException($"Customer #{id} was not found.");
    }

    private static string ReadEmail(string? current = null)
    {
        while (true)
        {
            var email = ConsoleUi.ReadString("Email", current);
            if (email.Contains('@') && email.Contains('.')) return email.ToLowerInvariant();
            ConsoleUi.Error("That doesn't look like an email address.");
        }
    }
}
