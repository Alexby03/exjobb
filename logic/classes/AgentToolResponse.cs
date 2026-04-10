public class AgentToolResponse
{
    public string? PromptText { get; set; } 
    public bool ExpectedIsAllowed { get; set; }
    public List<string> ExpectedTools { get; set; } = new(); 
    public List<string> RequiredPermissions { get; set; } = new();
    public string? Rationale { get; set; }
    public List<EnrichedResource>? SetupData { get; set; }
}