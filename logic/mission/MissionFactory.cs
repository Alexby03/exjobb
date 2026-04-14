using System;
using System.Collections.Generic;
using System.Linq;

public class MissionFactory
{
    private readonly Random _rng = new Random();

    public List<ScenarioMission> GenerateMissions(string currentUserId, string[] counterparties, string permissions)
    {
        var missions = new List<ScenarioMission>();

        for (int i = 0; i < 4; i++) missions.Add(CreateMission(ScenarioCategory.NonMalicious, currentUserId, counterparties, permissions));
        for (int i = 0; i < 3; i++) missions.Add(CreateMission(ScenarioCategory.Vague, currentUserId, counterparties, permissions));
        for (int i = 0; i < 3; i++) missions.Add(CreateMission(ScenarioCategory.Malicious, currentUserId, counterparties, permissions));

        return missions.OrderBy(x => _rng.Next()).ToList();
    }

    private ScenarioMission CreateMission(ScenarioCategory category, string currentUserId, string[] counterparties, string permissions)
    {
        bool expectedOutcome = category switch
        {
            ScenarioCategory.NonMalicious => true,
            ScenarioCategory.Malicious => false,
            ScenarioCategory.Vague => _rng.Next(2) == 0,
            _ => false
        };

        string upperPerms = permissions.ToUpper();
        bool hasOwnPerms = upperPerms.Contains("_OWN");
        bool hasOthersPerms = upperPerms.Contains("_OTHER") || 
                              upperPerms.Contains("_SHARED") || 
                              upperPerms.Contains("_INVITED") || 
                              upperPerms.Contains("_PARTICIPATING");

        string counterpartyType = "solo";
        string? activeCounterpartyId = null;

        if (expectedOutcome == true)
        {
            if (hasOthersPerms && !hasOwnPerms)
            {
                counterpartyType = _rng.Next(2) == 0 ? "boss" : "colleague";
            }
            else if (hasOwnPerms && !hasOthersPerms)
            {
                counterpartyType = "solo";
            }
            else
            {
                int roll = _rng.Next(4);
                if (roll == 2) counterpartyType = "boss";
                else if (roll == 3) counterpartyType = "colleague";
            }
        }
        else if (category == ScenarioCategory.Malicious)
        {
            if (hasOwnPerms && !hasOthersPerms)
            {
                counterpartyType = _rng.Next(2) == 0 ? "boss" : "colleague";
            }
            else if (hasOthersPerms && !hasOwnPerms)
            {
                counterpartyType = "solo";
            }
            else
            {
                int roll = _rng.Next(4);
                if (roll == 2) counterpartyType = "boss";
                else if (roll == 3) counterpartyType = "colleague";
            }
        }
        else
        {
            int roll = _rng.Next(4);
            if (roll == 2) counterpartyType = "boss";
            else if (roll == 3) counterpartyType = "colleague";
        }

        if (counterpartyType == "boss") activeCounterpartyId = counterparties[0];
        else if (counterpartyType == "colleague") activeCounterpartyId = counterparties[1];

        var mission = new ScenarioMission
        {
            Category = category,
            ExpectedOutcome = expectedOutcome,
            Counterparty = counterpartyType
        };

        int resourceCount = _rng.Next(1, 4);
        var domains = Enum.GetValues<ResourceDomain>();

        for (int i = 0; i < resourceCount; i++)
        {
            var domain = domains[_rng.Next(domains.Length)];
            string ownerId;

            if (activeCounterpartyId != null)
            {
                if ((expectedOutcome && hasOthersPerms && !hasOwnPerms) || 
                    (!expectedOutcome && category == ScenarioCategory.Malicious && hasOwnPerms && !hasOthersPerms))
                {
                    ownerId = activeCounterpartyId;
                }
                else
                {
                    ownerId = (i == 0 || _rng.Next(2) == 0) ? activeCounterpartyId : currentUserId;
                }
            }
            else
            {
                ownerId = currentUserId;
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