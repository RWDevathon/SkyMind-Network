using Verse;
using RimWorld;
using System.Collections.Generic;

namespace SkyMind
{
    public class CompRemotelyTriggered : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Only connected, controlled explosive traps may be remotely triggered.
            if (parent.GetComp<CompSkyMind>()?.connected != true || parent.Faction != Faction.OfPlayer)
                yield break;

            yield return new Command_Action
            {
                icon = SMN_Textures.HackingIcon,
                defaultLabel = "SMN_DetonateIEDRemotely".Translate(),
                defaultDesc = "SMN_DetonateIEDRemotelyDesc".Translate(),
                action = delegate ()
                {
                    // Building_Trap takes a pawn as an argument, but Building_TrapExplosive does not use it, so passing null should be fine.
                    ((Building_TrapExplosive)parent).Spring(null);
                }
            };
        }
    }
}