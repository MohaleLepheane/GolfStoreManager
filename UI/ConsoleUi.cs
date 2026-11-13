using GolfStore.Data;
using Microsoft.Data.Sqlite;

namespace GolfStore.UI;

/// <summary>Reusable console input/output helpers.</summary>
public static class ConsoleUi
{
    public static void Header(string title)
    {
        try { Console.Clear(); } catch (IOException) { /* output is redirected */ }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(title);
        Console.WriteLine(new string('=', title.Length));
        Console.ResetColor();
    }

    public static string ReadMenuChoice()
    {
        Console.Write("\nChoose an option: ");
        return ReadLine();
    }

    /// <summary>Reads text. When <paramref name="current"/> is given, pressing Enter keeps it.</summary>
    public static string ReadString(string prompt, string? current = null, bool required = true)
    {
        while (true)
        {
            Console.Write(current is null ? $"{prompt}: " : $"{prompt} [{current}]: ");
            var input = ReadLine();
            if (input.Length > 0) return input;
            if (current is not null) return current;
            if (!required) return "";
            Error("A value is required.");
        }
    }

    public static int ReadInt(string prompt, int min = int.MinValue, int max = int.MaxValue, int? current = null)
    {
        while (true)
        {
            var input = ReadString(prompt, current?.ToString());
            if (int.TryParse(input, out var value) && value >= min && value <= max) return value;
            Error((min, max) switch
            {
                (int.MinValue, int.MaxValue) => "Enter a whole number.",
                (_, int.MaxValue) => $"Enter a whole number of at least {min}.",
                _ => $"Enter a whole number from {min} to {max}."
            });
        }
    }

    public static decimal ReadDecimal(string prompt, decimal min = 0m, decimal? current = null)
    {
        while (true)
        {
            var input = ReadString(prompt, current?.ToString("0.00")).TrimStart('$');
            if (decimal.TryParse(input, out var value) && value >= min) return Math.Round(value, 2);
            Error($"Enter an amount of at least {min:C}.");
        }
    }

    public static bool Confirm(string prompt)
    {
        Console.Write($"{prompt} (y/n): ");
        var answer = ReadLine().ToLowerInvariant();
        return answer is "y" or "yes";
    }

    /// <summary>Runs a menu action, reports expected errors in a friendly way, then waits for Enter.</summary>
    public static void Execute(Action action)
    {
        Console.WriteLine();
        try
        {
            action();
        }
        catch (InvalidOperationException ex)
        {
            Error(ex.Message);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteExtensions.ConstraintViolation)
        {
            Error($"The database rejected that change: {ex.Message}");
        }
        Pause();
    }

    public static void Pause()
    {
        Console.Write("\nPress Enter to continue...");
        ReadLine();
    }

    public static void Success(string message) => WriteColored(message, ConsoleColor.Green);
    public static void Warning(string message) => WriteColored(message, ConsoleColor.Yellow);
    public static void Error(string message) => WriteColored(message, ConsoleColor.Red);

    /// <summary>Prints rows as an aligned text table. Columns listed in rightAligned are right-justified.</summary>
    public static void PrintTable(string[] headers, IEnumerable<string[]> rows, params int[] rightAligned)
    {
        var data = rows.ToList();
        if (data.Count == 0)
        {
            Warning("(no records)");
            return;
        }

        var widths = headers
            .Select((header, i) => Math.Max(header.Length, data.Max(row => row[i].Length)))
            .ToArray();

        string Format(string[] cells) => string.Join("  ", cells.Select((cell, i) =>
            rightAligned.Contains(i) ? cell.PadLeft(widths[i]) : cell.PadRight(widths[i])));

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(Format(headers));
        Console.WriteLine(string.Join("  ", widths.Select(w => new string('-', w))));
        Console.ResetColor();
        foreach (var row in data) Console.WriteLine(Format(row));
    }

    private static void WriteColored(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static string ReadLine()
    {
        var line = Console.ReadLine();
        if (line is null) Environment.Exit(0); // input stream closed
        return line.Trim().Trim('﻿'); // drop a byte-order mark if input is piped in
    }
}
