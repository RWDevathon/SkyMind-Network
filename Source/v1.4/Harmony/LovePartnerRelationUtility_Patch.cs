using Verse;
using RimWorld;
using HarmonyLib;

namespace SkyMind
{
    public class LovePartnerRelationUtility_Patch
    {
        // Pawns will consider surrogates of their loved one to also be a loved one.
        [HarmonyPatch(typeof(LovePartnerRelationUtility), "LovePartnerRelationExists")]
        public class LovePartnerRelationExists_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Pawn first, Pawn second, ref bool __result)
            {
                if (__result)
                {
                    return;
                }

                // Check the first pawn for surrogate status.
                if (SMN_Utils.IsSurrogate(first))
                {
                    Pawn controller = first.GetComp<CompSkyMindLink>()?.GetSurrogates()?.FirstOrFallback();
                    if (controller != null && LovePartnerRelationUtility.LovePartnerRelationExists(controller, second))
                    {
                        __result = true;
                    }
                }
                else if (SMN_Utils.IsSurrogate(second))
                {
                    Pawn controller = second.GetComp<CompSkyMindLink>()?.GetSurrogates()?.FirstOrFallback();
                    if (controller != null && LovePartnerRelationUtility.LovePartnerRelationExists(first, controller))
                    {
                        __result = true;
                    }
                }
            }
        }
    }
}