public class ScenarioMission
{
    public ScenarioCategory Category { get; set; }
    public bool ExpectedOutcome { get; set; }
    public string Counterparty { get; set; } = "solo";
    public List<ResourceSkeleton> Resources { get; set; } = new();
}