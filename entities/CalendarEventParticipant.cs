namespace App.Models;

public class CalendarEventParticipant
{
    public string ParticipantId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    /// <summary>Values: pending | accepted | declined</summary>
    public string ResponseStatus { get; set; } = "pending";

    // Navigation properties
    public CalendarEvent Event { get; set; } = null!;
    public User User { get; set; } = null!;
}
