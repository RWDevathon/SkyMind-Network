using Verse;
using RimWorld;

namespace SkyMind
{
    public class Alert_FullSkillServers : Alert
    {
        public Alert_FullSkillServers()
        {
            defaultLabel = "SMN_AlertFullSkillServers".Translate();
            defaultExplanation = "SMN_AlertFullSkillServersDesc".Translate();
            defaultPriority = AlertPriority.Medium;
        }


        public override AlertReport GetReport()
        {
            if (!SkyMindNetwork_Settings.receiveSkillAlert || SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.SkillServer) <= 0)
                return false;

            if (SMN_Utils.gameComp.GetPoints(SMN_ServerType.SkillServer) >= SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.SkillServer) * 0.9f)
            {
                return true;
            }
            return false;
        }
    }
}
