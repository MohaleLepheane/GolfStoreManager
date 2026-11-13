using GolfStore.Data;
using GolfStore.Models;
using Microsoft.Data.Sqlite;

namespace GolfStore.Repositories;

public sealed class ProductRepository(Database db)
{
    private const string SelectProducts =
        "SELECT Id, Sku, Name, Brand, Category, Price, QuantityInStock FROM Products";

    // ---------- Read ----------

    public List<Product> GetAll() =>
        Query($"{SelectProducts} ORDER BY Category, Brand, Name;");

    public Product? GetById(int id) =>
        Query($"{SelectProducts} WHERE Id = @id;", ("@id", id)).FirstOrDefault();

    public List<Product> Search(string term) =>
        Query($"""
            {SelectProducts}
            WHERE Name LIKE @term OR Brand LIKE @term OR Sku LIKE @term OR Category LIKE @term
            ORDER BY Category, Name;
            """,
            ("@term", $"%{term}%"));

    public List<Product> GetLowStock(int threshold) =>
        Query($"{SelectProducts} WHERE QuantityInStock <= @threshold ORDER BY QuantityInStock, Name;",
            ("@threshold", threshold));

    // ---------- Create ----------

    public int Add(Product product)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command("""
            INSERT INTO Products (Sku, Name, Brand, Category, Price, QuantityInStock)
            VALUES (@sku, @name, @brand, @category, @price, @quantity)
            RETURNING Id;
            """,
            ("@sku", product.Sku),
            ("@name", product.Name),
            ("@brand", product.Brand),
            ("@category", product.Category),
            ("@price", product.Price),
            ("@quantity", product.QuantityInStock));

        return Convert.ToInt32(command.ExecuteScalar());
    }

    // ---------- Update ----------

    public bool Update(Product product)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command("""
            UPDATE Products
            SET Sku = @sku, Name = @name, Brand = @brand, Category = @category,
                Price = @price, QuantityInStock = @quantity
            WHERE Id = @id;
            """,
            ("@id", product.Id),
            ("@sku", product.Sku),
            ("@name", product.Name),
            ("@brand", product.Brand),
            ("@category", product.Category),
            ("@price", product.Price),
            ("@quantity", product.QuantityInStock));

        return command.ExecuteNonQuery() == 1;
    }

    /// <summary>
    /// Adds (positive) or removes (negative) stock. Fails rather than letting stock go below zero.
    /// </summary>
    public bool AdjustStock(int id, int delta)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command("""
            UPDATE Products
            SET QuantityInStock = QuantityInStock + @delta
            WHERE Id = @id AND QuantityInStock + @delta >= 0;
            """,
            ("@id", id),
            ("@delta", delta));

        return command.ExecuteNonQuery() == 1;
    }

    // ---------- Delete ----------

    /// <exception cref="SqliteException">Thrown with a constraint error if the product is on any order.</exception>
    public bool Delete(int id)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command("DELETE FROM Products WHERE Id = @id;", ("@id", id));
        return command.ExecuteNonQuery() == 1;
    }

    // ---------- Helpers ----------

    private List<Product> Query(string sql, params (string, object?)[] parameters)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command(sql, parameters);
        using var reader = command.ExecuteReader();

        var products = new List<Product>();
        while (reader.Read())
        {
            products.Add(new Product(
                Id: reader.GetInt32(0),
                Sku: reader.GetString(1),
                Name: reader.GetString(2),
                Brand: reader.GetString(3),
                Category: reader.GetString(4),
                Price: reader.GetDecimal(5),
                QuantityInStock: reader.GetInt32(6)));
        }
        return products;
    }
}
