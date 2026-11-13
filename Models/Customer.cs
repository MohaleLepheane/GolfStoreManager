namespace GolfStore.Models;

public record Customer(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone)
{
    public string FullName => $"{FirstName} {LastName}";
}
