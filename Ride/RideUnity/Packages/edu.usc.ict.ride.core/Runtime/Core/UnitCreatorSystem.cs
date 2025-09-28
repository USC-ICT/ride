using UnityEngine;
using Ride.Scenario;

namespace Ride.Entities
{
    /// <summary>
    /// Creates groups of agents and groups them into Military units (squads, platoons, etc)
    /// </summary>
    public class UnitCreatorSystem : RideSystemMonoBehaviour, IUnitCreatorSystem
    {
        const int FireTeamSize = 4;
        const int SquadSeparation = 3;

        IScenarioSystem scenarioSystem;
        IAgentSystem agentSystem;
        IGroupSystem groupSystem;

        public override void SystemInit()
        {
            base.SystemInit();

            scenarioSystem = Globals.api.scenarioSystem;
            agentSystem = Globals.api.agentSystem;
            groupSystem = Globals.api.groupSystem;
        }

        RideVector3 CalculateStartPosition(UnitCreationParams p)
        {
            RideVector3 startPos = p.position;
            int HalfTeam = p.rows * p.cols / 2;
            startPos.x -= HalfTeam / p.cols * p.memberSeparation;
            startPos.z += HalfTeam / p.rows * p.memberSeparation;
            return startPos;
        }

        public RideID CreateFireTeam(UnitCreationParams p)
        {
            RideVector3 startPos = CalculateStartPosition(p);

            int numAgents = p.rows * p.cols;

            RideID[] agents = new RideID[numAgents];

            for (int i = 0; i < p.cols; i++)
            {
                for (int j = 0; j < p.rows; j++)
                {
                    float xRandomMemberPlacement = Random.Range(0f, 5f); // generate randomness in x position of soldier placement
                    float zRandomMemberPlacement = Random.Range(0f, 5f); // generate randomness in z position of soldier placement

                    agents[i * p.rows + j] = agentSystem.AddAgent(new Unit(p.team, new RideVector3(startPos.x + i * xRandomMemberPlacement, startPos.y, startPos.z - j * zRandomMemberPlacement), p.prefab) { name = p.name });
                    agentSystem.SetAgentRotation(agents[i * p.rows + j], p.rotation);
                }
            }

            RideID fireteam = groupSystem.CreateGroup(p.name + " Fireteam", agents, null);
            groupSystem.AddMember(fireteam, agents[0], 10, RideDefines.TeamLeader);
            groupSystem.AddMember(fireteam, agents[1], 1, RideDefines.Rifleman);
            groupSystem.AddMember(fireteam, agents[2], 1, RideDefines.Grenadier);
            groupSystem.AddMember(fireteam, agents[3], 1, RideDefines.AutomaticRifleman);
            return fireteam;
        }

        public RideID CreateCustomGroup(UnitCreationParams p)
        {
            RideVector3 startPos = CalculateStartPosition(p);

            int numAgents = p.rows * p.cols;
            if (numAgents != p.numAgents) // override number of agents
                numAgents = p.numAgents;

            RideID[] agents = new RideID[numAgents];
            for (int idx = 0; idx < p.numAgents; idx++)
            {
                int i = idx / p.rows;
                int j = idx % p.cols;

                float xRandomMemberPlacement = Random.Range(0f, 5f); // generate randomness in x position of soldier placement
                float zRandomMemberPlacement = Random.Range(0f, 5f); // generate randomness in z position of soldier placement

                agents[idx] = agentSystem.AddAgent(new Unit(p.team, new RideVector3(startPos.x + i * xRandomMemberPlacement, startPos.y, startPos.z - j * zRandomMemberPlacement), p.prefab));
                agentSystem.SetAgentRotation(agents[i * p.rows + j], p.rotation);
            }

            RideID group = groupSystem.CreateGroup(p.name, agents, null);
            for (int i = 0; i < numAgents; i++)
            {
                if (i == 0)
                {
                    groupSystem.AddMember(group, agents[i], 10, RideDefines.TeamLeader);
                }
                else
                {
                    groupSystem.AddMember(group, agents[i], 1, RideDefines.Rifleman);
                }
            }
            return group;
        }

        public RideID CreateSquad(UnitCreationParams p)
        {
            RideVector3 startPos = CalculateStartPosition(p);
            RideID squad = groupSystem.CreateGroup(p.name + " Squad");
            RideID squadLeader = agentSystem.AddAgent(new Unit(p.team, new RideVector3(startPos.x, startPos.y, startPos.z), p.prefab) { name = p.name + " Squad Leader" });
            groupSystem.AddMember(squad, squadLeader, 20, RideDefines.SquadLeader);

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    UnitCreationParams subgroupParams = new UnitCreationParams(p.name);// string.Format("{0} - {1} {2}", p.name, "Fireteam", i * p.cols + j));
                    subgroupParams.rows = subgroupParams.cols = 2;
                    subgroupParams.memberSeparation = p.memberSeparation;
                    subgroupParams.position = new RideVector3(startPos.x + i * p.memberSeparation, startPos.y, startPos.z - j * p.memberSeparation);
                    subgroupParams.rotation = p.rotation;
                    subgroupParams.team = p.team;
                    subgroupParams.prefab = p.prefab;
                    RideID fireteam = CreateFireTeam(subgroupParams);
                    groupSystem.AddSubgroup(squad, fireteam);
                }
            }

            return squad;
        }

        public RideID CreatePlatoon(UnitCreationParams p)
        {
            RideVector3 startPos = CalculateStartPosition(p);
            RideID platoon = groupSystem.CreateGroup(p.name + " Platoon");
            RideID platoonLeader = agentSystem.AddAgent(new Unit(p.team, new RideVector3(startPos.x, startPos.y, startPos.z), p.prefab) { name = p.name + " Platoon Leader" });
            groupSystem.AddMember(platoon, platoonLeader, 40, RideDefines.PlatoonLeader);

            //NOTE: for now, platoons will consist of 4 squads and a platoon leader.
            //TODO: add in flexibility for platoon size (e.g. 3 squads instead of 4)
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    UnitCreationParams subgroupParams = new UnitCreationParams(p.name);// string.Format("{0} - {1} {2}", p.name, "Squad", i * p.cols + j));
                    subgroupParams.rows = subgroupParams.cols = 2; //Note
                    subgroupParams.memberSeparation = p.memberSeparation;
                    subgroupParams.position = new RideVector3(startPos.x + i * p.memberSeparation, startPos.y, startPos.z - j * p.memberSeparation);
                    subgroupParams.rotation = p.rotation;
                    subgroupParams.team = p.team;
                    subgroupParams.prefab = p.prefab;
                    RideID squad = CreateSquad(subgroupParams);
                    groupSystem.AddSubgroup(platoon, squad);
                }
            }

            return platoon;
        }

        public RideID CreateCompany(UnitCreationParams p)
        {
            RideVector3 startPos = CalculateStartPosition(p);
            RideID company = groupSystem.CreateGroup(p.name + " Company");
            RideID companyCommander = agentSystem.AddAgent(new Unit(p.team, new RideVector3(startPos.x, startPos.y, startPos.z), p.prefab) { name = p.name + " Company Commander" });
            RideID companyXO = agentSystem.AddAgent(new Unit(p.team, new RideVector3(startPos.x, startPos.y, startPos.z), p.prefab) { name = p.name + " Company XO" });
            groupSystem.AddMember(company, companyCommander, 40, RideDefines.CompanyCommander);
            groupSystem.AddMember(company, companyXO, 40, RideDefines.CompanyXO);
            RideID companyOfficers = CreateCustomGroup(new UnitCreationParams("Company Officers") { rows = 2, cols = 2, memberSeparation = p.memberSeparation, position = p.position, rotation = p.rotation, team = p.team, prefab = p.prefab });
            groupSystem.AddSubgroup(company, companyOfficers);

            //NOTE: for now, companies will consist of 3 platoons, a company commander and a company XO
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    UnitCreationParams subgroupParams = new UnitCreationParams(p.name);
                    subgroupParams.rows = subgroupParams.cols = 2;
                    subgroupParams.memberSeparation = p.memberSeparation;
                    subgroupParams.position = new RideVector3(startPos.x + i * p.memberSeparation, startPos.y, startPos.z - j * p.memberSeparation);
                    subgroupParams.rotation = p.rotation;
                    subgroupParams.team = p.team;
                    subgroupParams.prefab = p.prefab;
                    RideID platoon = CreatePlatoon(subgroupParams);
                    groupSystem.AddSubgroup(company, platoon);
                }
            }

            return company;
        }
    }
}
