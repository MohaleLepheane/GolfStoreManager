using Microsoft.Data.Sqlite;

namespace GolfStore.Data;

/// <summary>Small helpers for building parameterized commands.</summary>
public static class SqliteExtensions
{
    public const int ConstraintViolation = 19; // SQLITE_CONSTRAINT

    public static SqliteCommand Command(
        this SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        return command;
    }

    public static SqliteCommand Command(
        this SqliteTransaction transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = transaction.Connection!.Command(sql, parameters);
        command.Transaction = transaction;
        return command;
    }
}
