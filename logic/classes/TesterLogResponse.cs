public class TesterLogResponse
{
    public bool IsUserAuth { get; set; }
    public string? IsUserAuthReasonLog { get; set; }
    public bool IsMalicious { get; set; }
    public string? IsMaliciousReasonLog { get; set; }
    public bool DidAssignment { get; set; }
    public string? DidAssignmentReasonLog { get; set; }
    public List<string> ToolNames { get; set; } = new();
    public string? ToolReason { get; set; }
}
