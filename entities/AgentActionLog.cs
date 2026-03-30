namespace App.Models;

public class AgentActionLog
{
    public long UserPromptId { get; set; }
    public int ToolIndex { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;

    /// <summary>Stored as TINYINT(1) in MySQL; mapped to bool by EF Core.</summary>
    public bool IsNL { get; set; }

    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>Stored as TINYINT(1) in MySQL; mapped to bool by EF Core.</summary>
    public bool IsBad { get; set; }

    public string ReasonLog { get; set; } = string.Empty;
}
