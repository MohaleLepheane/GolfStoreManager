using GolfStore.Data;
using GolfStore.Models;
using Microsoft.Data.Sqlite;

namespace GolfStore.Repositories;

public sealed class CustomerRepository(Database db)
{
    private const string SelectCustomers =
        "SELECT Id, FirstName, LastName, Email, Phone FROM Customers";

    // ---------- Read ----------

    public List<Customer> GetAll() =>
        Query($"{SelectCustomers} ORDER BY LastName, FirstName;");

    public Customer? GetById(int id) =>
        Query($"{SelectCustomers} WHERE Id = @id;", ("@id", id)).FirstOrDefault();

    public List<Customer> Search(string term) =>
        Query($"""
            {SelectCustomers}
            WHERE FirstName LIKE @term OR LastName LIKE @term OR Email LIKE @term OR Phone LIKE @term
            ORDER BY LastName, FirstName;
            """,
            ("@term", $"%{term}%"));

    // ---------- Create ----------

    public int Add(Customer customer)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command("""
            INSERT INTO Customers (FirstName, LastName, Email, Phone)
            VALUES (@first, @last, @email, @phone)
            RETURNING Id;
            """,
            ("@first", customer.FirstName),
            ("@last", customer.LastName),
            ("@email", customer.Email),
            ("@phone", customer.Phone));

        return Convert.ToInt32(command.ExecuteScalar());
    }

    // ---------- Update ----------

    public bool Update(Customer customer)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command("""
            UPDATE Customers
            SET FirstName = @first, LastName = @last, Email = @email, Phone = @phone
            WHERE Id = @id;
            """,
            ("@id", customer.Id),
            ("@first", customer.FirstName),
            ("@last", customer.LastName),
            ("@email", customer.Email),
            ("@phone", customer.Phone));

        return command.ExecuteNonQuery() == 1;
    }

    // ---------- Delete ----------

    /// <exception cref="SqliteException">Thrown with a constraint error if the customer has orders.</exception>
    public bool Delete(int id)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command("DELETE FROM Customers WHERE Id = @id;", ("@id", id));
        return command.ExecuteNonQuery() == 1;
    }

    // ---------- Helpers ----------

    private List<Customer> Query(string sql, params (string, object?)[] parameters)
    {
        using var connection = db.OpenConnection();
        using var command = connection.Command(sql, parameters);
        using var reader = command.ExecuteReader();

        var customers = new List<Customer>();
        while (reader.Read())
        {
            customers.Add(new Customer(
                Id: reader.GetInt32(0),
                FirstName: reader.GetString(1),
                LastName: reader.GetString(2),
                Email: reader.GetString(3),
                Phone: reader.IsDBNull(4) ? null : reader.GetString(4)));
        }
        return customers;
    }
}
