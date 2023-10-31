using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;

namespace SkyMind
{
    public class CompSkyMindCommsConsole : ThingComp
    {
        private Building_CommsConsole Console
        {
            get
            {
                if (console == null)
                {
                    console = (Building_CommsConsole)parent;
                }
                return console;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Do not show button to remotely use the console if it isn't connected to the SkyMind or there are no connected devices.
            if (!parent.GetComp<CompSkyMind>()?.connected ?? false || SMN_Utils.gameComp.GetSkyMindDevices().Count == 0)
            {
                yield break;
            }

            // Do not show buttons if the console can not be used.
            if (!Console.CanUseCommsNow)
            {
                yield break;
            }

            IEnumerable<Thing> validPawns = SMN_Utils.gameComp.GetSkyMindDevices().Where(device => device is Pawn possiblePawn && possiblePawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking)).Concat(SMN_Utils.gameComp.GetCloudPawns().Where(cloudPawn => !cloudPawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled));

            if (!validPawns.Any())
            {
                yield break;
            }

            yield return new Command_Action
            {
                icon = SMN_Textures.ControlModeIcon,
                defaultLabel = "SMN_OpenRemoteComms".Translate(),
                defaultDesc = "SMN_OpenRemoteCommsDesc".Translate(),
                action = delegate ()
                {
                    List<FloatMenuOption> pawnOpts = new List<FloatMenuOption>();
                    foreach (Pawn pawn in validPawns)
                    {
                        pawnOpts.Add(new FloatMenuOption(pawn.LabelShortCap, delegate
                        {
                            List<FloatMenuOption> targetOpts = new List<FloatMenuOption>();
                            foreach (ICommunicable commTarget in Console.GetCommTargets(pawn))
                            {
                                // Factions can be communicated with even if the pawn has social work prohibited.
                                if (commTarget is Faction faction)
                                {
                                    targetOpts.Add(new FloatMenuOption(commTarget.GetCallLabel(), delegate
                                    {
                                        commTarget.TryOpenComms(pawn);
                                    }, itemIcon: faction.def.FactionIcon, iconColor: faction.Color));
                                }
                                // Other targets require social skill in order to be communicated with.
                                else
                                {
                                    if (pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
                                    {
                                        continue;
                                    }

                                    targetOpts.Add(new FloatMenuOption(commTarget.GetCallLabel(), delegate
                                    {
                                        commTarget.TryOpenComms(pawn);
                                    }));
                                }
                            }

                            if (targetOpts.Count != 0)
                            {
                                Find.WindowStack.Add(new FloatMenu(targetOpts));
                            }
                        }));
                    }
                    pawnOpts.SortBy((x) => x.Label);

                    if (pawnOpts.Count != 0)
                    {
                        Find.WindowStack.Add(new FloatMenu(pawnOpts, "SMN_ViableSources".Translate()));
                    }
                }
            };
        }

        private Building_CommsConsole console;
    }
}