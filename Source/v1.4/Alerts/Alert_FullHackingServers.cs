using Verse;
using RimWorld;

namespace SkyMind
{
    public class Alert_FullHackingServers : Alert
    {
        public Alert_FullHackingServers()
        {
            defaultLabel = "SMN_AlertFullHackingServers".Translate();
            defaultExplanation = "SMN_AlertFullHackingServersDesc".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertReport GetReport()
        {
            if (!SkyMindNetwork_Settings.playerCanHack || !SkyMindNetwork_Settings.receiveHackingAlert || SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.HackingServer) <= 0)
                return false;

            // Only display the hacking alert if it is near capacity and the hacking penalty is not so bad they can't afford an operation even with used capacity.
            float points = SMN_Utils.gameComp.GetPoints(SMN_ServerType.HackingServer);
            if (points >= SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.HackingServer) * 0.9f && points >= SMN_Utils.gameComp.hackCostTimePenalty + 400)
            {
                return true;
            }
            return false;
        }
    }
}
