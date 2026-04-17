// Lightweight DTO representing only the fields of generatedscenarios that we
// expose during testing. Everything else (category, expected outcome, expected
// tools, required permissions, rationale, setup data) is intentionally NOT
// loaded here because the testing agent must not see ground-truth labels.
public class ScenarioForTesting
{
    public int ScenarioId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public string? SetupDataJson { get; set; }
}
