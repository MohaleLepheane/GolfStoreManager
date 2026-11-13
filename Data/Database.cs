using Microsoft.Data.Sqlite;

namespace GolfStore.Data;

/// <summary>Opens connections to the SQLite database and creates the schema on first run.</summary>
public sealed class Database
{
    private readonly string _connectionString;

    public Database(string filePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = filePath,
            ForeignKeys = true // SQLite only enforces REFERENCES constraints when this is on
        }.ToString();
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>Creates the tables if needed and loads sample data into an empty database.</summary>
    public void Initialize()
    {
        using var connection = OpenConnection();

        using (var schema = connection.Command(ReadSqlFile("schema.sql")))
        {
            schema.ExecuteNonQuery();
        }

        using var count = connection.Command("SELECT COUNT(*) FROM Products;");
        if (Convert.ToInt64(count.ExecuteScalar()) == 0)
        {
            using var seed = connection.Command(ReadSqlFile("seed.sql"));
            seed.ExecuteNonQuery();
        }
    }

    private static string ReadSqlFile(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Sql", fileName));
}
