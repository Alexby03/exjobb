namespace App.Models;

public class CalendarEvent
{
    public string EventId { get; set; } = string.Empty;
    public string CalendarOwnerId { get; set; } = string.Empty;
    public string? OrganizerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public User CalendarOwner { get; set; } = null!;
    public User? Organizer { get; set; }
    public ICollection<CalendarEventParticipant> Participants { get; set; } = new List<CalendarEventParticipant>();
}
