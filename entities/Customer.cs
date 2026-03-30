namespace App.Models;

public class Customer
{
    public string CustomerId { get; set; } = string.Empty;
    public string? ManagedByUserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>Values: standard | premium | enterprise</summary>
    public string AccountType { get; set; } = string.Empty;

    /// <summary>Values: active | suspended | cancelled</summary>
    public string SubscriptionStatus { get; set; } = "active";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User? ManagedBy { get; set; }
}
