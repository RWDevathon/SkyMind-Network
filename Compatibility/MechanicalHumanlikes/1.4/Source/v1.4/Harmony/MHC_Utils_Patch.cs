using HarmonyLib;
using MechHumanlikes;
using Verse;

namespace SkyMind
{
    public class MHC_Utils_Patch
    {
        // Surrogates are treated as non-humanlike intelligences.
        [HarmonyPatch(typeof(MHC_Utils), "IsConsideredNonHumanlike")]
        public class IsConsideredNonHumanlike_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Pawn pawn, ref bool __result)
            {
                __result = __result || SMN_Utils.IsSurrogate(pawn);
            }
        }
    }
}
