using Verse;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;

namespace SkyMind
{
    public class ThoughtWorker_WantToSleepWithSpouseOrLover_Patch
    {
        // Cloud pawns don't trigger spouse/lover sleeping needs.
        [HarmonyPatch(typeof(ThoughtWorker_WantToSleepWithSpouseOrLover), "CurrentStateInternal")]
        public class CurrentStateInternal_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Pawn p, ref ThoughtState __result)
            {
                if (!__result.Active)
                    return;

                Pawn otherPawn = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel(p, false).otherPawn;

                HashSet<Pawn> cloudPawns = SMN_Utils.gameComp.GetCloudPawns();

                if (cloudPawns.Contains(p) || cloudPawns.Contains(otherPawn))
                    __result = false;
            }
        }
    }
}