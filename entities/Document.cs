namespace App.Models;

public class Document
{
    public string DocumentId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? Content { get; set; }
    public string? FolderPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public User Owner { get; set; } = null!;
}
