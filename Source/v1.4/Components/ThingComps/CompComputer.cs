using System.Text;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace SkyMind
{
    public class CompComputer : ThingComp
    {
        public CompProperties_Computer Props
        {
            get
            {
                return (CompProperties_Computer)props;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref serverMode, "SMN_serverMode", SMN_ServerType.SkillServer);
        }

        // There are two possible spawn states: created, in which case it sets its serverMode from Props and waits to turn on; post load spawn, in which case it already has a mode and state.
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            building = (Building)parent;
            networkConnection = parent.GetComp<CompSkyMind>();
            powerConnection = parent.GetComp<CompPowerTrader>();

            if (networkConnection == null || powerConnection == null)
            {
                Log.Error("[SMN] " + parent + " is missing a SkyMind or PowerTrader Comp! This means the gizmos/inspect pane are going to break! Report which object is responsible for this to its mod author.");
            }

            if (!respawningAfterLoad)
            {
                serverMode = Props.serverMode;
            }
        }

        public override void ReceiveCompSignal(string signal)
        {
            if (signal == "PowerTurnedOff" || signal == "SkyMindNetworkUserDisconnected")
            {
                SMN_Utils.gameComp.RemoveServer(building, serverMode);
            }
            else if ((signal == "SkyMindNetworkUserConnected" && powerConnection.PowerOn) || (signal == "PowerTurnedOn" && networkConnection?.connected != false))
            {
                UpdateGlow();
                SMN_Utils.gameComp.AddServer(building, serverMode);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!powerConnection.PowerOn || networkConnection?.connected == false)
                yield break;

            // Generate button to switch server mode based on which servermode the server is currently in.
            switch (serverMode)
            {
                // In Skill Mode, can switch to Security
                case SMN_ServerType.SkillServer:
                    yield return new Command_Action
                    {
                        icon = SMN_Textures.SkillIcon,
                        defaultLabel = "SMN_SkillMode".Translate(),
                        defaultDesc = "SMN_SkillModeDesc".Translate(),
                        action = delegate ()
                        {
                            ChangeServerMode(SMN_ServerType.SecurityServer);
                        }
                    };
                    break;
                // In Security Mode, can switch to Hacking
                case SMN_ServerType.SecurityServer:
                    yield return new Command_Action
                    {
                        icon = SMN_Textures.SecurityIcon,
                        defaultLabel = "SMN_SecurityMode".Translate(),
                        defaultDesc = "SMN_SecurityModeDesc".Translate(),
                        action = delegate ()
                        {
                            ChangeServerMode(SMN_ServerType.HackingServer);
                        }
                    };
                    break;
                // In Hacking Mode, can switch to Skill
                case SMN_ServerType.HackingServer:
                    yield return new Command_Action
                    {
                        icon = SMN_Textures.HackingIcon,
                        defaultLabel = "SMN_HackingMode".Translate(),
                        defaultDesc = "SMN_HackingModeDesc".Translate(),
                        action = delegate ()
                        {
                            ChangeServerMode(SMN_ServerType.SkillServer);
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
                // In an illegal Mode, can switch to Skill
                default:
                    yield return new Command_Action
                    {
                        icon = SMN_Textures.SkillIcon,
                        defaultLabel = "SMN_SwitchToSkillMode".Translate(),
                        defaultDesc = "SMN_SwitchToSkillModeDesc".Translate(),
                        action = delegate ()
                        {
                            serverMode = SMN_ServerType.SkillServer;
                            SMN_Utils.gameComp.AddServer(building, serverMode);
                        }
                    };
                    break;
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder ret = new StringBuilder();
            if (!powerConnection.PowerOn)
                return "";

            if (networkConnection?.connected == false)
            {
                ret.Append("SMN_ServerNetworkConnectionNeeded".Translate());
                return ret.Append(base.CompInspectStringExtra()).ToString();
            }

            if (serverMode == SMN_ServerType.SkillServer)
            {
                ret.AppendLine("SMN_SkillServersSynthesis".Translate(SMN_Utils.gameComp.GetPoints(SMN_ServerType.SkillServer), SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.SkillServer)))
                   .AppendLine("SMN_SkillProducedPoints".Translate(Props.passivePointGeneration))
                   .Append("SMN_SkillSlotsAdded".Translate(Props.pointStorage));
            }
            else if (serverMode == SMN_ServerType.SecurityServer)
            {
                ret.AppendLine("SMN_SecurityServersSynthesis".Translate(SMN_Utils.gameComp.GetPoints(SMN_ServerType.SecurityServer), SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.SecurityServer)))
                   .AppendLine("SMN_SecurityProducedPoints".Translate(Props.passivePointGeneration))
                   .Append("SMN_SecuritySlotsAdded".Translate(Props.pointStorage));
            }
            else if (serverMode == SMN_ServerType.HackingServer)
            {
                ret.AppendLine("SMN_HackingServersSynthesis".Translate(SMN_Utils.gameComp.GetPoints(SMN_ServerType.HackingServer), SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.HackingServer)))
                   .AppendLine("SMN_HackingProducedPoints".Translate(Props.passivePointGeneration))
                   .Append("SMN_HackingSlotsAdded".Translate(Props.pointStorage));
            }
            return ret.Append(base.CompInspectStringExtra()).ToString();
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            // Servers can not be connected to the network when despawned.
            if (networkConnection?.connected == true)
            {
                SMN_Utils.gameComp.DisconnectFromSkyMind(building);
            }
        }

        // Change this server to the given type, making sure it deregisters from the previous type.
        public void ChangeServerMode(SMN_ServerType newMode)
        {
            SMN_Utils.gameComp.RemoveServer(building, serverMode);
            SMN_Utils.gameComp.AddServer(building, newMode);
            serverMode = newMode;
            UpdateGlow();
        }

        // Change the color of the server's glow to match its server type: green for skill, blue for security, red for hacking, black for illegal states. Do nothing if there is no CompGlower.
        private void UpdateGlow()
        {
            CompGlower glower = parent.GetComp<CompGlower>();
            if (glower == null)
            {
                return;
            }

            switch (serverMode)
            {
                case SMN_ServerType.SkillServer:
                {
                    glower.GlowColor = new ColorInt(0, 200, 0);
                    break;
                }
                case SMN_ServerType.SecurityServer:
                {
                    glower.GlowColor = new ColorInt(0, 0, 200);
                    break;
                }
                case SMN_ServerType.HackingServer:
                {
                    glower.GlowColor = new ColorInt(200, 0, 0);
                    break;
                }
                default:
                {
                    glower.GlowColor = new ColorInt(0, 0, 0);
                    break;
                }
            }
        }

        private CompSkyMind networkConnection;
        private CompPowerTrader powerConnection;
        private Building building;
        private SMN_ServerType serverMode = SMN_ServerType.SkillServer;
    }
}
