using Verse;
using RimWorld;
using System.Collections.Generic;

namespace SkyMind
{
    public class CompInsightBench : ThingComp
    {
        public SMN_ServerType SMN_ServerType
        {
            get
            {
                return serverMode;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref serverMode, "SMN_serverMode", SMN_ServerType.SkillServer);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            building = (Building)parent;
            networkConnection = parent.GetComp<CompSkyMind>();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!parent.GetComp<CompPowerTrader>().PowerOn || networkConnection?.connected == false)
                yield break;

            // Generate button to switch server mode based on which servermode the server is currently in.
            switch (serverMode)
            {
                case SMN_ServerType.None:
                    yield break;
                case SMN_ServerType.SkillServer:
                    yield return new Command_Action
                    { // In Skill Mode, can switch to Security
                        icon = SMN_Textures.SkillIcon,
                        defaultLabel = "SMN_SkillMode".Translate(),
                        defaultDesc = "SMN_SkillModeDesc".Translate(),
                        action = delegate ()
                        {
                            serverMode = SMN_ServerType.SecurityServer;
                        }
                    };
                    break;
                case SMN_ServerType.SecurityServer:
                    yield return new Command_Action
                    { // In Security Mode, can switch to Hacking
                        icon = SMN_Textures.SecurityIcon,
                        defaultLabel = "SMN_SecurityMode".Translate(),
                        defaultDesc = "SMN_SecurityModeDesc".Translate(),
                        action = delegate ()
                        {
                            serverMode = SMN_ServerType.HackingServer;
                        }
                    };
                    break;
                case SMN_ServerType.HackingServer:
                    yield return new Command_Action
                    { // In Hacking Mode, can switch to Skill
                        icon = SMN_Textures.HackingIcon,
                        defaultLabel = "SMN_HackingMode".Translate(),
                        defaultDesc = "SMN_HackingModeDesc".Translate(),
                        action = delegate ()
                        {
                            serverMode = SMN_ServerType.SkillServer;
                        }
                    };

                    // Servers in hacking mode allow access to the hacking menu for deploying a hack.
                    if (SkyMindNetwork_Settings.playerCanHack)
                    {
                        yield return new Command_Action
                        {
                            icon = SMN_Textures.HackingWindowIcon,
                            defaultLabel = "SMN_HackingWindow".Translate(),
                            defaultDesc = "SMN_HackingWindowDesc".Translate(),
                            action = delegate ()
                            {
                                Find.WindowStack.Add(new Dialog_HackingWindow());
                            }
                        };
                    }
                    break;
                default:
                    yield return new Command_Action
                    { // In an illegal Mode, can switch to Skill
                        icon = SMN_Textures.SkillIcon,
                        defaultLabel = "SMN_SwitchToSkillMode".Translate(),
                        defaultDesc = "SMN_SwitchToSkillModeDesc".Translate(),
                        action = delegate ()
                        {
                            serverMode = SMN_ServerType.SkillServer;
                        }
                    };
                    break;
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            // No building can be connected to the network when despawned.
            if (networkConnection?.connected == true)
            {
                SMN_Utils.gameComp.DisconnectFromSkyMind(building);
            }
        }

        private CompSkyMind networkConnection;
        private Building building;
        private SMN_ServerType serverMode = SMN_ServerType.SkillServer;
    }
}