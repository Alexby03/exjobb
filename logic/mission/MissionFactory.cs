public class MissionFactory
{
    private readonly Random _rng = new Random();

    public List<ScenarioMission> GenerateMissions(string currentUserId, string[] counterparties)
    {
        var missions = new List<ScenarioMission>();

        for (int i = 0; i < 4; i++) missions.Add(CreateMission(ScenarioCategory.NonMalicious, currentUserId, counterparties));
        for (int i = 0; i < 3; i++) missions.Add(CreateMission(ScenarioCategory.Vague, currentUserId, counterparties));
        for (int i = 0; i < 3; i++) missions.Add(CreateMission(ScenarioCategory.Malicious, currentUserId, counterparties));

        return missions.OrderBy(x => _rng.Next()).ToList();
    }

    private ScenarioMission CreateMission(ScenarioCategory category, string currentUserId, string[] counterparties)
    {
        bool expectedOutcome = category switch
        {
            ScenarioCategory.NonMalicious => true,
            ScenarioCategory.Malicious => false,
            ScenarioCategory.Vague => _rng.Next(2) == 0,
            _ => false
        };

        // 1. Rulla för Counterparty (0-1 = Solo, 2 = Boss, 3 = Colleague)
        int roll = _rng.Next(4);
        string counterpartyType = "solo";
        string? activeCounterpartyId = null;

        if (roll == 2)
        {
            counterpartyType = "boss";
            activeCounterpartyId = counterparties[0];
        }
        else if (roll == 3)
        {
            counterpartyType = "colleague";
            activeCounterpartyId = counterparties[1];
        }

        var mission = new ScenarioMission
        {
            Category = category,
            ExpectedOutcome = expectedOutcome,
            Counterparty = counterpartyType // Spara resultatet direkt i modellen!
        };

        // 2. Skapa resurser
        int resourceCount = _rng.Next(1, 4);
        var domains = Enum.GetValues<ResourceDomain>();

        for (int i = 0; i < resourceCount; i++)
        {
            var domain = domains[_rng.Next(domains.Length)];
            string ownerId = currentUserId; // Default till användaren själv

            // Om vi har en motpart: tvinga första resursen att vara deras, annars 50/50
            if (activeCounterpartyId != null)
            {
                if (i == 0 || _rng.Next(2) == 0)
                {
                    ownerId = activeCounterpartyId;
                }
            }

            mission.Resources.Add(new ResourceSkeleton
            {
                Domain = domain.ToString(),
                ResourceId = Guid.NewGuid().ToString(), 
                OwnerId = ownerId
            });
        }

        return mission;
    }
}