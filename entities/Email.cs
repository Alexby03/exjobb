namespace App.Models;

public class Email
{
    public string EmailId { get; set; } = string.Empty;
    public string InboxOwnerId { get; set; } = string.Empty;
    public string? FromUserId { get; set; }
    public string? ToUserId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public User InboxOwner { get; set; } = null!;
    public User? FromUser { get; set; }
    public User? ToUser { get; set; }
}
