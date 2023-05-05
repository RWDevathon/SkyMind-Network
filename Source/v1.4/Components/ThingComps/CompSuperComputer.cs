using System.Collections.Generic;
using System.Text;
using UnityEngine.Diagnostics;
using Verse;

namespace SkyMind
{
    public class CompSuperComputer : ThingComp
    {
        public CompProperties_SuperComputer Props
        {
            get
            {
                return (CompProperties_SuperComputer)props;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            building = (Building)parent;

            // The server lists need to know how much storage and point generation exists for each server mode. This adds it to all three types.
            if (!respawningAfterLoad)
                SMN_Utils.gameComp.AddServer(building);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Servers in hacking mode allow access to the hacking menu for deploying a hack. Supercomputers always enable hacking mode, so they always have the gizmo to open the menu.
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
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder ret = new StringBuilder();

            ret.AppendLine("SMN_SkillServersSynthesis".Translate(SMN_Utils.gameComp.GetPoints(SMN_ServerType.SkillServer), SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.SkillServer)))
               .AppendLine("SMN_SecurityServersSynthesis".Translate(SMN_Utils.gameComp.GetPoints(SMN_ServerType.SecurityServer), SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.SecurityServer)))
               .AppendLine("SMN_HackingServersSynthesis".Translate(SMN_Utils.gameComp.GetPoints(SMN_ServerType.HackingServer), SMN_Utils.gameComp.GetPointCapacity(SMN_ServerType.HackingServer)))
               .AppendLine("SMN_SkillProducedPoints".Translate(Props.passivePointGeneration))
               .AppendLine("SMN_SecurityProducedPoints".Translate(Props.passivePointGeneration))
               .AppendLine("SMN_HackingProducedPoints".Translate(Props.passivePointGeneration))
               .AppendLine("SMN_SkillSlotsAdded".Translate(Props.pointStorage))
               .AppendLine("SMN_SecuritySlotsAdded".Translate(Props.pointStorage))
               .Append("SMN_HackingSlotsAdded".Translate(Props.pointStorage));
            return ret.Append(base.CompInspectStringExtra()).ToString();
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);

            // The server lists need to know how much storage exists for each server mode. This removes it from all three types.
            SMN_Utils.gameComp.RemoveServer(building);
        }

        public override void Notify_MapRemoved()
        {
            base.Notify_MapRemoved();

            // Things that have their map removed are not despawned but outright lost, but it should still be removed from the server types.
            SMN_Utils.gameComp.RemoveServer(building);
        }

        private Building building = null;
    }
}
