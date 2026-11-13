using GolfStore.Data;
using GolfStore.Models;
using Microsoft.Data.Sqlite;

namespace GolfStore.Repositories;

public sealed class OrderRepository(Database db)
{
    // Order headers joined with the customer's name and rolled-up totals from the line items.
    private const string SelectOrderSummaries = """
        SELECT o.Id,
               o.CustomerId,
               c.FirstName || ' ' || c.LastName AS CustomerName,
               o.OrderDate,
               o.Status,
               COALESCE(SUM(oi.Quantity), 0)                AS ItemCount,
               COALESCE(SUM(oi.Quantity * oi.UnitPrice), 0) AS Total
        FROM Orders o
        JOIN Customers c       ON c.Id = o.CustomerId
        LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
        """;

    private const string GroupOrderSummaries = """
        GROUP BY o.Id, o.CustomerId, c.FirstName, c.LastName, o.OrderDate, o.Status
        ORDER BY o.OrderDate DESC, o.Id DESC;
        """;

    // ---------- Read ----------

    public List<OrderSummary> GetAll() =>
        QuerySummaries($"{SelectOrderSummaries}\n{GroupOrderSummaries}");

    public List<OrderSummary> GetByCustomer(int customerId) =>
        QuerySummaries($"{SelectOrderSummaries}\nWHERE o.CustomerId = @customerId\n{GroupOrderSummaries}",
            ("@customerId", customerId));

    public OrderSummary? GetById(int id) =>
        QuerySummaries($"{SelectOrderSummaries}\nWHERE o.Id = @id\n{GroupOrderSummaries}",
            ("@id", id)).FirstOrDefault();

    public List<OrderLine> GetLines(int orderId)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command("""
            SELECT oi.ProductId, p.Sku, p.Name, oi.Quantity, oi.UnitPrice
            FROM OrderItems oi
            JOIN Products p ON p.Id = oi.ProductId
            WHERE oi.OrderId = @orderId
            ORDER BY oi.Id;
            """,
            ("@orderId", orderId));
        using var reader = command.ExecuteReader();

        var lines = new List<OrderLine>();
        while (reader.Read())
        {
            lines.Add(new OrderLine(
                ProductId: reader.GetInt32(0),
                Sku: reader.GetString(1),
                ProductName: reader.GetString(2),
                Quantity: reader.GetInt32(3),
                UnitPrice: reader.GetDecimal(4)));
        }
        return lines;
    }

    public List<CategorySales> GetSalesByCategory()
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command("""
            SELECT p.Category,
                   SUM(oi.Quantity)                AS UnitsSold,
                   SUM(oi.Quantity * oi.UnitPrice) AS Revenue
            FROM OrderItems oi
            JOIN Orders o   ON o.Id = oi.OrderId
            JOIN Products p ON p.Id = oi.ProductId
            WHERE o.Status <> 'Cancelled'
            GROUP BY p.Category
            ORDER BY Revenue DESC;
            """);
        using var reader = command.ExecuteReader();

        var rows = new List<CategorySales>();
        while (reader.Read())
        {
            rows.Add(new CategorySales(reader.GetString(0), reader.GetInt32(1), reader.GetDecimal(2)));
        }
        return rows;
    }

    // ---------- Create ----------

    /// <summary>
    /// Creates an order and takes the items out of inventory in a single transaction.
    /// If any item is short on stock, nothing is saved.
    /// </summary>
    public int Create(int customerId, IReadOnlyList<CartItem> items)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("An order must contain at least one item.");

        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();

        int orderId;
        using (var insertOrder = transaction.Command(
            "INSERT INTO Orders (CustomerId) VALUES (@customerId) RETURNING Id;",
            ("@customerId", customerId)))
        {
            orderId = Convert.ToInt32(insertOrder.ExecuteScalar());
        }

        foreach (var item in items)
        {
            // Reserve stock first; the WHERE clause makes this a no-op if there isn't enough.
            using (var reserve = transaction.Command("""
                UPDATE Products
                SET QuantityInStock = QuantityInStock - @quantity
                WHERE Id = @productId AND QuantityInStock >= @quantity;
                """,
                ("@productId", item.ProductId),
                ("@quantity", item.Quantity)))
            {
                if (reserve.ExecuteNonQuery() == 0)
                    throw new InvalidOperationException(
                        $"Not enough stock for product #{item.ProductId}. The order was not saved.");
            }

            // Copy the current price onto the line so later price changes don't rewrite history.
            using var insertLine = transaction.Command("""
                INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
                SELECT @orderId, Id, @quantity, Price FROM Products WHERE Id = @productId;
                """,
                ("@orderId", orderId),
                ("@productId", item.ProductId),
                ("@quantity", item.Quantity));
            insertLine.ExecuteNonQuery();
        }

        transaction.Commit(); // Disposing without committing rolls everything back.
        return orderId;
    }

    // ---------- Update ----------

    public void UpdateStatus(int orderId, OrderStatus newStatus)
    {
        if (newStatus == OrderStatus.Cancelled)
            throw new InvalidOperationException("Use Cancel so that stock is returned to inventory.");

        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();

        var current = GetStatus(transaction, orderId)
            ?? throw new InvalidOperationException($"Order #{orderId} was not found.");
        if (current == OrderStatus.Cancelled)
            throw new InvalidOperationException("A cancelled order can't change status.");

        using var update = transaction.Command(
            "UPDATE Orders SET Status = @status WHERE Id = @id;",
            ("@id", orderId),
            ("@status", newStatus.ToString()));
        update.ExecuteNonQuery();

        transaction.Commit();
    }

    /// <summary>Cancels a pending order and puts its items back into inventory.</summary>
    public void Cancel(int orderId)
    {
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();

        var current = GetStatus(transaction, orderId)
            ?? throw new InvalidOperationException($"Order #{orderId} was not found.");
        if (current != OrderStatus.Pending)
            throw new InvalidOperationException(
                $"Only pending orders can be cancelled. Order #{orderId} is {current}.");

        RestoreStock(transaction, orderId);

        using var update = transaction.Command(
            "UPDATE Orders SET Status = 'Cancelled' WHERE Id = @id;", ("@id", orderId));
        update.ExecuteNonQuery();

        transaction.Commit();
    }

    // ---------- Delete ----------

    /// <summary>
    /// Deletes an order and its lines. A pending order's items go back into inventory first;
    /// shipped or delivered items have already left the store, so stock is left alone.
    /// </summary>
    public void Delete(int orderId)
    {
        using var connection = db.OpenConnection();
        using var transaction = connection.BeginTransaction();

        var current = GetStatus(transaction, orderId)
            ?? throw new InvalidOperationException($"Order #{orderId} was not found.");
        if (current == OrderStatus.Pending)
            RestoreStock(transaction, orderId);

        // OrderItems rows are removed by ON DELETE CASCADE.
        using var delete = transaction.Command("DELETE FROM Orders WHERE Id = @id;", ("@id", orderId));
        delete.ExecuteNonQuery();

        transaction.Commit();
    }

    // ---------- Helpers ----------

    private static OrderStatus? GetStatus(SqliteTransaction transaction, int orderId)
    {
        using var command = transaction.Command("SELECT Status FROM Orders WHERE Id = @id;", ("@id", orderId));
        return command.ExecuteScalar() is string status ? Enum.Parse<OrderStatus>(status) : null;
    }

    private static void RestoreStock(SqliteTransaction transaction, int orderId)
    {
        using var command = transaction.Command("""
            UPDATE Products
            SET QuantityInStock = QuantityInStock + (
                SELECT SUM(oi.Quantity) FROM OrderItems oi
                WHERE oi.OrderId = @orderId AND oi.ProductId = Products.Id)
            WHERE Id IN (SELECT ProductId FROM OrderItems WHERE OrderId = @orderId);
            """,
            ("@orderId", orderId));
        command.ExecuteNonQuery();
    }

    private List<OrderSummary> QuerySummaries(string sql, params (string, object?)[] parameters)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command(sql, parameters);
        using var reader = command.ExecuteReader();

        var orders = new List<OrderSummary>();
        while (reader.Read())
        {
            orders.Add(new OrderSummary(
                Id: reader.GetInt32(0),
                CustomerId: reader.GetInt32(1),
                CustomerName: reader.GetString(2),
                OrderDate: reader.GetDateTime(3),
                Status: Enum.Parse<OrderStatus>(reader.GetString(4)),
                ItemCount: reader.GetInt32(5),
                Total: reader.GetDecimal(6)));
        }
        return orders;
    }
}
