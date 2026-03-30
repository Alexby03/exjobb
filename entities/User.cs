namespace App.Models;

public class User
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Email> InboxEmails { get; set; } = new List<Email>();
    public ICollection<Email> SentEmails { get; set; } = new List<Email>();
    public ICollection<Email> ReceivedEmails { get; set; } = new List<Email>();
    public ICollection<Customer> ManagedCustomers { get; set; } = new List<Customer>();
    public ICollection<CalendarEvent> OwnedCalendarEvents { get; set; } = new List<CalendarEvent>();
    public ICollection<CalendarEvent> OrganizedCalendarEvents { get; set; } = new List<CalendarEvent>();
    public ICollection<CalendarEventParticipant> EventParticipations { get; set; } = new List<CalendarEventParticipant>();
}
